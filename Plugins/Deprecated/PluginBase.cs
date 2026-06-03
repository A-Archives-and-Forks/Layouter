using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Layouter.Plugins
{
    /// <summary>
    /// 插件基类
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        private bool isEnabled = false;

        /// <summary>
        /// 插件名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 版本
        /// </summary>
        public abstract string Version { get; }

        /// <summary>
        /// 描述
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 作者
        /// </summary>
        public abstract string Author { get; }

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled => isEnabled;

        /// <summary>
        /// 初始化插件
        /// </summary>
        public virtual void Initialize()
        {
           
        }

        /// <summary>
        /// 启用插件
        /// </summary>
        public virtual void Enable()
        {
            isEnabled = true;
        }

        /// <summary>
        /// 禁用插件
        /// </summary>
        public virtual void Disable()
        {
            isEnabled = false;
        }

        /// <summary>
        /// 获取插件的设置界面
        /// </summary>
        public virtual UserControl GetSettingsView()
        {
            // 默认返回空
            return null;
        }

        /// <summary>
        /// 获取插件的主界面
        /// </summary>
        /// <returns>插件主界面</returns>
        public abstract UserControl GetMainView();

        ///// <summary>
        ///// 保存插件设置
        ///// </summary>
        ///// <typeparam name="T">设置类型</typeparam>
        ///// <param name="settings">设置对象</param>
        ///// <returns>是否保存成功</returns>
        //protected bool SaveSettings<T>(T settings)
        //{
        //    return Services.PluginSettingsService.Instance.SaveSettings(Id, settings);
        //}

        ///// <summary>
        ///// 加载插件设置
        ///// </summary>
        ///// <typeparam name="T">设置类型</typeparam>
        ///// <param name="defaultSettings">默认设置</param>
        ///// <returns>设置对象</returns>
        //protected T LoadSettings<T>(T defaultSettings = default)
        //{
        //    return Services.PluginSettingsService.Instance.LoadSettings(Id, defaultSettings);
        //}

    }
}
