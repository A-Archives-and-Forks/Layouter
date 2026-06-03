using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using PluginEntry;

namespace Layouter.Plugins
{

    public interface IPlugin
    {
        string Name { get; }

        void Register();

        Dictionary<string, Func<PluginParameter[], object>> FunctionDict { get; }

        //IReadOnlyDictionary<string, Func<dynamic, object>> GetFunctions();

        //object Run(string functionKey, dynamic parameters);
        object Run(string functionKey, params PluginParameter[] parameters);

        /// <summary>
        /// 获取参数描述
        /// </summary>
        Dictionary<string, List<PluginParameter>> GetParameterDescriptions();
    }

}
