﻿using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using WinUIEx;
using System.Diagnostics;
using Painto.Modules;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Painto
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Window
    {
        public ObservableCollection<DisplayInfo> Displays { get; } = new ObservableCollection<DisplayInfo>();
        private int selectedIndex = 0;
        public Settings()
        {
            this.InitializeComponent();

            this.Title = "Painto Setting";
            this.AppWindow.SetIcon("Assets/painto_logo.ico");

            // 获取窗口句柄
            IntPtr hwnd = WindowNative.GetWindowHandle(this);

            // 通过句柄获取 WindowId
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            // 获取 AppWindow 对象
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            Init();
            // 订阅 Closing 事件
            //appWindow.Closing += AppWindow_Closing;
        }

        //private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        //{
        //    // 处理关闭事件的逻辑
        //    this.Hide();
        //}

        public void Init()
        {
            // Setting 1: Monitor Related: Get All Monitors and Full Monitor
            GetAllDisplays();

            // Setting 2: UI related 
            SetupUI();
        }

        public void GetAllDisplays()
        {
            // 获取所有连接的显示器
            var displays = DisplayArea.FindAll();
            // 获取应用程序的本地设置容器
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // 从设置属性中获取MonitorIndex
            string MonitorIndex = localSettings.Values["Monitor"] as string;
            int monitorIndex = int.Parse(MonitorIndex);

            string MonitorFull = localSettings.Values["MonitorFull"] as string;
            int monitorFull = int.Parse(MonitorFull);
            bool _monitorFull = monitorFull != 0;

            for (int i = 0; i < displays.Count; i++) 
            {
                DisplayArea display = displays[i];
                // 获取显示器基本信息
                var isPrimary = display.IsPrimary;
                var workArea = display.WorkArea;
                bool isSelected = i == monitorIndex;
                //var dpi = display.GetDpi();

                SolidColorBrush bg = isSelected
                    ? new SolidColorBrush(Colors.LightBlue)
                    : new SolidColorBrush(Colors.LightGray);

                Displays.Add(new DisplayInfo
                {
                    ID = i+1,
                    DisplayId = display.DisplayId.Value,
                    IsPrimary = isPrimary,
                    WorkArea = workArea,
                    BackgroundColor = bg,
                });

                // 输出调试信息
                //Debug.WriteLine($"Display {display.DisplayId}");
                //Debug.WriteLine($"  Primary: {isPrimary}");
                //Debug.WriteLine($"  Resolution: {workArea.Width}x{workArea.Height}");
                //Debug.WriteLine($"  Position: ({workArea.X}, {workArea.Y})");
                //Debug.WriteLine($"  DPI: {dpi}");
            }
            
            DisplayGridView.SelectedIndex = monitorIndex;

            if (_monitorFull)
            {
                FullMonitorStatus.IsOn = true;
            }
        }

        public void SetupUI()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string toolbarCollapse = localSettings.Values["IsToolBarCollapse"] as string;
            int istoolbarCollapse = int.Parse(toolbarCollapse);
            bool istoolbarCollapsed = istoolbarCollapse != 0;
            if (istoolbarCollapsed)
            {
                ToolBarCollapsed.IsOn = true;
            }
        }

        public DisplayArea GetCurrentWindowDisplay(Window window)
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            return DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
        }

        private void SetMonitorBtn_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DisplayGridView.SelectedIndex;
            bool isFullMonitor = FullMonitorStatus.IsOn;
            App.m_window.MoveWindowFromMonitor(selectedIndex, isFullMonitor);
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Monitor"] = selectedIndex.ToString();

            if (isFullMonitor)
            {
                localSettings.Values["MonitorFull"] = "1";
            } else
            {
                localSettings.Values["MonitorFull"] = "0";
            }

            for (int i = 0; i < Displays.Count; i++)
            {
                if (Displays[i].ID - 1 == selectedIndex)
                {
                    Displays[i].BackgroundColor = new SolidColorBrush(Colors.LightBlue);
                }
                else
                {
                    Displays[i].BackgroundColor = new SolidColorBrush(Colors.LightGray);
                }
            }

            
        }

        private void ToolBarCollapsed_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                bool isOn = toggleSwitch.IsOn;
                if (isOn)
                {
                    App.m_window.IsToolBarCollapse = true;
                    localSettings.Values["IsToolBarCollapse"] = "1";
                    
                } else
                {
                    App.m_window.IsToolBarCollapse = false;
                    localSettings.Values["IsToolBarCollapse"] = "0";
                }

                App.m_window.SetCollapsed(isOn);
            }
        }

        // Test Method
        //private void FullMonitorStatus_Toggled(object sender, RoutedEventArgs e)
        //{
        //    var toggleSwitch = sender as ToggleSwitch;
        //    if (toggleSwitch != null)
        //    {
        //        bool isOn = toggleSwitch.IsOn;  // 获取开关的状态
        //                                        // 处理状态变化
        //        if (isOn)
        //        {
        //            // 开关被打开
        //            App.m_window.SetToolBarFullMonitor();
        //        }
        //        else
        //        {
        //            // 开关被关闭
        //            Console.WriteLine("Toggle is OFF");
        //        }
        //    }
        //}
    }
}
