using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YiJingFramework.Nongli.Lunar;
using YiJingFramework.PrimitiveTypes;
using YiJingFramework.EntityRelations.WuxingRelations.Extensions;
using YiJingFramework.EntityRelations.EntityCharacteristics.Extensions;
using System.Text.RegularExpressions;
using YiJingFramework.EntityRelations.EntityStrings.Conversions;
using YiJingFramework.EntityRelations.WuxingRelations;
using YiJingFramework.EntityRelations.EntityStrings.Extensions;

namespace HexagramPlacementWin.Pages
{
    public sealed partial class SmallSixRen : Page
    {
        private Brush? _originalBorderBrush; // 保存原始的边框颜色
        private DispatcherTimer? _timer; // 用于定时更新时间的计时器
        private DateTime _customDateTime; // 自定义时间
        private bool _useCustomTime; // 是否使用自定义时间
        private bool _isCalculated; // 是否已经计算过
        private Dizhi _lastShiDizhi; // 上一次计算的时辰地支
        private Dizhi[] _dizhiOrder = Array.Empty<Dizhi>(); // 地支顺序数组
        private LunarDateTime? _lunar; // 农历时间
        private TextBox[] _textBoxes; // 用于存储月份、日期和小时的文本框
        private string? _originalText; // 用于保存输入法组合前的文本
        private bool _isComposing; // 标记是否处于输入法组合状态
        private bool _isEmpty; // 标记输入是否为空
        private bool _isZero; // 标记输入是否为零

        private const string LiuqinSelf = "自身"; // 六亲中的“自身”
        private const string LiuqinSibling = "兄弟"; // 六亲中的“兄弟”
        private static readonly string[] Positions = { "大安", "留连", "速喜", "赤口", "小吉", "空亡" }; // 六壬中的六个位置

        public SmallSixRen()
        {
            InitializeComponent();
            InitializeStyles();
            // 初始化 TextBox 数组，包含月份、日期和小时的文本框
            _textBoxes = new[] { MonthTextBox, DayTextBox, HourTextBox };
            RegisterEventHandlers();
        }

        // 初始化样式，保存原始的边框颜色
        private void InitializeStyles()
        {
            _originalBorderBrush = MonthTextBox.BorderBrush;
        }

        // 注册事件处理器
        private void RegisterEventHandlers()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // 页面加载时的事件处理
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeDateTimeDisplay();
            TimeModeToggleSwitch.Toggled += OnTimeModeToggled;
            PalaceModeToggleSwitch.Toggled += OnPalaceModeToggled;

            // 使用循环为每个 TextBox 注册事件
            foreach (var textBox in _textBoxes)
            {
                textBox.TextChanged += OnTextBoxTextChanged;
                textBox.GotFocus += OnTextBoxGotFocus;
                textBox.BeforeTextChanging += OnTextBoxBeforeTextChanging;
                textBox.TextCompositionStarted += OnTextBoxTextCompositionStarted;
                textBox.TextCompositionEnded += OnTextBoxTextCompositionEnded;
            }
        }

        // 页面卸载时的事件处理
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopTimer();
            TimeModeToggleSwitch.Toggled -= OnTimeModeToggled;
            PalaceModeToggleSwitch.Toggled -= OnPalaceModeToggled;

            // 使用循环为每个 TextBox 取消事件
            foreach (var textBox in _textBoxes)
            {
                textBox.TextChanged -= OnTextBoxTextChanged;
                textBox.GotFocus -= OnTextBoxGotFocus;
                textBox.BeforeTextChanging -= OnTextBoxBeforeTextChanging;
                textBox.TextCompositionStarted -= OnTextBoxTextCompositionStarted;
                textBox.TextCompositionEnded -= OnTextBoxTextCompositionEnded;
            }
        }

        // 初始化日期时间显示
        private void InitializeDateTimeDisplay()
        {
            UpdateDateTimeDisplay();
            StartTimer();
        }

        // 启动计时器
        private void StartTimer()
        {
            StopTimer();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        // 停止计时器
        private void StopTimer()
        {
            if (_timer == null) return;

            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        // 计时器触发时的事件处理
        private void OnTimerTick(object? sender, object e)
        {
            if (!_useCustomTime)
            {
                DispatcherQueue.TryEnqueue(UpdateDateTimeDisplay);
            }
        }

        // 时间模式切换时的事件处理
        private void OnTimeModeToggled(object sender, RoutedEventArgs e)
        {
            _useCustomTime = TimeModeToggleSwitch.IsOn;
            if (_useCustomTime) StopTimer();
            else StartTimer();
        }

        // 宫位模式切换时的事件处理
        private void OnPalaceModeToggled(object sender, RoutedEventArgs e)
        {
            MonthTextBox.IsEnabled = PalaceModeToggleSwitch.IsOn;
            if (!MonthTextBox.IsEnabled) MonthTextBox.Text = string.Empty;
        }

        // TextBox 文本变化时的事件处理
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // 当 TextBox 内容发生变化时，重置 _isCalculated 为 false
            _isCalculated = false;
        }

        // TextBox 获得焦点时的事件处理
        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (_isEmpty || _isZero) RestoreAllTextBoxStylesAndHideError();
        }

        // 输入法组合开始时的事件处理
        private void OnTextBoxTextCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
        {
            _isComposing = true;
            _originalText = sender.Text; // 记录组合开始前的文本
        }

        // 输入法组合结束时的事件处理
        private void OnTextBoxTextCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
        {
            _isComposing = false;
            // 仅处理新增的字符
            if (sender.Text != _originalText)
            {
                // 使用正则表达式移除所有非数字字符（仅处理新增部分）
                string newInput = sender.Text.Replace(_originalText!, "");
                string filteredInput = Regex.Replace(newInput, @"[^\d]", "");
                sender.Text = _originalText + filteredInput;
            }
        }

        // TextBox 文本变化前的事件处理
        private void OnTextBoxBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // 允许删除操作和输入法组合中的临时字符
            if (_isComposing || string.IsNullOrEmpty(args.NewText))
            {
                return;
            }
            // 允许通过组合输入临时字符
            args.Cancel = !Regex.IsMatch(args.NewText, @"^[\d\p{P}\p{S}]*$");
        }

        // 恢复所有 TextBox 的样式并隐藏错误信息
        private void RestoreAllTextBoxStylesAndHideError()
        {
            MonthTextBox.BorderBrush = _originalBorderBrush;
            DayTextBox.BorderBrush = _originalBorderBrush;
            HourTextBox.BorderBrush = _originalBorderBrush;
            ErrorMessageTextBlock.Text = string.Empty;
            _isEmpty = false;
            _isZero = false;
        }

        // 显示日期时间选择器
        private async void ShowDateTimePicker()
        {
            var dateTime = DateTime.Now;
            var datePicker = new DatePicker { Date = new DateTimeOffset(dateTime.Date) };
            var timePicker = new TimePicker { Time = dateTime.TimeOfDay };

            var dialog = new ContentDialog
            {
                Title = "选择自定义时间",
                XamlRoot = XamlRoot,
                Content = CreatePickerPanel(datePicker, timePicker),
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                RequestedTheme = ((FrameworkElement)Content).ActualTheme
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                _customDateTime = new DateTime(datePicker.Date.Year, datePicker.Date.Month,
                    datePicker.Date.Day, timePicker.Time.Hours, timePicker.Time.Minutes, 0);
                UpdateDateTimeDisplay();
            };

            await dialog.ShowAsync();
        }

        // 创建日期和时间选择器的面板
        private static StackPanel CreatePickerPanel(DatePicker datePicker, TimePicker timePicker)
        {
            return new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "选择日期：", Margin = new Thickness(0, 0, 0, 5) },
                    datePicker,
                    new TextBlock { Text = "选择时间：", Margin = new Thickness(0, 10, 0, 5) },
                    timePicker
                }
            };
        }

        // 更新日期时间显示
        private void UpdateDateTimeDisplay()
        {
            var currentTime = GetCurrentDateTime();
            _lunar = LunarDateTime.FromGregorian(currentTime);

            var displayText = new StringBuilder()
                .AppendFormat("新历: {0:yyyy-MM-dd HH:mm:ss}\n", currentTime)
                .AppendFormat("农历: {0:C}年{1}{2}月{3}日\n",
                    _lunar.Nian,
                    _lunar.IsRunyue ? "闰" : "平",
                    _lunar.Yue,
                    _lunar.Ri)
                .AppendFormat("时辰: {0:C}", _lunar.Shi)
                .ToString();

            CurrentDateTimeButton.Content = displayText;
        }

        // 获取当前时间
        private DateTime GetCurrentDateTime() => _useCustomTime ? _customDateTime : DateTime.Now;

        // 计算六壬结果
        private void Calculate()
        {
            if (_lunar is null) return; // 添加空值检查
            if (!ValidateInputs()) return;

            var steps = GetCalculationSteps();
            var (positionsResult, shiGongIndex) = CalculatePositions(steps);

            UpdateMarksDisplay(positionsResult);
            _dizhiOrder = GenerateDizhiOrder(_lunar.Shi, shiGongIndex);
            UpdateDizhiDisplay(_lunar.Shi, shiGongIndex);
            UpdateLiuqinDisplay(shiGongIndex);

            _isCalculated = true;
            _lastShiDizhi = _lunar.Shi;
        }

        // 验证输入是否有效
        private bool ValidateInputs()
        {
            var validationResults = new[]
            {
                ValidateInput(MonthTextBox, PalaceModeToggleSwitch.IsOn),
                ValidateInput(DayTextBox),
                ValidateInput(HourTextBox)
            };

            var hasEmpty = validationResults.Any(r => r._isEmpty);
            var hasZero = validationResults.Any(r => r._isZero);

            return HandleValidationErrors(hasEmpty, hasZero);
        }

        // 验证单个输入框的内容
        private (bool _isEmpty, bool _isZero) ValidateInput(TextBox box, bool isRequired = true)
        {
            _isEmpty = isRequired && string.IsNullOrWhiteSpace(box.Text);
            _isZero = !_isEmpty && box.Text.All(c => c == '0');

            box.BorderBrush = _isEmpty || _isZero
                ? new SolidColorBrush(Microsoft.UI.Colors.Red)
                : _originalBorderBrush;

            return (_isEmpty, _isZero);
        }

        // 处理验证错误
        private bool HandleValidationErrors(bool hasEmpty, bool hasZero)
        {
            if (hasEmpty)
            {
                ShowError("请填写缺失项");
                return false;
            }

            if (hasZero)
            {
                ShowError("请填写非0数");
                return false;
            }

            ClearErrors();
            return true;
        }

        // 显示错误信息
        private void ShowError(string message) => ErrorMessageTextBlock.Text = message;
        // 清除错误信息
        private void ClearErrors() => ErrorMessageTextBlock.Text = string.Empty;

        // 获取计算步骤
        private int[] GetCalculationSteps()
        {
            return new[]
            {
                PalaceModeToggleSwitch.IsOn ? ParseInput(MonthTextBox) : 0,
                ParseInput(DayTextBox),
                ParseInput(HourTextBox)
            };
        }

        // 解析输入框中的内容为整数
        private static int ParseInput(TextBox box) =>
            int.TryParse(box.Text, out var value) ? value : 0;

        // 计算六壬的位置结果
        private static (Dictionary<string, StringBuilder> PositionsResult, int ShiGongIndex)
            CalculatePositions(int[] steps)
        {
            var positionsResult = Positions.ToDictionary(
                p => p,
                _ => new StringBuilder());

            int currentIndex = 0;
            int shiGongIndex = -1;

            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] == 0) continue;

                int step = steps[i] % 6;
                step = (step == 0) ? 6 : step;
                currentIndex = (currentIndex + step - 1) % 6;

                if (i == 2) shiGongIndex = currentIndex;

                var position = Positions[currentIndex];
                positionsResult[position].Append(GetStepPrefix(i));
            }

            return (positionsResult, shiGongIndex);
        }

        // 获取步骤前缀
        private static string GetStepPrefix(int stepIndex) => stepIndex switch
        {
            0 => "月",
            1 => "日",
            2 => "时",
            _ => string.Empty
        };

        // 更新六壬位置的显示
        private void UpdateMarksDisplay(Dictionary<string, StringBuilder> positionsResult)
        {
            LiuLianMarks.Text = positionsResult["留连"].ToString();
            SuXiMarks.Text = positionsResult["速喜"].ToString();
            ChiKouMarks.Text = positionsResult["赤口"].ToString();
            DaAnMarks.Text = positionsResult["大安"].ToString();
            KongWangMarks.Text = positionsResult["空亡"].ToString();
            XiaoJiMarks.Text = positionsResult["小吉"].ToString();
        }

        // 更新地支的显示
        private void UpdateDizhiDisplay(Dizhi currentShi, int shiGongIndex)
        {
            DaAnDizhi.Text = _dizhiOrder[0].ToString("C");
            LiuLianDizhi.Text = _dizhiOrder[1].ToString("C");
            SuXiDizhi.Text = _dizhiOrder[2].ToString("C");
            ChiKouDizhi.Text = _dizhiOrder[3].ToString("C");
            XiaoJiDizhi.Text = _dizhiOrder[4].ToString("C");
            KongWangDizhi.Text = _dizhiOrder[5].ToString("C");
        }

        // 生成地支顺序
        private static Dizhi[] GenerateDizhiOrder(Dizhi currentShi, int shiGongIndex)
        {
            var dizhis = new Dizhi[6];
            dizhis[shiGongIndex] = currentShi;

            for (int i = 1; i < 6; i++)
            {
                int index = (currentShi.Index + i * 2 - 1) % 12;
                dizhis[(shiGongIndex + i) % 6] = Dizhi.FromIndex((index + 1) % 12);
            }

            return dizhis;
        }

        // 更新六亲的显示
        private void UpdateLiuqinDisplay(int shiGongIndex)
        {
            var liuqinOrder = GenerateLiuqinOrder(_dizhiOrder, shiGongIndex);
            DaAnLiuqin.Text = liuqinOrder[0];
            LiuLianLiuqin.Text = liuqinOrder[1];
            SuXiLiuqin.Text = liuqinOrder[2];
            ChiKouLiuqin.Text = liuqinOrder[3];
            XiaoJiLiuqin.Text = liuqinOrder[4];
            KongWangLiuqin.Text = liuqinOrder[5];
        }

        // 生成六亲顺序
        private static string[] GenerateLiuqinOrder(Dizhi[] dizhiOrder, int shiGongIndex)
        {
            var currentShi = dizhiOrder[shiGongIndex];
            var liuqins = new string[6];

            for (int i = 0; i < 6; i++)
            {
                liuqins[i] = (i == shiGongIndex)
                    ? LiuqinSelf
                    : GetWuxingRelation(currentShi, dizhiOrder[i]).ToString(WuxingRelationToStringConversions.Liuqin);
            }

            AdjustLiuqinPosition(liuqins, shiGongIndex);
            return liuqins;
        }

        // 获取五行关系
        private static WuxingRelation GetWuxingRelation(Dizhi current, Dizhi target)
        {
            return current.Wuxing().GetRelation(target.Wuxing());
        }

        // 调整六亲位置
        private static void AdjustLiuqinPosition(IList<string> liuqins, int shiGongIndex)
        {
            for (int i = 1; i <= 2; i++)
            {
                int index = (shiGongIndex + i) % liuqins.Count;
                if (liuqins.Any(l => l == liuqins[index] && l != LiuqinSelf))
                {
                    liuqins[index] = LiuqinSibling;
                    return;
                }
            }
        }

        // 日期时间按钮点击事件处理
        private void OnDateTimeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_useCustomTime) ShowDateTimePicker();
        }

        // 计算按钮点击事件处理
        private void OnCalculateClick(object sender, RoutedEventArgs e)
        {
            _isCalculated = _lunar!.Shi.Equals(_lastShiDizhi) ? true : false;
            if (!_isCalculated)
            {
                StopTimer();
                Calculate();
            }
        }
    }
}