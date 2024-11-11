using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.Input;
using Painto.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using Windows.Devices.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Painto
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToolBarWindow : Window
    {

        private Polyline _currentLine;
        public static bool _isDrawingMode = false;
        public static bool _isEraserMode = false;
        public static bool _computerMode = false;
        private const double EraserRadius = 15;

        // Pen Attribution 
        public static Windows.UI.Color penColor = Colors.Black;
        public static int penThickness = 2;
        

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

        internal static IntPtr hwnd;

        IReadOnlyList<PointerDevice> _devices;
        bool _isPenSupported;

        public ToolBarWindow()
        {
            this.InitializeComponent();
            hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);
            WinUIEx.HwndExtensions.SetAlwaysOnTop(hwnd, true);
            // ! IMPORTANT Click Through
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            //_ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            this.Title = "PaintoDrawWindow";
            this.AppWindow.SetIcon("Assets/painto_logo.ico");
            RemoveTitleBarAndBorder(hwnd);
            RemoveWindowShadow(hwnd);
            EnterFullScreenMode();

            _devices =  PointerDevice.GetPointerDevices();
            _isPenSupported = _devices.Any(pd => pd.PointerDeviceType == PointerDeviceType.Pen);

            LockScreen();
            //this.AppWindow.Move(new Windows.Graphics.PointInt32(0, 0));
        }

        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private Microsoft.UI.Windowing.AppWindow GetAppWindowForCurrentWindow()
        {
            WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);
        }

        // Fullscreen 
        private void EnterFullScreenMode()
        {
            var m_appWindow = GetAppWindowForCurrentWindow();
            m_appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
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

        // Draw
        // Draw 

        public Windows.UI.Color ConvertHexToColor(string hex)
        {
            // 去掉开头的 '#' 字符
            hex = hex.Replace("#", string.Empty);

            byte a = 255; // 默认为不透明
            byte r, g, b;

            if (hex.Length == 8) // 如果长度为8，表示ARGB
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }
            else // 默认为RGB
            {
                r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return Windows.UI.Color.FromArgb(a, r, g, b);
        }

        private void DrawingCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(DrawingCanvas).Position;

            if (_computerMode)
                return;

            if (e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                if (!_isEraserMode)
                {
                    _currentLine = new Polyline
                    {
                        Stroke = new SolidColorBrush(penColor),
                        StrokeThickness = penThickness,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    DrawingCanvas.Children.Add(_currentLine);
                    _isDrawingMode = true;
                    _currentLine.Points.Add(point);
                }
                else
                {
                    // 在橡皮擦模式下，开始擦除
                    _isDrawingMode = true;
                    RemoveIntersectingLines(point);
                }
            }
        }

        private void DrawingCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(DrawingCanvas).Position;

            if (_computerMode || !_isDrawingMode)
                return;

            if (e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                if (!_isEraserMode)
                {
                    // 继续绘图
                    var lastPoint = _currentLine.Points.LastOrDefault();
                    if (Distance(lastPoint, point) > 2.0)
                    {
                        _currentLine.Points.Add(point);
                    }
                }
                else
                {
                    // 继续擦除
                    RemoveIntersectingLines(point);
                }
            }
        }

        private void DrawingCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                _isDrawingMode = false;
            }
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        public void DeleteAllInk()
        {
            DrawingCanvas.Children.Clear();
        }



        private void RemoveIntersectingLines(Point position)
        {
            var linesToRemove = new List<Polyline>();
            foreach (var child in DrawingCanvas.Children.OfType<Polyline>())
            {
                if (IsLineIntersectingWithEraser(child, position))
                {
                    linesToRemove.Add(child);
                }
            }

            foreach (var line in linesToRemove)
            {
                DrawingCanvas.Children.Remove(line);
            }
        }

        private bool IsLineIntersectingWithEraser(Polyline line, Point eraserPosition)
        {
            foreach (var point in line.Points)
            {
                if (IsPointInEraserArea(point, eraserPosition))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPointInEraserArea(Point point, Point eraserPosition)
        {
            var distance = Math.Sqrt(Math.Pow(point.X - eraserPosition.X, 2) + Math.Pow(point.Y - eraserPosition.Y, 2));
            return distance < EraserRadius;
        }

        public static void UnlockScreen()
        {
            
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);
            // Click Through 
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | ~WS_EX_TRANSPARENT);
        }

        public static void LockScreen()
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);
            // Not Click Through
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

    }
}
