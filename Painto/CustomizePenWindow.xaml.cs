using Microsoft.UI.Windowing;
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Painto.Modules;
using Windows.UI.WindowManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Painto
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CustomizePenWindow : Window
    {

        // Property
        private int thickness;
        private Color color;
        public PenData penData;
        public delegate void MyEventHandler(object sender, EventArgs e);
        public event MyEventHandler UpdatePenLayout;
        public event MyEventHandler SwitchBackToDrawingMode;
        private uint dpiWindow;

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

        public CustomizePenWindow(int thickness, Color color, PenData penData)
        {
            this.thickness = thickness;
            this.color = color;
            this.penData = penData; 
            this.InitializeComponent();
            Init();
        }

        private void Init()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinUIEx.HwndExtensions.SetAlwaysOnTop(hwnd, true);
            dpiWindow = GetDpiForWindow(hwnd);
            RemoveTitleBarAndBorder(hwnd);
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW & ~WS_EX_APPWINDOW);

            //RemoveWindowShadow(hwnd);
            AdaptWindowLocation();

            InitUI();
            
        }

        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        private void InitUI()
        {
            ThicknessAdjuster.Value = thickness;
            ColorPickerforPen.Color = color;
        }

        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public void AdaptWindowLocation()
        {

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            int screenHeight = displayArea.WorkArea.Height;
            int width = (int)(380 * dpiWindow / 96.0);
            int height = (int)(770 * dpiWindow / 96.0);
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(5, screenHeight - height, width, height));
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

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int thickness = (int)ThicknessAdjuster.Value;
            Color newColor = ColorPickerforPen.Color;
            penData.PenColor = newColor;
            penData.Thickness = thickness;
            penData.PenColorString = newColor.ToString();
            UpdatePenLayout?.Invoke(this, EventArgs.Empty);
            SwitchBackToDrawingMode?.Invoke(this, EventArgs.Empty);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchBackToDrawingMode?.Invoke(this, EventArgs.Empty);
        }
    }
}
