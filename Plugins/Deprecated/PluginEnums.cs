using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Layouter.Plugins
{
    public enum PluginStyle
    {
        [Description("窗口")]
        Window,
        [Description("列表")]
        List,
        [Description("浮块")]
        FloatingBlock
    }
}
