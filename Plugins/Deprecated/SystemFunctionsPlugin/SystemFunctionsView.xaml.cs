using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Layouter.Plugins.Deprecated.SystemFunctionsPlugin;

namespace Layouter.Plugins.SystemFunctionsPlugin
{
    /// <summary>
    /// SystemFunctionsView.xaml 的交互逻辑
    /// </summary>
    public partial class SystemFunctionsView : UserControl
    {
        private List<SystemFunction> systemFunctions;

        public SystemFunctionsView()
        {
            InitializeComponent();
        }

        public SystemFunctionsView(List<SystemFunction> systemFunctions) : this()
        {
            SetSystemFunctions(systemFunctions);
        }

        public void SetSystemFunctions(List<SystemFunction> functions)
        {
            this.systemFunctions = functions;
            SystemFunctionsItemsControl.ItemsSource = this.systemFunctions;
        }

        private void SystemFunction_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                var element = sender as FrameworkElement;
                if (element != null)
                {
                    var systemFunction = element.Tag as SystemFunction;
                    if (systemFunction != null)
                    {
                        try
                        {
                            Process.Start("explorer.exe", systemFunction.Path);
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"打开系统功能时出错: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
