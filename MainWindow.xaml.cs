using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace HexagramPlacementWin
{
    public sealed partial class MainWindow : Window
    {
        private readonly AppWindow m_AppWindow;
        public static MainWindow? CurrentWindow { get; private set; }

        public MainWindow()
        {
            this.InitializeComponent();
            SetCurrentWindow(this);

            m_AppWindow = GetAppWindowForCurrentWindow();

            // 设置云母背景
            SystemBackdrop = new MicaBackdrop();
            // 自定义标题栏
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            // 标题
            m_AppWindow.Title = Application.Current.Resources["AppTitle"] as string;

            // 明确订阅事件
            NavView.SelectionChanged += NavView_SelectionChanged;
            NavView.Loaded += NavView_Loaded;

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // 取消事件订阅
            NavView.SelectionChanged -= NavView_SelectionChanged;
            NavView.Loaded -= NavView_Loaded;
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            SetSettingsItemContent();
            NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        }

        public AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var transitionInfo = new DrillInNavigationTransitionInfo();

            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(Pages.Settings), null, transitionInfo);
            }
            else if (args.SelectedItemContainer != null && args.SelectedItemContainer.Tag != null)
            {
                string? tag = args.SelectedItemContainer.Tag?.ToString();
                switch (tag)
                {
                    case "HexagramPlacementWin.Pages.SmallSixRen":
                        ContentFrame.Navigate(typeof(Pages.SmallSixRen), null, transitionInfo);
                        break;
                }
            }
        }

        private void SetSettingsItemContent()
        {
            if (NavView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Content = "设置";
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(Pages.Settings))
            {
                NavView.SelectedItem = NavView.SettingsItem;
            }
            else
            {
                NavView.SelectedItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(n => n.Tag?.ToString() == ContentFrame.SourcePageType.FullName);
            }
        }

        public static void SetCurrentWindow(MainWindow window)
        {
            CurrentWindow = window;
        }
    }

    // 使用 P/Invoke 而非 LibraryImport 来实现窗口最小尺寸设置
    internal static class WindowNativeMethods
    {
        public const int WM_GETMINMAXINFO = 0x0024;

        // P/Invoke 调用 SetWindowPos（不使用不安全代码）
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // 定义结构体 POINT 和 MINMAXINFO
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        // 定义常量，用于设置窗口位置和大小
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
    }

    internal static class WindowNativeHelper
    {
        // 设置窗口的最小尺寸
        public static void AdjustMinSize(ref WindowNativeMethods.MINMAXINFO minMaxInfo)
        {
            minMaxInfo.ptMinTrackSize = new WindowNativeMethods.POINT(1200, 800);
        }
    }
}
