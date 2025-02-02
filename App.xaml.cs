using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;

namespace HexagramPlacementWin
{
    // 应用程序类，继承自 Application
    public partial class App : Application
    {
        // 构造函数，初始化应用程序组件
        public App()
        {
            InitializeComponent();
        }

        // 应用程序启动时触发的事件，初始化窗口并激活
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // 创建主窗口实例
            var window = new MainWindow();
            window.Activate();  // 激活并显示主窗口
            ApplySavedTheme();  // 应用已保存的主题设置
        }

        // 从本地设置中获取并应用保存的主题
        private static void ApplySavedTheme()
        {
            // 获取应用的本地设置
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // 尝试获取保存的主题值，如果存在，则应用该主题
            if (localSettings.Values.TryGetValue("AppTheme", out object? savedTheme))
            {
                SetTheme(savedTheme.ToString());  // 设置应用程序主题
            }
        }

        // 设置应用程序主题，根据传入的主题字符串应用主题
        public static void SetTheme(string? theme)
        {
            // 获取当前窗口的根元素（FrameworkElement）
            if (MainWindow.CurrentWindow?.Content is FrameworkElement rootElement)
            {
                // 根据传入的主题字符串解析并设置应用程序的主题
                rootElement.RequestedTheme = ParseTheme(theme ?? "Default");

                // 更新标题栏颜色，以匹配当前的主题
                UpdateTitleBarColor(MainWindow.CurrentWindow, rootElement.ActualTheme);
            }
        }

        // 解析主题字符串并返回相应的 ElementTheme 枚举值
        private static ElementTheme ParseTheme(string theme) => theme switch
        {
            "Light" => ElementTheme.Light,  // 如果主题为 "Light"，返回 Light 主题
            "Dark" => ElementTheme.Dark,    // 如果主题为 "Dark"，返回 Dark 主题
            _ => ElementTheme.Default       // 默认主题
        };

        // 更新标题栏颜色，确保标题栏颜色与应用程序的主题一致
        private static void UpdateTitleBarColor(MainWindow mainWindow, ElementTheme actualTheme)
        {
            // 获取当前窗口的标题栏
            var titleBar = mainWindow.GetAppWindowForCurrentWindow().TitleBar;

            // 根据实际的主题设置标题栏按钮的前景色
            titleBar.ButtonForegroundColor = actualTheme switch
            {
                ElementTheme.Dark => Microsoft.UI.Colors.White,  // 如果是 Dark 主题，按钮前景色为白色
                ElementTheme.Light => Microsoft.UI.Colors.Black, // 如果是 Light 主题，按钮前景色为黑色
                _ => Application.Current.RequestedTheme == ApplicationTheme.Dark
                    ? Microsoft.UI.Colors.White  // 如果是应用程序的暗黑模式，按钮前景色为白色
                    : Microsoft.UI.Colors.Black  // 否则按钮前景色为黑色
            };
        }
    }
}
