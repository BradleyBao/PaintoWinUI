using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using System.Collections.ObjectModel;
using Painto.Modules;
using Windows.System;
using Windows.Storage;
using Newtonsoft.Json;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display; 

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Painto
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        // Child Window - Toolbar 
        private ToolBarWindow _toolbarWindow;
        private int _currentWindowWidth;
        private uint dpiWindow;

        // Transparent + Click Through 
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;

        private const int WS_CAPTION = 0x00C00000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;

        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;
        private const int DWMNCRP_DISABLED = 1;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("dwmapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);


        public ObservableCollection<PenData> PenItems { get; set; }


        public MainWindow()
        {
            this.InitializeComponent();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinUIEx.HwndExtensions.SetAlwaysOnTop(hwnd, true);
            dpiWindow = GetDpiForWindow(hwnd);
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);

            // ! IMPORTANT Click Through
            //_ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            //_ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            this.Title = "Painto";
            this.AppWindow.SetIcon("Assets/painto_logo.ico");

            RemoveTitleBarAndBorder(hwnd);
            RemoveWindowShadow(hwnd);

            //EnterFullScreenMode();

            Init();

            // Create Toolbar 
        }

        private void Init()
        {
            MajorFunctionControl.SelectedIndex = 1;
            var selectedItem = (GridViewItem)MajorFunctionControl.ContainerFromIndex(1);
            selectedItem?.Focus(FocusState.Programmatic);

            penControl.DisableWindowControl += DisableToolBarControl;
            penControl.SaveData += PenControl_SaveData;
            penControl.SwitchBackDrawControl += PenControl_SwitchBackDrawControl;
            ControlPanel.LayoutUpdated += ControlPanel_LayoutUpdated;
            

            InitWindow();
            SourceInitialized();
            InitPens();
            //AdaptWindowLocation();
        }

        private void PenControl_SwitchBackDrawControl(object sender, EventArgs e)
        {
            MajorFunctionControl.SelectedIndex = 1;
            var selectedItem = (GridViewItem)MajorFunctionControl.ContainerFromIndex(1);
            selectedItem?.Focus(FocusState.Programmatic);
            ToolBarWindow._computerMode = false;
            ToolBarWindow._isEraserMode = false;
            ToolBarWindow.LockScreen();
        }

        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);


        private void PenControl_SaveData(object sender, EventArgs e)
        {
            SavePenItems(penControl.ItemsSource);
        }

        private void InitWindow()
        {
            _toolbarWindow = new ToolBarWindow();
            IntPtr mainHwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            IntPtr toolbarHwnd = WinRT.Interop.WindowNative.GetWindowHandle(_toolbarWindow);


            // 将 ToolBarWindow 设置为 MainWindow 的子窗体
            //SetOwner(toolbarHwnd, mainHwnd);
            _toolbarWindow.Activate();
        }

        private void DisableToolBarControl(object sender, EventArgs e)
        {
            // 强制切换至电脑模式
            MajorFunctionControl.SelectedIndex = 0;
            var selectedItem = (GridViewItem)MajorFunctionControl.ContainerFromIndex(0);
            selectedItem?.Focus(FocusState.Programmatic);
            ToolBarWindow._computerMode = true;
            ToolBarWindow.UnlockScreen();
            
        }

        private void RequestRestoreWindow(object sender, EventArgs e)
        {
            AdaptWindowLocation();
        }

        private void ControlPanel_LayoutUpdated(object sender, object e)
        {
            if (ControlPanel.ActualWidth > 0 && ControlPanel.ActualHeight > 0)
            {
                AdaptWindowLocation();
            }
        }

        public void AdaptWindowLocation()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            int screenHeight = displayArea.WorkArea.Height;

            // 根据 DPI 更改窗口大小
            int controlPanelWidth = (int)(ControlPanel.ActualWidth * dpiWindow / 96.0);
            int controlPanelHeight = (int)(ControlPanel.ActualHeight * dpiWindow / 96.0); 
            //this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(5 + PenItems.Count, screenHeight, controlPanelWidth, controlPanelHeight));
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(5, screenHeight, controlPanelWidth, controlPanelHeight));
        }

        public void AdaptWindowLocation(int height)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            int screenHeight = displayArea.WorkArea.Height;
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(5 + PenItems.Count, screenHeight - height, 300 + PenItems.Count * 15, height));
        }

        private void SourceInitialized()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            IntPtr toolbarHwnd = WinRT.Interop.WindowNative.GetWindowHandle(_toolbarWindow);

            // Set Ownership
            SetWindowLong(hwnd, WindowLongFlags.GWL_HWNDPARENT, toolbarHwnd);
        }

        private Microsoft.UI.Windowing.AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);
        }

        // Fullscreen 
        private void EnterFullScreenMode()
        {
            var m_appWindow = GetAppWindowForCurrentWindow();
            m_appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }

        //[DllImport("user32.dll", SetLastError = false)]
        //private static extern IntPtr SetParent(IntPtr child, IntPtr parent);


        // Set Ownership

        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern IntPtr SetWindowLong(IntPtr hWnd, WindowLongFlags nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongFlags nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, WindowLongFlags nIndex, IntPtr dwNewLong);

        private static IntPtr SetWindowLong(IntPtr hWnd, WindowLongFlags nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) // Check if we're running in a 64-bit environment
            {
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            }
            else // 32-bit environment
            {
                return SetWindowLong32(hWnd, nIndex, dwNewLong);
            }
        }

        private enum WindowLongFlags
        {
            GWL_HWNDPARENT = -8
        }

        internal void InitPens()
        {
            PenItems = LoadPenItems();

            // If not empty 
            if (PenItems.Count <= 0)
            {
                // Default Value
                PenItems = new ObservableCollection<PenData>
                {
                    new PenData { PenColor = Colors.Black, Thickness = 5, penType = "Normal", Icon = "\uEE56", PenColorString = Colors.Black.ToString()},
                    new PenData { PenColor = Color.FromArgb(30,115,199,255), Thickness = 5, penType = "Normal", Icon = "\uEE56", PenColorString = Color.FromArgb(30,144,255,255).ToString()},
                    new PenData { PenColor = Color.FromArgb(235,59,74,255), Thickness = 5, penType = "Normal", Icon = "\uEE56", PenColorString = Color.FromArgb(253,230,224,255).ToString()}
                };
            }

            penControl.ItemsSource = PenItems;
            ToolBarWindow.penColor = PenItems[0].PenColor;
            ToolBarWindow.penThickness = PenItems[0].Thickness;
            SavePenItems(PenItems);
        }

        public void SavePenItems(ObservableCollection<PenData> penItems)
        {
            // 序列化 ObservableCollection<PenData> 对象为 JSON 字符串
            string penItemsJson = JsonConvert.SerializeObject(penItems);

            // 获取应用程序的本地设置容器
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // 存储 JSON 字符串到设置属性中
            localSettings.Values["PenItems"] = penItemsJson;
        }

        public ObservableCollection<PenData> LoadPenItems()
        {
            // 获取应用程序的本地设置容器
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // 从设置属性中获取 JSON 字符串
            string penItemsJson = localSettings.Values["PenItems"] as string;

            // 如果 JSON 字符串存在，则反序列化为 ObservableCollection<PenData> 对象
            if (!string.IsNullOrEmpty(penItemsJson))
            {
                var penItems = JsonConvert.DeserializeObject<ObservableCollection<PenData>>(penItemsJson);
                return penItems;
            }

            // 如果没有存储的 PenItems 数据，则返回一个空的集合或 null
            return new ObservableCollection<PenData>();
        }

        private void RemoveTitleBarAndBorder(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_BORDER | WS_DLGFRAME);
            SetWindowLong(hwnd, GWL_STYLE, style);
        }

        private void RemoveWindowShadow(IntPtr hwnd)
        {
            int value = DWMNCRP_DISABLED;
            DwmSetWindowAttribute(hwnd, DWMWA_NCRENDERING_POLICY, ref value, sizeof(int));

            value = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_TRANSITIONS_FORCEDISABLED, ref value, sizeof(int));
        }

        private void AddNewPen()
        {
            PenData new_penData = new PenData
            {
                PenColor = Colors.Black,
                Thickness = 5,
                penType = "Normal",
                Icon = "\uEE56",
                PenColorString = Colors.Black.ToString()
            };
            PenItems.Add(new_penData);
            penControl.ItemsSource = PenItems;
            SavePenItems(PenItems); 
        }

        // Win32 API 常量
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_NOREDRAW = 0x0080;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void RootTool_SelectItem(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FontIcon clickedItem)
            {
                // 处理点击事件，并访问 tappedItem 对象
                //var deviceName = tappedItem.deviceName;
                //var deviceIP = tappedItem.deviceIP;
                // 执行其他操作
                switch (clickedItem.Tag)
                {
                    case "Eraser":
                        ToolBarWindow._isEraserMode = true;
                        if (ToolBarWindow._computerMode)
                        {
                            ToolBarWindow.LockScreen();
                            ToolBarWindow._computerMode = false;
                        }
                        break;

                    case "DrawMode":
                        ToolBarWindow._isEraserMode = false;
                        if (ToolBarWindow._computerMode)
                        {
                            ToolBarWindow.LockScreen();
                            ToolBarWindow._computerMode = false;
                        }
                        
                        break;

                    case "ComputerMode":
                        if (!ToolBarWindow._computerMode)
                        {
                            ToolBarWindow._computerMode = true;
                            ToolBarWindow.UnlockScreen();
                        }
                        
                        break;

                    default:
                        ToolBarWindow._isEraserMode = false;
                        ToolBarWindow._computerMode = false;
                        break;
                }
                
            }
        }
        private delegate void CloseAppDelegate();

        private void SubFunctionControl_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FontIcon clickedItem)
            {
                switch (clickedItem.Tag)
                {
                    case "AddPen":
                        AddNewPen();
                        break;

                    case "CloseApp":
                        APPShutDown();
                        break;

                    default:
                        
                        break;
                }
            }
        }

        // ShutDown: Close first then shutdown
        private void APPShutDown()
        {
            this.Close();
            App.Current.Exit();
        }
    }
}
