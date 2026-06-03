using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Layouter.Models;
using Layouter.Plugins;

namespace Layouter.Plugins.SysCheckPlugin
{
    public class SysCheckDetailedView : Window
    {
        private string pluginId;
        private Grid mainGrid;
        private StackPanel listPanel;

        public SysCheckDetailedView(string pluginId)
        {
            this.pluginId = pluginId;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            Title = "系统资源监控";
            Width = 300;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanMinimize;
            Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));

            mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 标题栏
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 列表栏

            // 创建标题栏
            var titleBar = CreateTitleBar();
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 创建列表栏
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };

            listPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10)
            };

            scrollViewer.Content = listPanel;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            Content = mainGrid;
        }

        private Border CreateTitleBar()
        {
            var titleBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                Padding = new Thickness(10),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80))
            };

            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var titleText = new TextBlock
            {
                Text = "SYSTEM",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            titlePanel.Children.Add(titleText);
            titleBorder.Child = titlePanel;

            return titleBorder;
        }

        public void AddResourceItem(string name, double value, Brush progressBarColor)
        {
            var itemBorder = new Border
            {
                Padding = new Thickness(0, 5, 0, 5)
            };

            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // 资源名称和使用率
            var headerPanel = new Grid();
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = name,
                Foreground = Brushes.White,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);
            headerPanel.Children.Add(nameText);

            var valueText = new TextBlock
            {
                Text = $"{(int)value}%",
                Foreground = Brushes.White,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valueText, 1);
            headerPanel.Children.Add(valueText);

            itemPanel.Children.Add(headerPanel);

            // 进度条
            var progressBarBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Height = 10,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var progressBar = new Border
            {
                Background = progressBarColor,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = (value / 100) * (Width - 20), // 减去边距
                Height = 10
            };

            progressBarBorder.Child = progressBar;
            itemPanel.Children.Add(progressBarBorder);

            // 底部黄线
            var bottomLine = new Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(255, 215, 0)), // 黄色
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            itemPanel.Children.Add(bottomLine);
            itemBorder.Child = itemPanel;

            listPanel.Children.Add(itemBorder);
        }

        public void ClearResourceItems()
        {
            listPanel.Children.Clear();
        }

        public void UpdateResourceItem(string name, double value, Brush progressBarColor)
        {
            // 查找现有项目并更新
            foreach (var child in listPanel.Children)
            {
                if (child is Border itemBorder && itemBorder.Child is StackPanel itemPanel)
                {
                    var headerPanel = itemPanel.Children[0] as Grid;
                    if (headerPanel != null)
                    {
                        var nameText = headerPanel.Children[0] as TextBlock;
                        if (nameText != null && nameText.Text == name)
                        {
                            // 更新值
                            var valueText = headerPanel.Children[1] as TextBlock;
                            if (valueText != null)
                            {
                                valueText.Text = $"{(int)value}%";
                            }

                            // 更新进度条
                            var progressBarBorder = itemPanel.Children[1] as Border;
                            if (progressBarBorder != null && progressBarBorder.Child is Border progressBar)
                            {
                                progressBar.Width = (value / 100) * (Width - 20);
                                progressBar.Background = progressBarColor;
                            }

                            return;
                        }
                    }
                }
            }

            // 如果没有找到，添加新项目
            AddResourceItem(name, value, progressBarColor);
        }
    }
}