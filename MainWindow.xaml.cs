using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HexagramPlacementWin
{
    public sealed partial class MainWindow : Window
    {
        #region Fields
        // 页面映射字典，用于存储页面标签和页面类型的映射关系
        private readonly Dictionary<string, Type> _pageMap = new()
        {
            { "HexagramPlacementWin.Pages.SmallSixRen", typeof(Pages.SmallSixRen) }
        };

        // Win32窗口互操作对象，用于与原生窗口交互
        private WindowInteropHelper _windowInterop = null!;
        #endregion

        #region Properties
        // 当前窗口的静态属性
        public static MainWindow CurrentWindow { get; private set; } = null!;
        #endregion

        #region Initialization
        // 构造函数，初始化窗口并调用核心初始化方法
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindowCore();
        }

        // 核心初始化方法，设置视觉效果、导航和窗口互操作
        private void InitializeWindowCore()
        {
            CurrentWindow = this;  // 设置当前窗口实例
            InitializeVisualSettings();  // 初始化视觉设置
            InitializeNavigation();  // 初始化导航
            InitializeWindowInterop();  // 初始化窗口互操作
        }

        // 初始化视觉设置，包括设置窗口背景和标题栏等
        private void InitializeVisualSettings()
        {
            SystemBackdrop = new MicaBackdrop();  // 设置背景为Mica效果
            ExtendsContentIntoTitleBar = true;  // 扩展内容到标题栏
            SetTitleBar(AppTitleBar);  // 设置自定义标题栏
            Title = Application.Current.Resources["AppTitle"] as string;  // 设置应用标题
        }

        // 初始化导航设置，包括菜单项的初始化和关闭事件处理
        private void InitializeNavigation()
        {
            NavView.Loaded += (s, e) =>
            {
                // 选中导航视图的第一个项
                NavView.SelectedItem = NavView.MenuItems.FirstOrDefault();

                // 设置“设置”项的内容
                if (NavView.SettingsItem is NavigationViewItem settingsItem)
                {
                    settingsItem.Content = "设置";
                }
            };
            Closed += MainWindow_Closed;  // 窗口关闭时处理相关清理工作
        }

        // 初始化窗口互操作，设置窗口句柄和尺寸
        private void InitializeWindowInterop()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);  // 获取窗口句柄
            _windowInterop = new WindowInteropHelper(hwnd, 1400, 800);  // 初始化窗口互操作对象，设置最小尺寸
        }
        #endregion

        #region Event Handlers
        // 处理窗口关闭事件，释放互操作资源
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _windowInterop?.Dispose();  // 释放窗口互操作资源
            Closed -= MainWindow_Closed;  // 移除关闭事件处理
        }

        // 处理导航视图项选择变化事件
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var transition = new DrillInNavigationTransitionInfo();  // 导航过渡动画

            if (args.IsSettingsSelected)
            {
                // 如果选择了设置项，则导航到设置页面
                ContentFrame.Navigate(typeof(Pages.Settings), null, transition);
            }
            else if (args.SelectedItemContainer?.Tag is string tag)
            {
                // 根据选择的标签进行页面导航
                NavigateByTag(tag, transition);
            }
        }

        // 处理页面导航完成事件
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // 根据当前页面设置导航视图选中的项
            NavView.SelectedItem = ContentFrame.SourcePageType == typeof(Pages.Settings)
                ? NavView.SettingsItem
                : NavView.MenuItems.FirstOrDefault(n =>
                    (n as NavigationViewItem)?.Tag?.ToString() == e.SourcePageType.FullName)!;
        }
        #endregion

        #region Navigation Methods
        // 根据标签进行页面导航
        private void NavigateByTag(string tag, NavigationTransitionInfo transitionInfo)
        {
            // 根据标签查找对应的页面类型，并进行导航
            if (_pageMap.TryGetValue(tag, out Type? pageType))
            {
                ContentFrame.Navigate(pageType, null, transitionInfo);
            }
        }
        #endregion

        #region Win32 Interop Helper
        // 用于处理Win32窗口互操作的辅助类
        private sealed class WindowInteropHelper : IDisposable
        {
            private const int WM_GETMINMAXINFO = 0x0024;  // 获取最小/最大窗口信息消息
            private const int GWLP_WNDPROC = -4;  // 获取窗口过程消息

            private readonly IntPtr _hwnd;  // 窗口句柄
            private readonly IntPtr _originalWndProc;  // 原始窗口过程
            private readonly IntPtr _newWndProcPtr;  // 新的窗口过程指针
            private readonly WndProc _newWndProc;  // 新的窗口过程委托

            // 结构体，用于表示窗口大小和位置等信息
            [StructLayout(LayoutKind.Sequential)]
            private struct POINT
            {
                public int X;
                public int Y;
                public POINT(int x, int y) => (X, Y) = (x, y);
            }

            // 结构体，用于表示最小/最大信息
            [StructLayout(LayoutKind.Sequential)]
            private struct MINMAXINFO
            {
                public POINT ptReserved;
                public POINT ptMaxSize;
                public POINT ptMaxPosition;
                public POINT ptMinTrackSize;
                public POINT ptMaxTrackSize;
            }

            // 窗口过程委托，用于处理窗口消息
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

            // 构造函数，初始化窗口互操作
            public WindowInteropHelper(IntPtr hwnd, int minWidth, int minHeight)
            {
                _hwnd = hwnd;
                _newWndProc = NewWindowProc;
                _newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
                _originalWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, _newWndProcPtr);

                // 设置最小窗口尺寸
                MinWidth = minWidth;
                MinHeight = minHeight;
            }

            public int MinWidth { get; }
            public int MinHeight { get; }

            // 新的窗口过程方法，用于处理窗口消息
            private IntPtr NewWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
            {
                if (uMsg == WM_GETMINMAXINFO)
                {
                    // 修改窗口的最小尺寸信息
                    var info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    info.ptMinTrackSize = new POINT(MinWidth, MinHeight);
                    Marshal.StructureToPtr(info, lParam, true);
                }
                return CallWindowProc(_originalWndProc, hWnd, uMsg, wParam, lParam);
            }

            // 释放窗口互操作资源
            public void Dispose()
            {
                SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _originalWndProc);  // 恢复原始窗口过程
                GC.SuppressFinalize(this);
            }

            // Win32 API，用于设置窗口过程
            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
            private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            // Win32 API，用于调用窗口过程
            [DllImport("user32.dll")]
            private static extern IntPtr CallWindowProc(
                IntPtr lpPrevWndFunc,
                IntPtr hWnd,
                uint Msg,
                IntPtr wParam,
                IntPtr lParam);
        }
        #endregion

        #region Helper Methods
        // 获取当前窗口的应用窗口对象
        public AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);  // 获取窗口句柄
            var myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);  // 获取窗口ID
            return AppWindow.GetFromWindowId(myWndId);  // 根据窗口ID获取应用窗口对象
        }
        #endregion
    }
}
