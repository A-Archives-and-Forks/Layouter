using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Layouter.Plugins.Views;
using Layouter.Services;
using PluginEntry;

namespace Layouter.Plugins
{
    public class PluginManager
    {
        private readonly PluginLoader loader;
        private readonly string pluginsDirectory;
        private readonly PluginSecurityChecker securityChecker;
        private WindowTemplateManager templateManager;

        public event EventHandler<PluginStatusChangedEventArgs> PluginStatusChanged;
        public event EventHandler<PluginLoadedEventArgs> PluginLoaded;
        public event EventHandler<PluginCodeLoadedEventArgs> PluginCodeLoaded;

        private readonly Dictionary<string, LoadedPlugin> plugins = new Dictionary<string, LoadedPlugin>();
        public IReadOnlyDictionary<string, LoadedPlugin> Plugins => plugins;

        private readonly Dictionary<string, bool> pluginWindowStates = new Dictionary<string, bool>();
        public IReadOnlyDictionary<string, bool> PluginWindowStates => pluginWindowStates;

        // 记录已显示的插件窗口,避免重复显示
        private readonly HashSet<string> displayedPlugins = new HashSet<string>();


        public PluginManager(string pluginsDirectory)
        {
            this.pluginsDirectory = pluginsDirectory;
            loader = new PluginLoader(pluginsDirectory, this);
            securityChecker = new PluginSecurityChecker();
            templateManager = new WindowTemplateManager(this);
        }

        public void LoadPluginsMetadata(bool autoShowPluginWindows = false)
        {
            var loadedPlugins = loader.LoadPluginMetadata();

            foreach (var plugin in loadedPlugins)
            {
                string id = string.IsNullOrEmpty(plugin.Descriptor.Id)
                    ? Guid.NewGuid().ToString()
                    : plugin.Descriptor.Id;

                plugin.Descriptor.Id = id;
                if (plugins.ContainsKey(id))
                {
                    plugins[id] = plugin;
                    continue;
                }

                plugins[id] = plugin;

                PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(plugin));

                UpdatePluginWindowState(id, true);
            }

            // 如果需要自动显示插件窗口
            if (autoShowPluginWindows)
            {
                ShowAllPluginWindows();
            }
        }

        public async Task<bool> LoadPluginCode(string pluginId)
        {
            if (!plugins.TryGetValue(pluginId, out var plugin))
            {
                Log.Error($"Plugin with ID {pluginId} not found");
                return false;
            }

            if (plugin.IsCodeLoaded)
            {
                // 插件代码已加载,无需重复加载
                return true;
            }

            var result = await loader.LoadPluginCode(plugin);
            if (result != null)
            {
                // 更新插件对象
                plugin.SetPlugin(result.Plugin, result.ParameterDescriptions);

                // 通知插件代码加载完成
                PluginCodeLoaded?.Invoke(this, new PluginCodeLoadedEventArgs(plugin));
                return true;
            }

            return false;
        }

        public void UpdatePluginWindowState(string pluginId, bool isVisible)
        {
            pluginWindowStates[pluginId] = isVisible;
        }

        // 显示所有插件窗口
        public void ShowAllPluginWindows()
        {
            foreach (var plugin in plugins.Values)
            {
                if (plugin.Descriptor.IsEnabled && !displayedPlugins.Contains(plugin.Descriptor.Id))
                {
                    ShowPluginWindow(plugin);
                }
            }
        }

        // 显示单个插件窗口
        public void ShowPluginWindow(LoadedPlugin plugin)
        {
            if (!plugin.Descriptor.IsEnabled || displayedPlugins.Contains(plugin.Descriptor.Id))
            {
                return;
            }

            //首先检测是否已经存在对应插件的窗口的配置文件
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Env.AppName);
            var pluginConfigFile = Path.Combine(configPath, $"partition_{plugin.Descriptor.Id}.json");

            if (File.Exists(pluginConfigFile))
            {
                // 对于已有配置文件的插件,直接显示窗口,不需要等待代码加载
                PartitionDataService.Instance.ShowWindow(plugin.Descriptor.Id, true);
            }
            //首次加载
            else
            {
                // 异步加载插件代码,不阻塞UI线程
                if (!plugin.IsCodeLoaded)
                {
                    Task.Run(async () =>
                    {
                        await LoadPluginCode(plugin.Descriptor.Id);
                    });
                }

                var pluginWindow = templateManager.CreatePluginWindow(plugin);
                pluginWindow.Loaded += (s, e) => SysUtil.SetDesktopLevelWindow(pluginWindow);
                pluginWindow.Show();
            }

            // 记录已显示的插件
            displayedPlugins.Add(plugin.Descriptor.Id);
        }

        public async Task RunPluginFunctionAsync(string pluginId, string functionKey, params PluginParameter[] args)
        {
            if (!plugins.TryGetValue(pluginId, out var plugin))
            {
                Log.Warning($"Plugin with ID {pluginId} not found");
                return;
            }
            if (!plugin.Descriptor.IsEnabled)
            {
                Log.Warning($"Plugin {plugin.Descriptor.Name} is disabled");
                return;
            }

            // 确保插件代码已加载
            if (!plugin.IsCodeLoaded)
            {
                bool loaded = await LoadPluginCode(pluginId);
                if (!loaded)
                {
                    Log.Error($"Failed to load plugin code for {plugin.Descriptor.Name}");
                    return;
                }
            }

            // 执行安全检查
            if (!securityChecker.CheckFunction(plugin.Plugin, functionKey))
            {
                Log.Error($"Security check failed for function {functionKey}");
                return;
            }

            try
            {
                var result = plugin.Plugin.Run(functionKey, args);
            }
            catch (Exception ex)
            {
                Log.Error($"Error executing function {functionKey}: {ex.Message}");
            }
        }

        public async void RunPluginFunction(string pluginId, string functionKey, params PluginParameter[] args)
        {
            if (!plugins.TryGetValue(pluginId, out var plugin))
            {
                Log.Warning($"Plugin with ID {pluginId} not found");
                return;
            }
            if (!plugin.Descriptor.IsEnabled)
            {
                Log.Warning($"Plugin {plugin.Descriptor.Name} is disabled");
                return;
            }

            // 确保插件代码已加载
            if (!plugin.IsCodeLoaded)
            {
                bool loaded = await LoadPluginCode(pluginId);
                if (!loaded)
                {
                    Log.Error($"Failed to load plugin code for {plugin.Descriptor.Name}");
                    return;
                }
            }

            // 执行安全检查
            if (!securityChecker.CheckFunction(plugin.Plugin, functionKey))
            {
                Log.Error($"Security check failed for function {functionKey}");
                return;
            }

            try
            {
                var result = plugin.Plugin.Run(functionKey, args);
            }
            catch (Exception ex)
            {
                Log.Error($"Error executing function {functionKey}: {ex.Message}");
            }
        }

        public void SetPluginEnabled(string pluginId, bool enabled)
        {
            if (!plugins.TryGetValue(pluginId, out var plugin))
            {
                Log.Error($"Plugin with ID {pluginId} not found");
                return;
            }

            if (plugin.Descriptor.IsEnabled != enabled)
            {
                plugin.Descriptor.IsEnabled = enabled;

                // 用户状态保存到 AppData，不回写插件包
                PluginSettingsService.Instance.SaveEnabled(pluginId, enabled);

                PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(plugin, enabled));
            }
        }

        public bool SavePluginStyle(string pluginId, PluginStyle style)
        {
            try
            {
                if (!plugins.TryGetValue(pluginId, out var plugin))
                {
                    return false;
                }

                plugin.UpdateStyle(style);
                return PluginSettingsService.Instance.SaveStyle(pluginId, style);
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving plugin style: {ex.Message}");
                return false;
            }
        }

        public IEnumerable<string> GetPluginFunctions(string pluginId)
        {
            if (!plugins.TryGetValue(pluginId, out var plugin))
            {
                Log.Error($"Plugin with ID {pluginId} not found");
                return null;
            }
            return plugin.Plugin?.FunctionDict?.Keys ?? Enumerable.Empty<string>();
        }

        public bool RemovePlugin(string pluginId)
        {
            if (!plugins.TryGetValue(pluginId, out var plugin))
            {
                return false;
            }

            try
            {
                plugin.Plugin?.Unregister();
                if (!string.IsNullOrWhiteSpace(plugin.PackageFilePath) && File.Exists(plugin.PackageFilePath))
                {
                    File.Delete(plugin.PackageFilePath);
                }

                PluginSettingsService.Instance.DeleteSettings(pluginId);
                plugins.Remove(pluginId);
                pluginWindowStates.Remove(pluginId);
                displayedPlugins.Remove(pluginId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error removing plugin {pluginId}: {ex.Message}");
                return false;
            }
        }
    }

    public class PluginLoadedEventArgs : EventArgs
    {
        public LoadedPlugin Plugin { get; }

        public PluginLoadedEventArgs(LoadedPlugin plugin)
        {
            Plugin = plugin;
        }
    }

    public class PluginStatusChangedEventArgs : EventArgs
    {
        public LoadedPlugin Plugin { get; }
        public bool IsEnabled { get; }

        public PluginStatusChangedEventArgs(LoadedPlugin plugin, bool isEnabled)
        {
            Plugin = plugin;
            IsEnabled = isEnabled;
        }
    }

    public class PluginCodeLoadedEventArgs : EventArgs
    {
        public LoadedPlugin Plugin { get; }

        public PluginCodeLoadedEventArgs(LoadedPlugin plugin)
        {
            Plugin = plugin;
        }
    }

}
