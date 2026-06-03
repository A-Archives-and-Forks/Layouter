using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Layouter.Plugins
{
    /// <summary>
    /// 插件管理器
    /// </summary>
    public class PluginManager
    {
        private static PluginManager instance;
        public static PluginManager Instance => instance ?? (instance = new PluginManager());

        private readonly string pluginsFolder;
        private readonly string pluginsConfig;
        private readonly Dictionary<string, IPlugin> loadedPlugins;
        private readonly Dictionary<string, PluginInfo> pluginInfos;

        /// <summary>
        /// 获取所有已加载的插件
        /// </summary>
        public IReadOnlyDictionary<string, IPlugin> LoadedPlugins => loadedPlugins;

        /// <summary>
        /// 获取所有插件信息
        /// </summary>
        public IReadOnlyDictionary<string, PluginInfo> PluginInfos => pluginInfos;

        /// <summary>
        /// 插件状态变更事件
        /// </summary>
        public event EventHandler<PluginEventArgs> PluginStatusChanged;

        private PluginManager()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Layouter");
            pluginsFolder = Path.Combine(appDataPath, "Plugins");
            pluginsConfig = Path.Combine(appDataPath, "plugins.json");
            loadedPlugins = new Dictionary<string, IPlugin>();
            pluginInfos = new Dictionary<string, PluginInfo>();

            // 确保插件目录存在
            if (!Directory.Exists(pluginsFolder))
            {
                Directory.CreateDirectory(pluginsFolder);
            }
        }

        /// <summary>
        /// 初始化插件管理器
        /// </summary>
        public void Initialize()
        {
            try
            {
                LoadPluginConfig();

                LoadPlugins();

                Log.Information("插件管理器初始化完成");
            }
            catch (Exception ex)
            {
                Log.Information($"初始化插件管理器时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载插件配置
        /// </summary>
        private void LoadPluginConfig()
        {
            try
            {
                if (File.Exists(pluginsConfig))
                {
                    string json = File.ReadAllText(pluginsConfig);
                    var pluginInfos = JsonConvert.DeserializeObject<List<PluginInfo>>(json);

                    if (pluginInfos != null)
                    {
                        foreach (var info in pluginInfos)
                        {
                            this.pluginInfos[info.Id] = info;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Information($"加载插件配置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存插件配置
        /// </summary>
        public void SavePluginConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(pluginInfos.Values.ToList(), Formatting.Indented);
                File.WriteAllText(pluginsConfig, json);
                Log.Information("插件配置已保存");
            }
            catch (Exception ex)
            {
                Log.Information($"保存插件配置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载所有插件
        /// </summary>
        private void LoadPlugins()
        {
            try
            {
                // 获取插件目录下的所有DLL文件
                string[] dllFiles = Directory.GetFiles(pluginsFolder, "*.dll");

                foreach (string dllFile in dllFiles)
                {
                    try
                    {
                        LoadPlugin(dllFile);
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"加载插件 {dllFile} 时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Information($"加载插件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载单个插件
        /// </summary>
        /// <param name="filePath">插件程序集路径</param>
        /// <returns>是否加载成功</returns>
        public bool LoadPlugin(string filePath)
        {
            try
            {
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext.Equals(".dll"))
                {
                    #region DLL插件
                    // 加载程序集
                    Assembly assembly = Assembly.LoadFrom(filePath);

                    // 查找实现了IPlugin接口的类型
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            // 创建插件实例
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);

                            // 生成插件ID
                            string pluginId = $"{plugin.Name}_{plugin.Version}";

                            // 检查插件是否已加载
                            if (loadedPlugins.ContainsKey(pluginId))
                            {
                                Log.Information($"插件 {pluginId} 已加载，跳过");
                                continue;
                            }

                            // 初始化插件
                            plugin.Initialize();

                            // 添加到已加载插件列表
                            loadedPlugins[pluginId] = plugin;

                            // 更新或添加插件信息
                            if (!pluginInfos.TryGetValue(pluginId, out PluginInfo pluginInfo))
                            {
                                pluginInfo = new PluginInfo
                                {
                                    Id = pluginId,
                                    Name = plugin.Name,
                                    Version = plugin.Version,
                                    Description = plugin.Description,
                                    Author = plugin.Author,
                                    AssemblyPath = filePath,
                                    TypeName = type.FullName,
                                    IsEnabled = false
                                };

                                pluginInfos[pluginId] = pluginInfo;
                            }
                            else
                            {
                                // 更新插件信息
                                pluginInfo.Name = plugin.Name;
                                pluginInfo.Version = plugin.Version;
                                pluginInfo.Description = plugin.Description;
                                pluginInfo.Author = plugin.Author;
                                pluginInfo.AssemblyPath = filePath;
                                pluginInfo.TypeName = type.FullName;
                            }

                            // 如果插件配置为启用，则启用插件
                            if (pluginInfo.IsEnabled)
                            {
                                EnablePlugin(pluginId);
                            }

                            Log.Information($"插件 {pluginId} 加载成功");
                            return true;
                        }
                    }

                    #endregion


                }
                else if (ext.Equals(".json"))
                {
                    string json = File.ReadAllText(filePath);
                    var plugin = JsonConvert.DeserializeObject<ImportPluginInfo>(json);
                    if (plugin != null)
                    {
                        string pluginId = $"{plugin.Name}_{plugin.Version}";
                        pluginInfos[pluginId] = plugin;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Information($"加载插件 {filePath} 时出错: {ex.Message}");
                return false;
            }

            return false;
        }

        /// <summary>
        /// 启用插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>是否启用成功</returns>
        public bool EnablePlugin(string pluginId)
        {
            try
            {
                if (loadedPlugins.TryGetValue(pluginId, out IPlugin plugin) &&
                    pluginInfos.TryGetValue(pluginId, out PluginInfo pluginInfo))
                {
                    if (!plugin.IsEnabled)
                    {
                        plugin.Enable();
                        pluginInfo.IsEnabled = true;
                        SavePluginConfig();

                        // 触发插件状态变更事件
                        OnPluginStatusChanged(new PluginEventArgs(pluginId, true));

                        Log.Information($"插件 {pluginId} 已启用");
                    }

                    return true;
                }

                Log.Information($"插件 {pluginId} 不存在或未加载");
                return false;
            }
            catch (Exception ex)
            {
                Log.Information($"启用插件 {pluginId} 时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 禁用插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>是否禁用成功</returns>
        public bool DisablePlugin(string pluginId)
        {
            try
            {
                if (loadedPlugins.TryGetValue(pluginId, out IPlugin plugin) && pluginInfos.TryGetValue(pluginId, out PluginInfo pluginInfo))
                {
                    if (plugin.IsEnabled)
                    {
                        plugin.Disable();
                        pluginInfo.IsEnabled = false;
                        SavePluginConfig();

                        OnPluginStatusChanged(new PluginEventArgs(pluginId, false));

                        Log.Information($"插件 {pluginId} 已禁用");
                    }
                    return true;
                }

                Log.Information($"插件 {pluginId} 不存在或未加载");
                return false;
            }
            catch (Exception ex)
            {
                Log.Information($"禁用插件 {pluginId} 时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 切换插件状态
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>是否切换成功</returns>
        public bool TogglePluginStatus(string pluginId)
        {
            try
            {
                if (loadedPlugins.TryGetValue(pluginId, out IPlugin plugin))
                {
                    if (plugin.IsEnabled)
                    {
                        return DisablePlugin(pluginId);
                    }
                    else
                    {
                        return EnablePlugin(pluginId);
                    }
                }

                Log.Information($"插件 {pluginId} 不存在或未加载");
                return false;
            }
            catch (Exception ex)
            {
                Log.Information($"切换插件 {pluginId} 状态时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 导入插件
        /// </summary>
        /// <param name="filePath">插件文件路径</param>
        /// <returns>是否导入成功</returns>
        public bool ImportPlugin(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Information($"插件文件 {filePath} 不存在");
                    return false;
                }

                // 获取文件名
                string fileName = Path.GetFileName(filePath);

                // 目标路径
                string targetPath = Path.Combine(pluginsFolder, fileName);

                // 如果目标文件已存在，先删除
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                // 复制文件
                File.Copy(filePath, targetPath);

                // 加载插件
                bool result = LoadPlugin(targetPath);

                if (result)
                {
                    Log.Information($"插件 {fileName} 导入成功");
                }
                else
                {
                    Log.Information($"插件 {fileName} 导入失败");

                    // 删除复制的文件
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Information($"导入插件 {filePath} 时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>是否删除成功</returns>
        public bool RemovePlugin(string pluginId)
        {
            try
            {
                if (pluginInfos.TryGetValue(pluginId, out PluginInfo pluginInfo))
                {
                    if (pluginInfo.IsEnabled)
                    {
                        DisablePlugin(pluginId);
                    }

                    loadedPlugins.Remove(pluginId);
                    pluginInfos.Remove(pluginId);

                    // 保存配置
                    SavePluginConfig();

                    if (File.Exists(pluginInfo.AssemblyPath))
                    {
                        File.Delete(pluginInfo.AssemblyPath);
                    }

                    Log.Information($"插件 {pluginId} 已删除");
                    return true;
                }

                Log.Information($"插件 {pluginId} 不存在");
                return false;
            }
            catch (Exception ex)
            {
                Log.Information($"删除插件 {pluginId} 时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取插件
        /// </summary>
        /// <param name="pluginId">插件ID</param>
        /// <returns>插件实例</returns>
        public IPlugin GetPlugin(string pluginId)
        {
            if (loadedPlugins.TryGetValue(pluginId, out IPlugin plugin))
            {
                return plugin;
            }

            return null;
        }

        /// <summary>
        /// 触发插件状态变更事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnPluginStatusChanged(PluginEventArgs e)
        {
            PluginStatusChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// 插件事件参数
    /// </summary>
    public class PluginEventArgs : EventArgs
    {
        public PluginEventArgs(string pluginId, bool isEnabled)
        {
            PluginId = pluginId;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// 插件ID
        /// </summary>
        public string PluginId { get; }

        /// <summary>
        /// 插件是否已启用
        /// </summary>
        public bool IsEnabled { get; }


    }
}
