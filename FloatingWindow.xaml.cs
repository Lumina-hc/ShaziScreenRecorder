using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ScreenRecorder;

public partial class FloatingWindow : Window
{
    public event Action? RecordToggleRequested;
    public event Action? ExpandRequested;

    private bool _recording;
    private bool _paused;

    public FloatingWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 28;
            Top = wa.Top + 28;
        };
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src && IsInside(src, Btn))
            return;
        if (e.ClickCount == 2)
        {
            ExpandRequested?.Invoke();
            return;
        }
        try { DragMove(); } catch { }
    }

    private static bool IsInside(DependencyObject child, DependencyObject parent)
    {
        for (var cur = child; cur != null; cur = VisualTreeHelper.GetParent(cur) ?? LogicalTreeHelper.GetParent(cur))
            if (cur == parent)
                return true;
        return false;
    }

    private void Root_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ExpandRequested?.Invoke();
    }

    private void Btn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RecordToggleRequested?.Invoke();
    }

    private void Expand_Click(object sender, RoutedEventArgs e) => ExpandRequested?.Invoke();

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    public void SetRecording(bool recording)
    {
        _recording = recording;
        Stop.Visibility = recording ? Visibility.Visible : Visibility.Collapsed;
        Core.Visibility = recording ? Visibility.Collapsed : Visibility.Visible;
        Root.BorderBrush = recording ? new SolidColorBrush(Color.FromRgb(0xF0, 0x47, 0x47)) : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        Dot.Fill = recording ? new SolidColorBrush(Color.FromRgb(0xF0, 0x47, 0x47)) : new SolidColorBrush(Color.FromRgb(0x5F, 0x5F, 0x5F));
        StateText.Text = recording ? "正在录制" : "就绪";

        if (recording)
        {
            var blink = new DoubleAnimation(1, 0.35, TimeSpan.FromMilliseconds(650))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Dot.BeginAnimation(OpacityProperty, blink);
        }
        else
        {
            Dot.BeginAnimation(OpacityProperty, null);
            Dot.Opacity = 1;
        }
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        StateText.Text = paused ? "已暂停" : (_recording ? "正在录制" : "就绪");
        Dot.Fill = paused ? new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)) : (_recording ? new SolidColorBrush(Color.FromRgb(0xF0, 0x47, 0x47)) : new SolidColorBrush(Color.FromRgb(0x5F, 0x5F, 0x5F)));
    }

    public void SetTime(TimeSpan time)
    {
        TimeText.Text = time.ToString(@"hh\:mm\:ss");
    }
}
