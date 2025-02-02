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
        private Brush? _originalBorderBrush; // ����ԭʼ�ı߿���ɫ
        private DispatcherTimer? _timer; // ���ڶ�ʱ����ʱ��ļ�ʱ��
        private DateTime _customDateTime; // �Զ���ʱ��
        private bool _useCustomTime; // �Ƿ�ʹ���Զ���ʱ��
        private bool _isCalculated; // �Ƿ��Ѿ������
        private Dizhi _lastShiDizhi; // ��һ�μ����ʱ����֧
        private Dizhi[] _dizhiOrder = Array.Empty<Dizhi>(); // ��֧˳������
        private LunarDateTime? _lunar; // ũ��ʱ��
        private TextBox[] _textBoxes; // ���ڴ洢�·ݡ����ں�Сʱ���ı���
        private string? _originalText; // ���ڱ������뷨���ǰ���ı�
        private bool _isComposing; // ����Ƿ������뷨���״̬
        private bool _isEmpty; // ��������Ƿ�Ϊ��
        private bool _isZero; // ��������Ƿ�Ϊ��

        private const string LiuqinSelf = "����"; // �����еġ�����
        private const string LiuqinSibling = "�ֵ�"; // �����еġ��ֵܡ�
        private static readonly string[] Positions = { "��", "����", "��ϲ", "���", "С��", "����" }; // �����е�����λ��

        public SmallSixRen()
        {
            InitializeComponent();
            InitializeStyles();
            // ��ʼ�� TextBox ���飬�����·ݡ����ں�Сʱ���ı���
            _textBoxes = new[] { MonthTextBox, DayTextBox, HourTextBox };
            RegisterEventHandlers();
        }

        // ��ʼ����ʽ������ԭʼ�ı߿���ɫ
        private void InitializeStyles()
        {
            _originalBorderBrush = MonthTextBox.BorderBrush;
        }

        // ע���¼�������
        private void RegisterEventHandlers()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ҳ�����ʱ���¼�����
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeDateTimeDisplay();
            TimeModeToggleSwitch.Toggled += OnTimeModeToggled;
            PalaceModeToggleSwitch.Toggled += OnPalaceModeToggled;

            // ʹ��ѭ��Ϊÿ�� TextBox ע���¼�
            foreach (var textBox in _textBoxes)
            {
                textBox.TextChanged += OnTextBoxTextChanged;
                textBox.GotFocus += OnTextBoxGotFocus;
                textBox.BeforeTextChanging += OnTextBoxBeforeTextChanging;
                textBox.TextCompositionStarted += OnTextBoxTextCompositionStarted;
                textBox.TextCompositionEnded += OnTextBoxTextCompositionEnded;
            }
        }

        // ҳ��ж��ʱ���¼�����
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopTimer();
            TimeModeToggleSwitch.Toggled -= OnTimeModeToggled;
            PalaceModeToggleSwitch.Toggled -= OnPalaceModeToggled;

            // ʹ��ѭ��Ϊÿ�� TextBox ȡ���¼�
            foreach (var textBox in _textBoxes)
            {
                textBox.TextChanged -= OnTextBoxTextChanged;
                textBox.GotFocus -= OnTextBoxGotFocus;
                textBox.BeforeTextChanging -= OnTextBoxBeforeTextChanging;
                textBox.TextCompositionStarted -= OnTextBoxTextCompositionStarted;
                textBox.TextCompositionEnded -= OnTextBoxTextCompositionEnded;
            }
        }

        // ��ʼ������ʱ����ʾ
        private void InitializeDateTimeDisplay()
        {
            UpdateDateTimeDisplay();
            StartTimer();
        }

        // ������ʱ��
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

        // ֹͣ��ʱ��
        private void StopTimer()
        {
            if (_timer == null) return;

            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        // ��ʱ������ʱ���¼�����
        private void OnTimerTick(object? sender, object e)
        {
            if (!_useCustomTime)
            {
                DispatcherQueue.TryEnqueue(UpdateDateTimeDisplay);
            }
        }

        // ʱ��ģʽ�л�ʱ���¼�����
        private void OnTimeModeToggled(object sender, RoutedEventArgs e)
        {
            _useCustomTime = TimeModeToggleSwitch.IsOn;
            if (_useCustomTime) StopTimer();
            else StartTimer();
        }

        // ��λģʽ�л�ʱ���¼�����
        private void OnPalaceModeToggled(object sender, RoutedEventArgs e)
        {
            MonthTextBox.IsEnabled = PalaceModeToggleSwitch.IsOn;
            if (!MonthTextBox.IsEnabled) MonthTextBox.Text = string.Empty;
        }

        // TextBox �ı��仯ʱ���¼�����
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            // �� TextBox ���ݷ����仯ʱ������ _isCalculated Ϊ false
            _isCalculated = false;
        }

        // TextBox ��ý���ʱ���¼�����
        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (_isEmpty || _isZero) RestoreAllTextBoxStylesAndHideError();
        }

        // ���뷨��Ͽ�ʼʱ���¼�����
        private void OnTextBoxTextCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
        {
            _isComposing = true;
            _originalText = sender.Text; // ��¼��Ͽ�ʼǰ���ı�
        }

        // ���뷨��Ͻ���ʱ���¼�����
        private void OnTextBoxTextCompositionEnded(TextBox sender, TextCompositionEndedEventArgs args)
        {
            _isComposing = false;
            // �������������ַ�
            if (sender.Text != _originalText)
            {
                // ʹ��������ʽ�Ƴ����з������ַ����������������֣�
                string newInput = sender.Text.Replace(_originalText!, "");
                string filteredInput = Regex.Replace(newInput, @"[^\d]", "");
                sender.Text = _originalText + filteredInput;
            }
        }

        // TextBox �ı��仯ǰ���¼�����
        private void OnTextBoxBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // ����ɾ�����������뷨����е���ʱ�ַ�
            if (_isComposing || string.IsNullOrEmpty(args.NewText))
            {
                return;
            }
            // ����ͨ�����������ʱ�ַ�
            args.Cancel = !Regex.IsMatch(args.NewText, @"^[\d\p{P}\p{S}]*$");
        }

        // �ָ����� TextBox ����ʽ�����ش�����Ϣ
        private void RestoreAllTextBoxStylesAndHideError()
        {
            MonthTextBox.BorderBrush = _originalBorderBrush;
            DayTextBox.BorderBrush = _originalBorderBrush;
            HourTextBox.BorderBrush = _originalBorderBrush;
            ErrorMessageTextBlock.Text = string.Empty;
            _isEmpty = false;
            _isZero = false;
        }

        // ��ʾ����ʱ��ѡ����
        private async void ShowDateTimePicker()
        {
            var dateTime = DateTime.Now;
            var datePicker = new DatePicker { Date = new DateTimeOffset(dateTime.Date) };
            var timePicker = new TimePicker { Time = dateTime.TimeOfDay };

            var dialog = new ContentDialog
            {
                Title = "ѡ���Զ���ʱ��",
                XamlRoot = XamlRoot,
                Content = CreatePickerPanel(datePicker, timePicker),
                PrimaryButtonText = "ȷ��",
                CloseButtonText = "ȡ��",
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

        // �������ں�ʱ��ѡ���������
        private static StackPanel CreatePickerPanel(DatePicker datePicker, TimePicker timePicker)
        {
            return new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "ѡ�����ڣ�", Margin = new Thickness(0, 0, 0, 5) },
                    datePicker,
                    new TextBlock { Text = "ѡ��ʱ�䣺", Margin = new Thickness(0, 10, 0, 5) },
                    timePicker
                }
            };
        }

        // ��������ʱ����ʾ
        private void UpdateDateTimeDisplay()
        {
            var currentTime = GetCurrentDateTime();
            _lunar = LunarDateTime.FromGregorian(currentTime);

            var displayText = new StringBuilder()
                .AppendFormat("����: {0:yyyy-MM-dd HH:mm:ss}\n", currentTime)
                .AppendFormat("ũ��: {0:C}��{1}{2}��{3}��\n",
                    _lunar.Nian,
                    _lunar.IsRunyue ? "��" : "ƽ",
                    _lunar.Yue,
                    _lunar.Ri)
                .AppendFormat("ʱ��: {0:C}", _lunar.Shi)
                .ToString();

            CurrentDateTimeButton.Content = displayText;
        }

        // ��ȡ��ǰʱ��
        private DateTime GetCurrentDateTime() => _useCustomTime ? _customDateTime : DateTime.Now;

        // �������ɽ��
        private void Calculate()
        {
            if (_lunar is null) return; // ��ӿ�ֵ���
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

        // ��֤�����Ƿ���Ч
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

        // ��֤��������������
        private (bool _isEmpty, bool _isZero) ValidateInput(TextBox box, bool isRequired = true)
        {
            _isEmpty = isRequired && string.IsNullOrWhiteSpace(box.Text);
            _isZero = !_isEmpty && box.Text.All(c => c == '0');

            box.BorderBrush = _isEmpty || _isZero
                ? new SolidColorBrush(Microsoft.UI.Colors.Red)
                : _originalBorderBrush;

            return (_isEmpty, _isZero);
        }

        // ������֤����
        private bool HandleValidationErrors(bool hasEmpty, bool hasZero)
        {
            if (hasEmpty)
            {
                ShowError("����дȱʧ��");
                return false;
            }

            if (hasZero)
            {
                ShowError("����д��0��");
                return false;
            }

            ClearErrors();
            return true;
        }

        // ��ʾ������Ϣ
        private void ShowError(string message) => ErrorMessageTextBlock.Text = message;
        // ���������Ϣ
        private void ClearErrors() => ErrorMessageTextBlock.Text = string.Empty;

        // ��ȡ���㲽��
        private int[] GetCalculationSteps()
        {
            return new[]
            {
                PalaceModeToggleSwitch.IsOn ? ParseInput(MonthTextBox) : 0,
                ParseInput(DayTextBox),
                ParseInput(HourTextBox)
            };
        }

        // ����������е�����Ϊ����
        private static int ParseInput(TextBox box) =>
            int.TryParse(box.Text, out var value) ? value : 0;

        // �������ɵ�λ�ý��
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

        // ��ȡ����ǰ׺
        private static string GetStepPrefix(int stepIndex) => stepIndex switch
        {
            0 => "��",
            1 => "��",
            2 => "ʱ",
            _ => string.Empty
        };

        // ��������λ�õ���ʾ
        private void UpdateMarksDisplay(Dictionary<string, StringBuilder> positionsResult)
        {
            LiuLianMarks.Text = positionsResult["����"].ToString();
            SuXiMarks.Text = positionsResult["��ϲ"].ToString();
            ChiKouMarks.Text = positionsResult["���"].ToString();
            DaAnMarks.Text = positionsResult["��"].ToString();
            KongWangMarks.Text = positionsResult["����"].ToString();
            XiaoJiMarks.Text = positionsResult["С��"].ToString();
        }

        // ���µ�֧����ʾ
        private void UpdateDizhiDisplay(Dizhi currentShi, int shiGongIndex)
        {
            DaAnDizhi.Text = _dizhiOrder[0].ToString("C");
            LiuLianDizhi.Text = _dizhiOrder[1].ToString("C");
            SuXiDizhi.Text = _dizhiOrder[2].ToString("C");
            ChiKouDizhi.Text = _dizhiOrder[3].ToString("C");
            XiaoJiDizhi.Text = _dizhiOrder[4].ToString("C");
            KongWangDizhi.Text = _dizhiOrder[5].ToString("C");
        }

        // ���ɵ�֧˳��
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

        // �������׵���ʾ
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

        // ��������˳��
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

        // ��ȡ���й�ϵ
        private static WuxingRelation GetWuxingRelation(Dizhi current, Dizhi target)
        {
            return current.Wuxing().GetRelation(target.Wuxing());
        }

        // ��������λ��
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

        // ����ʱ�䰴ť����¼�����
        private void OnDateTimeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_useCustomTime) ShowDateTimePicker();
        }

        // ���㰴ť����¼�����
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