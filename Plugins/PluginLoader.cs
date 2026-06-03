using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using PluginEntry;

namespace Layouter.Plugins
{
    public class PluginLoader
    {
        private readonly string pluginsDirectory;
        private readonly List<Assembly> loadedAssemblies = new List<Assembly>();
        private readonly Dictionary<string, string> pluginExtractionPaths = new Dictionary<string, string>();
        private readonly Dictionary<string, string> pluginManifestDirectories = new Dictionary<string, string>();
        private readonly Dictionary<string, LoadedPlugin> metadataLoadedPlugins = new Dictionary<string, LoadedPlugin>();
        private readonly PluginManager pluginManager;

        public PluginLoader(string pluginsDirectory, PluginManager pluginManager = null)
        {
            this.pluginsDirectory = pluginsDirectory;
            this.pluginManager = pluginManager;
            Directory.CreateDirectory(this.pluginsDirectory);
        }

        public IEnumerable<LoadedPlugin> LoadPluginMetadata()
        {
            var loadedPlugins = new List<LoadedPlugin>();
            var plugFiles = Directory.GetFiles(pluginsDirectory, $"*{PluginProtocol.PackageExtension}");

            foreach (var plugFile in plugFiles)
            {
                try
                {
                    var packageName = Path.GetFileNameWithoutExtension(plugFile);
                    var tempDir = Path.Combine(Path.GetTempPath(), "Layouter", "Plugins", packageName);

                    // 清理之前可能存在的目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                    Directory.CreateDirectory(tempDir);

                    ZipFile.ExtractToDirectory(plugFile, tempDir);

                    var jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);

                    #region 插件描述文件
                    var descriptorFile = FindPluginFile(jsonFiles, PluginProtocol.ManifestFileName);

                    if (descriptorFile == null)
                    {
                        continue;
                    }

                    var descriptorJson = File.ReadAllText(descriptorFile);
                    var descriptor = PluginDescriptor.FromJson(descriptorJson);

                    // 设置ID以便跟踪
                    if (string.IsNullOrEmpty(descriptor.Id))
                    {
                        descriptor.Id = packageName;
                    }

                    if (string.IsNullOrWhiteSpace(descriptor.ProtocolVersion))
                    {
                        descriptor.ProtocolVersion = PluginProtocol.CurrentVersion;
                    }

                    var manifestDirectory = Path.GetDirectoryName(descriptorFile);
                    pluginExtractionPaths[descriptor.Id] = tempDir;
                    pluginManifestDirectories[descriptor.Id] = manifestDirectory;

                    #endregion

                    #region 图标描述文件

                    IconDescriptor iconDescriptor = null;
                    var iconFile = FindPluginFile(jsonFiles, PluginProtocol.IconFileName);

                    if (iconFile != null)
                    {
                        var iconJson = File.ReadAllText(iconFile);
                        iconDescriptor = IconDescriptor.FromJson(iconJson);
                        iconDescriptor.ResolveRelativePaths(Path.GetDirectoryName(iconFile));
                    }

                    #endregion

                    #region 样式描述文件

                    PluginStyle style = null;
                    var styleFile = FindPluginFile(jsonFiles, PluginProtocol.StyleFileName);
                    if (styleFile != null)
                    {
                        var styleJson = File.ReadAllText(styleFile);
                        style = PluginStyle.FromJson(styleJson);
                    }
                    #endregion

                    var userSettings = PluginSettingsService.Instance.LoadPluginSettings(descriptor.Id);
                    if (userSettings?.IsEnabled.HasValue == true)
                    {
                        descriptor.IsEnabled = userSettings.IsEnabled.Value;
                    }
                    if (userSettings?.Style != null)
                    {
                        style = userSettings.Style;
                    }

                    // 创建只包含元数据的LoadedPlugin对象
                    var lp = new LoadedPlugin(descriptor, null, iconDescriptor, style)
                    {
                        PackageFilePath = plugFile,
                        ExtractionDirectory = tempDir,
                        ManifestDirectory = manifestDirectory,
                        PackageName = packageName
                    };
                    loadedPlugins.Add(lp);

                    // 保存到字典中，以便后续异步加载代码
                    metadataLoadedPlugins[descriptor.Id] = lp;
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading plugin metadata {plugFile}: {ex.Message}");
                }
            }

            return loadedPlugins;
        }

        private static string FindPluginFile(IEnumerable<string> files, string fileName)
        {
            return files.FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public class PluginCodeLoadResult
        {
            public IPlugin Plugin { get; set; }
            public Dictionary<string, List<PluginParameter>> ParameterDescriptions { get; set; }
        }

        public async Task<PluginCodeLoadResult> LoadPluginCode(LoadedPlugin plugin)
        {
            string pluginId = plugin?.Descriptor?.Id;

            try
            {
                // 检查插件元数据是否已加载
                if (!metadataLoadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
                {
                    Log.Error($"Plugin metadata not loaded for {pluginId}");
                    return null;
                }

                // 获取插件解压路径
                if (!pluginExtractionPaths.TryGetValue(pluginId, out var tempDir) || !Directory.Exists(tempDir))
                {
                    Log.Error($"Plugin extraction path not found for {pluginId}");
                    return null;
                }

                var jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);
                var descriptorFile = FindPluginFile(jsonFiles, PluginProtocol.ManifestFileName);

                if (descriptorFile == null)
                {
                    Log.Error($"Plugin descriptor file not found for {pluginId}");
                    return null;
                }

                var descriptorJson = File.ReadAllText(descriptorFile);
                var descriptor = PluginDescriptor.FromJson(descriptorJson);

                // 加载插件代码
                var codeFilePath = Path.Combine(Path.GetDirectoryName(descriptorFile), descriptor.CodeFilePath.TrimStart('.', '\\', '/'));

                if (!File.Exists(codeFilePath))
                {
                    Log.Error($"Plugin code file not found: {codeFilePath}");
                    return null;
                }

#pragma warning disable CS8603 // 可能返回 null 引用。

                // 使用Task.Run在后台线程执行耗时操作
                return await Task.Run(() =>
                {
                    try
                    {
                        var code = File.ReadAllText(codeFilePath);

                        // 进行安全检查
                        var securityChecker = new PluginSecurityChecker();
                        var validationResult = securityChecker.ValidateCode(code, descriptor);
                        if (!validationResult.IsAllowed)
                        {
                            Log.Error($"Security check failed for plugin {descriptor.Name}: {validationResult}");
                            return null;
                        }

                        //引用的dll文件
                        var dllFiles = Directory.GetFiles(tempDir, "*.dll", SearchOption.AllDirectories);

                        var assembly = CompileCode(code, dllFiles);
                        loadedAssemblies.Add(assembly);

                        var pluginType = assembly.GetTypes().FirstOrDefault(t => t.Name.Equals(descriptor.PluginClassName, StringComparison.CurrentCultureIgnoreCase));

                        if (pluginType == null)
                        {
                            Log.Error($"Plugin class {descriptor.PluginClassName} not found");
                            return null;
                        }

                        if (!typeof(IPlugin).IsAssignableFrom(pluginType))
                        {
                            Log.Error($"Plugin class {descriptor.PluginClassName} must implement IPlugin");
                            return null;
                        }

                        var plugin = Activator.CreateInstance(pluginType) as IPlugin;
                        plugin?.Register();

                        Dictionary<string, List<PluginParameter>> parameterDescriptions = null;
                        try
                        {
                            parameterDescriptions = plugin.GetParameterDescriptions();
                        }
                        catch
                        {
                            // 插件不支持参数描述,使用默认空字典
                            parameterDescriptions = new Dictionary<string, List<PluginParameter>>();
                        }

                        var result = new PluginCodeLoadResult
                        {
                            Plugin = plugin,
                            ParameterDescriptions = parameterDescriptions
                        };

                        return result;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error loading plugin code for {pluginId}: {ex.Message}");
                        return null;
                    }
                });
#pragma warning restore CS8603 // 可能返回 null 引用。
            }
            catch (Exception ex)
            {
                Log.Error($"Error in LoadPluginCodeAsync for {pluginId}: {ex.Message}");
                return null;
            }
        }

        public IEnumerable<LoadedPlugin> LoadPlugins()
        {
            // 首先加载插件元数据
            var metadataPlugins = LoadPluginMetadata();
            var loadedPlugins = new List<LoadedPlugin>();

            // 然后加载每个插件的代码
            foreach (var plugin in metadataPlugins)
            {
                try
                {
                    var result = LoadPluginCode(plugin).GetAwaiter().GetResult();

                    if (result != null)
                    {
                        // 更新插件对象，添加代码和参数描述
                        plugin.SetPlugin(result.Plugin, result.ParameterDescriptions);
                        loadedPlugins.Add(plugin);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading plugin code for {plugin.Descriptor.Id}: {ex.Message}");
                }
            }

            return loadedPlugins;
        }

        public string GetPluginExtractionPath(string pluginId)
        {
            return pluginExtractionPaths.TryGetValue(pluginId, out var path) ? path : null;
        }

        public string GetPluginManifestDirectory(string pluginId)
        {
            return pluginManifestDirectories.TryGetValue(pluginId, out var path) ? path : null;
        }

        private Assembly CompileCode(string sourceCode, params string[] dllReferences)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            //var references = new MetadataReference[]
            //{
            //    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            //    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            //    MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            //    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location),
            //    // WPF相关引用
            //    MetadataReference.CreateFromFile(typeof(System.Windows.Window).Assembly.Location),
            //    MetadataReference.CreateFromFile(typeof(System.Windows.Controls.Control).Assembly.Location),
            //    MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location)
            //};

            var references = AppDomain.CurrentDomain.GetAssemblies()
                                 .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                 .Select(a => MetadataReference.CreateFromFile(a.Location));

            //添加外部dll引用
            if (dllReferences.Any())
            {
                foreach (var dll in dllReferences)
                {
                    if (File.Exists(dll))
                    {
                        references = references.Append(MetadataReference.CreateFromFile(dll)).ToArray();
                    }
                }
            }

            var compilation = CSharpCompilation.Create(Path.GetRandomFileName(),
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    var errorMessage = new StringBuilder();
                    foreach (var diagnostic in failures)
                    {
                        errorMessage.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                    }

                    throw new Exception($"Compilation failed: {errorMessage}");
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return Assembly.Load(ms.ToArray());
                }
            }
        }
    }

    public class LoadedPlugin
    {
        public PluginDescriptor Descriptor { get; }

        public IPlugin Plugin { get; private set; }

        public IconDescriptor IconDescriptor { get; }

        public PluginStyle Style { get; private set; }

        public Dictionary<string, List<PluginParameter>> ParameterDescriptions { get; private set; }

        public bool IsCodeLoaded => Plugin != null;

        public string PackageFilePath { get; set; }

        public string ExtractionDirectory { get; set; }

        public string ManifestDirectory { get; set; }

        public string PackageName { get; set; }

        public LoadedPlugin(PluginDescriptor descriptor, IPlugin plugin, IconDescriptor iconDescriptor, PluginStyle style = null, Dictionary<string, List<PluginParameter>> parameterDescriptions = null)
        {
            Descriptor = descriptor;
            Plugin = plugin;
            IconDescriptor = iconDescriptor;
            Style = style;
            ParameterDescriptions = parameterDescriptions ?? new Dictionary<string, List<PluginParameter>>();
        }

        public LoadedPlugin(PluginDescriptor descriptor, IconDescriptor iconDescriptor)
        {
            Descriptor = descriptor;
            IconDescriptor = iconDescriptor;
            Plugin = null;
            ParameterDescriptions = new Dictionary<string, List<PluginParameter>>();
        }

        public void SetPlugin(IPlugin plugin, Dictionary<string, List<PluginParameter>> parameterDescriptions)
        {
            Plugin = plugin;
            ParameterDescriptions = parameterDescriptions ?? new Dictionary<string, List<PluginParameter>>();
        }

        public void UpdateStyle(PluginStyle style)
        {
            Style = style;
        }
    }

}
