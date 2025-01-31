using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HexagramPlacementWin
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var window = new MainWindow();
            window.Activate();
            ApplySavedTheme(); // 确保主题正确应用
        }

        private static void ApplySavedTheme()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("AppTheme", out var savedTheme))
            {
                SetTheme(savedTheme?.ToString() ?? "Default");

            }
        }

        public static void SetTheme(string theme)
        {
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));
            if (MainWindow.CurrentWindow != null)
            {
                var rootElement = MainWindow.CurrentWindow.Content as FrameworkElement;
                var mainWindow = MainWindow.CurrentWindow;
                var appWindow = mainWindow.GetAppWindowForCurrentWindow();
                var titleBar = appWindow.TitleBar;
                if (rootElement != null)
                {
                    switch (theme)
                    {
                        case "Light":
                            rootElement.RequestedTheme = ElementTheme.Light;
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                            break;
                        case "Dark":
                            rootElement.RequestedTheme = ElementTheme.Dark;
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                            break;
                        case "Default":
                            rootElement.RequestedTheme = ElementTheme.Default;
                            if (rootElement.RequestedTheme == ElementTheme.Dark)
                            {
                                titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                            }
                            break;
                    }
                }
            }
        }

        private Window? m_window;
    }
}
