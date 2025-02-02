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
        // ҳ��ӳ���ֵ䣬���ڴ洢ҳ���ǩ��ҳ�����͵�ӳ���ϵ
        private readonly Dictionary<string, Type> _pageMap = new()
        {
            { "HexagramPlacementWin.Pages.SmallSixRen", typeof(Pages.SmallSixRen) }
        };

        // Win32���ڻ���������������ԭ�����ڽ���
        private WindowInteropHelper _windowInterop = null!;
        #endregion

        #region Properties
        // ��ǰ���ڵľ�̬����
        public static MainWindow CurrentWindow { get; private set; } = null!;
        #endregion

        #region Initialization
        // ���캯������ʼ�����ڲ����ú��ĳ�ʼ������
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindowCore();
        }

        // ���ĳ�ʼ�������������Ӿ�Ч���������ʹ��ڻ�����
        private void InitializeWindowCore()
        {
            CurrentWindow = this;  // ���õ�ǰ����ʵ��
            InitializeVisualSettings();  // ��ʼ���Ӿ�����
            InitializeNavigation();  // ��ʼ������
            InitializeWindowInterop();  // ��ʼ�����ڻ�����
        }

        // ��ʼ���Ӿ����ã��������ô��ڱ����ͱ�������
        private void InitializeVisualSettings()
        {
            SystemBackdrop = new MicaBackdrop();  // ���ñ���ΪMicaЧ��
            ExtendsContentIntoTitleBar = true;  // ��չ���ݵ�������
            SetTitleBar(AppTitleBar);  // �����Զ��������
            Title = Application.Current.Resources["AppTitle"] as string;  // ����Ӧ�ñ���
        }

        // ��ʼ���������ã������˵���ĳ�ʼ���͹ر��¼�����
        private void InitializeNavigation()
        {
            NavView.Loaded += (s, e) =>
            {
                // ѡ�е�����ͼ�ĵ�һ����
                NavView.SelectedItem = NavView.MenuItems.FirstOrDefault();

                // ���á����á��������
                if (NavView.SettingsItem is NavigationViewItem settingsItem)
                {
                    settingsItem.Content = "����";
                }
            };
            Closed += MainWindow_Closed;  // ���ڹر�ʱ�������������
        }

        // ��ʼ�����ڻ����������ô��ھ���ͳߴ�
        private void InitializeWindowInterop()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);  // ��ȡ���ھ��
            _windowInterop = new WindowInteropHelper(hwnd, 1400, 800);  // ��ʼ�����ڻ���������������С�ߴ�
        }
        #endregion

        #region Event Handlers
        // �����ڹر��¼����ͷŻ�������Դ
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _windowInterop?.Dispose();  // �ͷŴ��ڻ�������Դ
            Closed -= MainWindow_Closed;  // �Ƴ��ر��¼�����
        }

        // ��������ͼ��ѡ��仯�¼�
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var transition = new DrillInNavigationTransitionInfo();  // �������ɶ���

            if (args.IsSettingsSelected)
            {
                // ���ѡ����������򵼺�������ҳ��
                ContentFrame.Navigate(typeof(Pages.Settings), null, transition);
            }
            else if (args.SelectedItemContainer?.Tag is string tag)
            {
                // ����ѡ��ı�ǩ����ҳ�浼��
                NavigateByTag(tag, transition);
            }
        }

        // ����ҳ�浼������¼�
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // ���ݵ�ǰҳ�����õ�����ͼѡ�е���
            NavView.SelectedItem = ContentFrame.SourcePageType == typeof(Pages.Settings)
                ? NavView.SettingsItem
                : NavView.MenuItems.FirstOrDefault(n =>
                    (n as NavigationViewItem)?.Tag?.ToString() == e.SourcePageType.FullName)!;
        }
        #endregion

        #region Navigation Methods
        // ���ݱ�ǩ����ҳ�浼��
        private void NavigateByTag(string tag, NavigationTransitionInfo transitionInfo)
        {
            // ���ݱ�ǩ���Ҷ�Ӧ��ҳ�����ͣ������е���
            if (_pageMap.TryGetValue(tag, out Type? pageType))
            {
                ContentFrame.Navigate(pageType, null, transitionInfo);
            }
        }
        #endregion

        #region Win32 Interop Helper
        // ���ڴ���Win32���ڻ������ĸ�����
        private sealed class WindowInteropHelper : IDisposable
        {
            private const int WM_GETMINMAXINFO = 0x0024;  // ��ȡ��С/��󴰿���Ϣ��Ϣ
            private const int GWLP_WNDPROC = -4;  // ��ȡ���ڹ�����Ϣ

            private readonly IntPtr _hwnd;  // ���ھ��
            private readonly IntPtr _originalWndProc;  // ԭʼ���ڹ���
            private readonly IntPtr _newWndProcPtr;  // �µĴ��ڹ���ָ��
            private readonly WndProc _newWndProc;  // �µĴ��ڹ���ί��

            // �ṹ�壬���ڱ�ʾ���ڴ�С��λ�õ���Ϣ
            [StructLayout(LayoutKind.Sequential)]
            private struct POINT
            {
                public int X;
                public int Y;
                public POINT(int x, int y) => (X, Y) = (x, y);
            }

            // �ṹ�壬���ڱ�ʾ��С/�����Ϣ
            [StructLayout(LayoutKind.Sequential)]
            private struct MINMAXINFO
            {
                public POINT ptReserved;
                public POINT ptMaxSize;
                public POINT ptMaxPosition;
                public POINT ptMinTrackSize;
                public POINT ptMaxTrackSize;
            }

            // ���ڹ���ί�У����ڴ�������Ϣ
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            private delegate IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

            // ���캯������ʼ�����ڻ�����
            public WindowInteropHelper(IntPtr hwnd, int minWidth, int minHeight)
            {
                _hwnd = hwnd;
                _newWndProc = NewWindowProc;
                _newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
                _originalWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, _newWndProcPtr);

                // ������С���ڳߴ�
                MinWidth = minWidth;
                MinHeight = minHeight;
            }

            public int MinWidth { get; }
            public int MinHeight { get; }

            // �µĴ��ڹ��̷��������ڴ�������Ϣ
            private IntPtr NewWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
            {
                if (uMsg == WM_GETMINMAXINFO)
                {
                    // �޸Ĵ��ڵ���С�ߴ���Ϣ
                    var info = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    info.ptMinTrackSize = new POINT(MinWidth, MinHeight);
                    Marshal.StructureToPtr(info, lParam, true);
                }
                return CallWindowProc(_originalWndProc, hWnd, uMsg, wParam, lParam);
            }

            // �ͷŴ��ڻ�������Դ
            public void Dispose()
            {
                SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _originalWndProc);  // �ָ�ԭʼ���ڹ���
                GC.SuppressFinalize(this);
            }

            // Win32 API���������ô��ڹ���
            [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
            private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            // Win32 API�����ڵ��ô��ڹ���
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
        // ��ȡ��ǰ���ڵ�Ӧ�ô��ڶ���
        public AppWindow GetAppWindowForCurrentWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);  // ��ȡ���ھ��
            var myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);  // ��ȡ����ID
            return AppWindow.GetFromWindowId(myWndId);  // ���ݴ���ID��ȡӦ�ô��ڶ���
        }
        #endregion
    }
}
