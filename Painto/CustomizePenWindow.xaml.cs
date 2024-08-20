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

        public CustomizePenWindow(int thickness, Color color)
        {
            this.thickness = thickness;
            this.color = color;
            this.InitializeComponent();
            Init();
        }

        private void Init()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinUIEx.HwndExtensions.SetAlwaysOnTop(hwnd, true);
            RemoveTitleBarAndBorder(hwnd);

            //RemoveWindowShadow(hwnd);
            AdaptWindowLocation();

            InitUI();
            
        }

        private void InitUI()
        {
            ThicknessAdjuster.Value = thickness;
            ColorPickerforPen.Color = color;
        }

        public void AdaptWindowLocation()
        {

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            int screenHeight = displayArea.WorkArea.Height;
            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(5, screenHeight - 770, 380, 770));
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

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
