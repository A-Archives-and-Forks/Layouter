using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Layouter.Plugins.MemoryMonitorPlugin
{
    public class MemoryMonitorPlugin : PluginBase
    {
        private MemoryMonitorView memMonitorView;

        public override string Name => "内存监视器";

        public override string Version => "1.0.0";

        public override string Description => "显示系统内存使用情况";

        public override string Author => "Layouter";

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Enable()
        {
            base.Enable();

            if (memMonitorView != null)
            {
                memMonitorView.StartMonitoring();
            }
        }

        public override void Disable()
        {
            base.Disable();

            if (memMonitorView != null)
            {
                memMonitorView.StopMonitoring();
            }
        }

        public override UserControl GetMainView()
        {
            if (memMonitorView == null)
            {
                memMonitorView = new MemoryMonitorView();

                if (IsEnabled)
                {
                    memMonitorView.StartMonitoring();
                }
            }

            return memMonitorView;
        }
    }

}
