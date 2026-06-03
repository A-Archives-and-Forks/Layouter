using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace Layouter.Plugins.SystemFunctionsPlugin
{

    public class SystemFunction
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string IconPath { get; set; }
    }

    public class SystemFunctionsPlugin : PluginBase
    {
        private SystemFunctionsView sysFunctionsView;

        public override string Name => "系统功能插件";

        public override string Version => "1.0.0";

        public override string Description => "系统功能展示";

        public override string Author => "Layouter";

        public override void Initialize()
        {
            base.Initialize();

            // 初始化系统功能列表
            InitializeSystemFunctions();
        }

        public override void Enable()
        {
            base.Enable();

            if (sysFunctionsView != null)
            {
                sysFunctionsView.IsEnabled = true;
            }
        }

        public override void Disable()
        {
            base.Disable();

            if (sysFunctionsView != null)
            {
                sysFunctionsView.IsEnabled = false;
            }
        }

        public override UserControl GetMainView()
        {
            if (sysFunctionsView == null)
            {
                sysFunctionsView = new SystemFunctionsView();
            }

            return sysFunctionsView;
        }

        private void InitializeSystemFunctions()
        {
            // 系统功能列表
            var systemFunctions = new List<SystemFunction>
            {
                new SystemFunction
                {
                    Name = "OneDrive",
                    Path = "::{018D5C66-4533-4307-9B53-224DE2ED1FE6}",
                    IconPath = "pack://application:,,,/Layouter;component/Resources/Icons/onedrive.png"
                },
                new SystemFunction
                {
                    Name = "网络和共享中心",
                    Path = "::{8E908FC9-BECC-40f6-915B-F4CA0E70D03D}",
                    IconPath = "pack://application:,,,/Layouter;component/Resources/Icons/network.png"
                },
                new SystemFunction
                {
                    Name = "同步中心",
                    Path = "::{9C73F5E5-7AE7-4E32-A8E8-8D23B85255BF}",
                    IconPath = "pack://application:,,,/Layouter;component/Resources/Icons/sync.png"
                },
                new SystemFunction
                {
                    Name = "Windows 搜索",
                    Path = "::{2559a1f8-21d7-11d4-bdaf-00c04f60b9f0}",
                    IconPath = "pack://application:,,,/Layouter;component/Resources/Icons/search.png"
                },
                new SystemFunction
                {
                    Name = "控制面板",
                    Path = "::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}",
                    IconPath = "pack://application:,,,/Layouter;component/Resources/Icons/control_panel.png"
                },
                new SystemFunction
                {
                    Name = "回收站",
                    Path = "::{645FF040-5081-101B-9F08-00AA002F954E}",
                    IconPath = "pack://application:,,,/Layouter;component/Resources/Icons/recycle_bin.png"
                }
            };

            // 将系统功能列表传递给视图
            if (sysFunctionsView == null)
            {
                sysFunctionsView = new SystemFunctionsView(systemFunctions);
            }
            else
            {
                sysFunctionsView.SetSystemFunctions(systemFunctions);
            }
        }
    }


}
