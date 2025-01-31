using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using YiJingFramework.Nongli.Lunar;
using YiJingFramework.EntityRelations.EntityStrings.Extensions;
using YiJingFramework.PrimitiveTypes;
using YiJingFramework.EntityRelations.EntityStrings.Conversions;
using YiJingFramework.EntityRelations.WuxingRelations;
using YiJingFramework.EntityRelations.WuxingRelations.Extensions;
using YiJingFramework.EntityRelations.EntityCharacteristics.Extensions;
using System.Diagnostics;

namespace HexagramPlacementWin.Pages
{
    public sealed partial class SmallSixRen : Page
    {
        private Brush originalBorderBrush;
        private DispatcherTimer _timer;
        private bool useCustomTime;
        private bool isValid = true;
        private bool isCalculated;
        private DateTime customDateTime;

        public SmallSixRen()
        {
            this.InitializeComponent();

            InitializeControls();
            this.Loaded += SmallSixRen_Loaded;
            this.Unloaded += SmallSixRen_Unloaded;
            MonthTextBox.TextChanged += TextBox_TextChanged;
            DayTextBox.TextChanged += TextBox_TextChanged;
            HourTextBox.TextChanged += TextBox_TextChanged;
        }

        private void SmallSixRen_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayCurrentDateTime();
            StartTimer();
        }

        private void SmallSixRen_Unloaded(object sender, RoutedEventArgs e)
        {
            MonthTextBox.TextChanged -= TextBox_TextChanged;
            DayTextBox.TextChanged -= TextBox_TextChanged;
            HourTextBox.TextChanged -= TextBox_TextChanged;
            StopTimer();
        }

        private void InitializeControls()
        {
            if (MonthTextBox != null)
            {
                originalBorderBrush = MonthTextBox.BorderBrush;
            }
        }

        private void StartTimer()
        {
            StopTimer(); // 确保先停止之前的计时器

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerElapsed;
            _timer.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerElapsed;
                _timer = null;
            }
        }

        private void OnTimerElapsed(object sender, object e)
        {
            if (!useCustomTime)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    DisplayCurrentDateTime();
                });
            }
        }


        private void PalaceModeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (PalaceModeToggleSwitch.IsOn && MonthTextBox != null)
            {
                MonthTextBox.IsEnabled = true;
            }
            else if (MonthTextBox != null)
            {
                MonthTextBox.IsEnabled = false;
                MonthTextBox.Text = string.Empty;
            }
        }

        private void TimeModeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                useCustomTime = toggleSwitch.IsOn;
                if (useCustomTime)
                {
                    StopTimer();
                }
                else
                {
                    StartTimer();
                }
            }
        }

        private async void CurrentDateTimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (useCustomTime)
            {
                DateTime now = DateTime.Now;

                DatePicker datePicker = new()
                {
                    Date = new DateTimeOffset(now.Date)
                };
                TimePicker timePicker = new()
                {
                    Time = now.TimeOfDay
                };

                ContentDialog dialog = new()
                {
                    Title = "选择自定义时间",
                    XamlRoot = this.XamlRoot,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "选择日期：" ,Margin=new Thickness(0,0,0,5)},
                            datePicker,
                            new TextBlock { Text = "选择时间：" ,Margin=new Thickness(0,10,0,5)},
                            timePicker
                        }
                    },
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    RequestedTheme = ((FrameworkElement)this.Content).ActualTheme // 设置对话框的主题
                };

                dialog.PrimaryButtonClick += (s, args) =>
                {
                    customDateTime = new DateTime(DateTime.Now.Year, datePicker.Date.Month, datePicker.Date.Day, timePicker.Time.Hours, timePicker.Time.Minutes, 0);
                    DisplayCurrentDateTime(); // 更新显示
                };

                await dialog.ShowAsync();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                int selectionStart = textBox.SelectionStart;
                string originalText = textBox.Text;
                string filteredText = FilterNonNumeric(originalText);

                if (originalText != filteredText)
                {
                    textBox.Text = filteredText;
                    textBox.SelectionStart = selectionStart - (originalText.Length - filteredText.Length);
                }
            }

            if (isCalculated)
            {
                isCalculated = false;
                StartTimer();
            }
        }

        private static string FilterNonNumeric(string input)
        {
            return new string(input.Where(char.IsDigit).ToArray());
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!isValid)
            {
                RestoreAllTextBoxStylesAndHideError();
            }
        }

        private void RestoreAllTextBoxStylesAndHideError()
        {
            RestoreInitialTextBoxStyle(MonthTextBox);
            RestoreInitialTextBoxStyle(DayTextBox);
            RestoreInitialTextBoxStyle(HourTextBox);
            ErrorMessageTextBlock.Text = string.Empty;
            isValid = true;
        }

        private void RestoreInitialTextBoxStyle(TextBox textBox)
        {
            if (textBox != null)
            {
                textBox.BorderBrush = originalBorderBrush;
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            isValid = true; // 重置 isValid 状态
            ErrorMessageTextBlock.Text = string.Empty;

            if (PalaceModeToggleSwitch.IsOn && string.IsNullOrWhiteSpace(MonthTextBox.Text))
            {
                isValid = false;
                MarkTextBoxAsInvalid(MonthTextBox);
            }
            else
            {
                RestoreInitialTextBoxStyle(MonthTextBox);
            }

            if (string.IsNullOrWhiteSpace(DayTextBox.Text))
            {
                isValid = false;
                MarkTextBoxAsInvalid(DayTextBox);
            }
            else
            {
                RestoreInitialTextBoxStyle(DayTextBox);
            }

            if (string.IsNullOrWhiteSpace(HourTextBox.Text))
            {
                isValid = false;
                MarkTextBoxAsInvalid(HourTextBox);
            }
            else
            {
                RestoreInitialTextBoxStyle(HourTextBox);
            }

            if (!isValid)
            {
                ErrorMessageTextBlock.Text = "请填写缺失项";
                return;
            }

            if (!isCalculated)
            {
                StopTimer();
                CalculateResult();
                isCalculated = true;
            }
        }

        private static void MarkTextBoxAsInvalid(TextBox textBox)
        {
            textBox.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
        }

        private DateTime GetCurrentDateTime()
        {
            return useCustomTime ? customDateTime : DateTime.Now;
        }

        private static LunarDateTime GetLunarDateTime(DateTime dateTime)
        {
            return LunarDateTime.FromGregorian(dateTime);
        }

        private static Dizhi GetCurrentShiDizhi(LunarDateTime lunar)
        {
            return lunar.Shi;
        }

        private void DisplayCurrentDateTime()
        {
            var now = GetCurrentDateTime();
            LunarDateTime lunar = GetLunarDateTime(now);

            CurrentDateTimeButton.Content = string.Format(
                "新历: {0:yyyy-MM-dd HH:mm:ss}\n农历: {1:C}年{2}{3}月{4}日\n时辰: {5:C}",
                now, lunar.Nian, lunar.IsRunyue ? "闰" : "平", lunar.Yue, lunar.Ri, lunar.Shi);
        }

        private void CalculateResult()
        {
            LiuLianMarks.Text = string.Empty;
            SuXiMarks.Text = string.Empty;
            ChiKouMarks.Text = string.Empty;
            DaAnMarks.Text = string.Empty;
            KongWangMarks.Text = string.Empty;
            XiaoJiMarks.Text = string.Empty;

            int month = int.TryParse(MonthTextBox?.Text, out int parsedMonth) ? parsedMonth : 0;
            int day = int.TryParse(DayTextBox?.Text, out int parsedDay) ? parsedDay : 0;
            int hour = int.TryParse(HourTextBox?.Text, out int parsedHour) ? parsedHour : 0;

            if (!PalaceModeToggleSwitch.IsOn)
            {
                month = 0;
            }

            int[] steps = { month, day, hour };
            string[] positions = { "大安", "留连", "速喜", "赤口", "小吉", "空亡" };

            int currentIndex = 0; // 记录当前索引
            int shiGongIndex = -1; // 初始化时宫索引

            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] == 0) continue;

                int stepsCount = steps[i] % 6;
                if (stepsCount == 0) stepsCount = 6;
                currentIndex = (currentIndex + stepsCount - 1) % 6;

                if (i == 2) // 记录时宫索引
                {
                    shiGongIndex = currentIndex;
                }

                switch (positions[currentIndex])
                {
                    case "大安":
                        DaAnMarks.Text += i == 0 ? "月" : i == 1 ? "日" : "时";
                        break;
                    case "留连":
                        LiuLianMarks.Text += i == 0 ? "月" : i == 1 ? "日" : "时";
                        break;
                    case "速喜":
                        SuXiMarks.Text += i == 0 ? "月" : i == 1 ? "日" : "时";
                        break;
                    case "赤口":
                        ChiKouMarks.Text += i == 0 ? "月" : i == 1 ? "日" : "时";
                        break;
                    case "小吉":
                        XiaoJiMarks.Text += i == 0 ? "月" : i == 1 ? "日" : "时";
                        break;
                    case "空亡":
                        KongWangMarks.Text += i == 0 ? "月" : i == 1 ? "日" : "时";
                        break;
                }
            }

            // 获取当前时间的地支
            LunarDateTime lunar = GetLunarDateTime(GetCurrentDateTime());
            Dizhi currentShiDizhi = GetCurrentShiDizhi(lunar);

            // 排地支
            var dizhiOrder = GenerateDizhiOrder(currentShiDizhi, shiGongIndex);
            DaAnDizhi.Text = dizhiOrder[0].ToString("C");
            LiuLianDizhi.Text = dizhiOrder[1].ToString("C");
            SuXiDizhi.Text = dizhiOrder[2].ToString("C");
            ChiKouDizhi.Text = dizhiOrder[3].ToString("C");
            XiaoJiDizhi.Text = dizhiOrder[4].ToString("C");
            KongWangDizhi.Text = dizhiOrder[5].ToString("C");

            // 排六亲
            var liuqinOrder = GenerateLiuqinOrder(dizhiOrder, currentShiDizhi, shiGongIndex);
            DaAnLiuqin.Text = liuqinOrder[0];
            LiuLianLiuqin.Text = liuqinOrder[1];
            SuXiLiuqin.Text = liuqinOrder[2];
            ChiKouLiuqin.Text = liuqinOrder[3];
            XiaoJiLiuqin.Text = liuqinOrder[4];
            KongWangLiuqin.Text = liuqinOrder[5];
        }

        private static Dizhi[] GenerateDizhiOrder(Dizhi currentShiDizhi, int shiGongIndex)
        {
            Dizhi[] dizhis = new Dizhi[6];
            dizhis[shiGongIndex] = currentShiDizhi; // 时宫地支设为当前时辰地支

            for (int i = 1; i < 6; i++)
            {
                int index = (currentShiDizhi.Index + i * 2 - 1) % 12;
                dizhis[(shiGongIndex + i) % 6] = Dizhi.FromIndex((index + 1) % 12);
            }

            return dizhis;
        }

        private static string[] GenerateLiuqinOrder(Dizhi[] dizhiOrder, Dizhi currentShiDizhi, int shiGongIndex)
        {
            var liuqinNames = new string[6];
            for (int i = 0; i < 6; i++)
            {
                if (i == shiGongIndex) // 时宫所落的六亲一定是“自身”
                {
                    liuqinNames[i] = "自身";
                }
                else
                {
                    var wuxingRelation = GetWuxingRelation(currentShiDizhi, dizhiOrder[i]);
                    liuqinNames[i] = wuxingRelation.ToString(WuxingRelationToStringConversions.Liuqin);
                }
            }

            // 修正六亲顺序
            FixLiuqinOrder(liuqinNames, shiGongIndex);

            return liuqinNames;
        }

        private static void FixLiuqinOrder(string[] liuqinNames, int shiGongIndex)
        {
            // 从时宫开始顺时针寻找第一个与其他宫位相同六亲的宫位，将其六亲转变为兄弟
            for (int i = 1; i < 3; i++) // 实际排盘中，兄弟一般都在自身所落的下一宫或下两宫，所以只检查这两个位置
            {
                int currentIndex = (shiGongIndex + i) % liuqinNames.Length;
                for (int j = 0; j < liuqinNames.Length; j++)
                {
                    if (j != currentIndex && liuqinNames[currentIndex] == liuqinNames[j])
                    {
                        liuqinNames[currentIndex] = "兄弟";
                        return;
                    }
                }
            }
        }


        private static WuxingRelation GetWuxingRelation(Dizhi dizhi, Dizhi currentShiDizhi)
        {
            var relation = dizhi.Wuxing().GetRelation(currentShiDizhi.Wuxing());
            return relation;
        }
    }
}
