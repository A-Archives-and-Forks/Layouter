using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Layouter.Plugins
{
    public class PluginInfo
    {
        /// <summary>
        /// 插件ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// 程序集路径
        /// </summary>
        public string AssemblyPath { get; set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 样式
        /// </summary>
        public PluginStyle Style { get; set; }

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 插件文件路径
        /// </summary>
        public string PluginPath { get; set; }

        public object GetData()
        {
            return null;
        }
    }


    public class ImportPluginInfo : PluginInfo 
    {
        public List<PluginComplexData> Items { get; set; } = new List<PluginComplexData>();
    }


}
