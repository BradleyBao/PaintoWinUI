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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Painto
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        private Polyline _currentLine;
        private bool _isDrawingMode = false;
        private bool _isEraserMode = false;
        private bool _computerMode = false;
        private const double EraserRadius = 15;

        // Child Window - Toolbar 
        private Window _toolbarWindow;

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
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);

            // ! IMPORTANT Click Through
            //_ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            //_ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);

            RemoveTitleBarAndBorder(hwnd);
            RemoveWindowShadow(hwnd);

            EnterFullScreenMode();

            InitPens();

            // Create Toolbar 
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

        internal void InitPens()
        {
            PenItems = new ObservableCollection<PenData>
            {
                new PenData { PenColor = Colors.Black.ToString(), Thickness = 5, penType = "Normal", Icon = "\uEE56"},
                new PenData { PenColor = Colors.Yellow.ToString(), Thickness = 5, penType = "Normal", Icon = "\uEE56" }
            };
            penControl.ItemsSource = PenItems;
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

        private void DrawingCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(DrawingCanvas).Position;

            // 在计算机模式下不进行任何绘画或擦除操作
            if (_computerMode)
                return;

            // 在非橡皮擦模式下按下左键开始绘画
            if (!_isEraserMode && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                _currentLine = new Polyline
                {
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2
                };
                DrawingCanvas.Children.Add(_currentLine);
                _isDrawingMode = true;
                _currentLine.Points.Add(point);
            }
            // 在橡皮擦模式下按下左键开始擦除
            else if (_isEraserMode && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                RemoveIntersectingLines(point);
            }
        }

        private void DrawingCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(DrawingCanvas).Position;

            // 在计算机模式下不进行任何绘画或擦除操作
            if (_computerMode)
                return;

            // 绘画模式下按下左键绘制线条
            if (_isDrawingMode && !_isEraserMode && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                _currentLine.Points.Add(point);
            }

            // 橡皮擦模式下按住左键移动时进行擦除
            if (_isEraserMode && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {

                // 移除与橡皮擦重叠的线条部分
                RemoveIntersectingLines(point);
            }
        }

        private void DrawingCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isEraserMode && e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed)
            {
                _isDrawingMode = false;
            }
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

        internal void UnlockScreen()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinUIEx.HwndExtensions.SetAlwaysOnTop(hwnd, true);
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        internal void LockScreen()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinUIEx.HwndExtensions.SetAlwaysOnTop(hwnd, true);
            int extendedStyle = GetWindowLong(hwnd, GWL_STYLE);
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | ~WS_EX_TRANSPARENT);
        }

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
                        _isEraserMode = true;
                        if (_computerMode)
                        {
                            LockScreen();
                            _computerMode = false;
                        }
                        break;

                    case "DrawMode":
                        _isEraserMode = false;
                        if (_computerMode)
                        {
                            LockScreen();
                            _computerMode = false;
                        }
                        
                        break;

                    case "ComputerMode":
                        if (!_computerMode)
                        {
                            _computerMode = true;
                            UnlockScreen();
                        }
                        
                        break;

                    default:
                        _isEraserMode = false;
                        _computerMode = false;
                        break;
                }
                
            }
        }
    }
}
