using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HexagramPlacementWin.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled; // 禁用缓存
            // 添加 Loaded 事件处理程序
            Loaded += Settings_Loaded;
            // 页面卸载事件
            Unloaded += Settings_Unloaded;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            // 控件加载完成后，执行初始化操作
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue("AppTheme", out var savedTheme))
            {
                SetThemeComboBoxSelection(savedTheme.ToString()!, ThemeMode);
            }
            else
            {
                App.SetTheme("Default");
                ThemeMode.SelectedIndex = 0;
            }
            ThemeMode.SelectionChanged += ThemeMode_SelectionChanged;
        }
        private void Settings_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeMode.SelectionChanged -= ThemeMode_SelectionChanged;
        }
        private void ThemeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTheme = ((ComboBoxItem)ThemeMode.SelectedItem)?.Tag?.ToString();
            if (selectedTheme != null)
            {
                App.SetTheme(selectedTheme);
                SaveThemePreference(selectedTheme);
            }
        }
        private static void SaveThemePreference(string theme)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["AppTheme"] = theme;
        }

        private static void SetThemeComboBoxSelection(string theme, ComboBox comboBox)
        {
            if (string.IsNullOrEmpty(theme)) return;
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag?.ToString() == theme);
            }
        }
    }
}
