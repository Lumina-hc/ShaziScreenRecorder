using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ScreenRecorder.Core;
using ScreenRecorderLib;

namespace ScreenRecorder;

public partial class MainWindow : Window
{
    private readonly RecorderService _service = new();
    private readonly ObservableCollection<RecordingItem> _items = new();
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _watch = new();
    private FloatingWindow? _floating;
    private CaptureKind _kind = CaptureKind.FullScreen;
    private ScreenRect _region = ScreenRect.Empty;
    private string _outputDir;
    private bool _recording;
    private bool _paused;

    public MainWindow()
    {
        InitializeComponent();
        _outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "傻子录屏");
        Directory.CreateDirectory(_outputDir);
        PathBox.Text = _outputDir;
        ReloadItems();
        FileList.ItemsSource = _items;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => UpdateTimer();

        _service.StatusChanged += OnStatusChanged;
        _service.Completed += OnCompleted;
        _service.Failed += OnFailed;

        Loaded += (_, _) =>
        {
            LoadDisplays();
            LoadMicDevices();
            LoadWindows();
        };
    }

    private void LoadDisplays()
    {
        try
        {
            var displays = Recorder.GetDisplays();
            DisplayCombo.ItemsSource = displays;
            if (displays.Count > 0)
                DisplayCombo.SelectedIndex = 0;
        }
        catch { }
    }

    private void LoadMicDevices()
    {
        try
        {
            MicCombo.ItemsSource = Recorder.GetSystemAudioCaptureDevices();
            if (MicCombo.Items.Count > 0)
                MicCombo.SelectedIndex = 0;
        }
        catch { }
    }

    private void LoadWindows()
    {
        try
        {
            var list = Recorder.GetWindows()
                .Where(w => w.IsValidWindow() && !w.IsMinmimized())
                .ToList();
            WindowCombo.ItemsSource = list;
            if (list.Count > 0)
                WindowCombo.SelectedIndex = 0;
        }
        catch { }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }
        try { DragMove(); } catch { }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_recording)
        {
            _recording = false;
            _service.Completed -= OnCompleted;
            _service.Failed -= OnFailed;
            _service.StatusChanged -= OnStatusChanged;
            _service.Stop();
            var sw = Stopwatch.StartNew();
            while (_service.Status != RecorderStatus.Idle && sw.ElapsedMilliseconds < 15000)
                System.Threading.Thread.Sleep(50);
        }
        _service.Dispose();
        _floating?.Close();
        base.OnClosing(e);
    }

    private void Seg_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton tb || _recording)
            return;
        var tag = tb.Tag as string;
        _kind = tag switch
        {
            "Window" => CaptureKind.Window,
            "Region" => CaptureKind.Region,
            _ => CaptureKind.FullScreen
        };
        SegFull.IsChecked = _kind == CaptureKind.FullScreen;
        SegWindow.IsChecked = _kind == CaptureKind.Window;
        SegRegion.IsChecked = _kind == CaptureKind.Region;

        DisplayCombo.Visibility = _kind == CaptureKind.Region || _kind == CaptureKind.FullScreen ? Visibility.Visible : Visibility.Collapsed;
        WindowPanel.Visibility = _kind == CaptureKind.Window ? Visibility.Visible : Visibility.Collapsed;
        RegionPanel.Visibility = _kind == CaptureKind.Region ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Display_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void RefreshWindows_Click(object sender, RoutedEventArgs e) => LoadWindows();

    private void Region_Click(object sender, RoutedEventArgs e)
    {
        var display = DisplayCombo.SelectedItem as RecordableDisplay;
        var monitor = MonitorHelper.FindByDevice(display?.DeviceName ?? "");
        if (monitor == null)
            return;

        Hide();
        var selector = new RegionSelector(monitor);
        var ok = selector.ShowDialog();
        Show();
        Activate();

        if (ok == true && selector.Result.Width > 2 && selector.Result.Height > 2)
        {
            _region = selector.Result;
            RegionText.Foreground = (Brush)FindResource("Text");
            RegionText.Text = $"{(int)_region.Width} × {(int)_region.Height}  @  ({(int)_region.Left}, {(int)_region.Top})";
        }
    }

    private void MicToggle_Changed(object sender, RoutedEventArgs e)
    {
        MicCombo.IsEnabled = MicToggle.IsChecked == true;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "选择保存目录" };
        try { dlg.InitialDirectory = _outputDir; } catch { }
        if (dlg.ShowDialog() == true)
        {
            _outputDir = dlg.FolderName;
            PathBox.Text = _outputDir;
            ReloadItems();
        }
    }

    private void RecordBtn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_recording)
            StopRecording();
        else
            StartRecording();
    }

    private void StartRecording()
    {
        if (_kind == CaptureKind.Window && WindowCombo.SelectedItem is not RecordableWindow win)
        {
            StatusText.Text = "请选择一个窗口";
            return;
        }
        if (_kind == CaptureKind.Region && _region.Equals(ScreenRect.Empty))
        {
            StatusText.Text = "请先框选录制区域";
            return;
        }

        Directory.CreateDirectory(_outputDir);
        var path = Path.Combine(_outputDir, $"录屏_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

        var cfg = new RecordingConfig
        {
            Kind = _kind,
            SystemAudio = SysToggle.IsChecked == true,
            Microphone = MicToggle.IsChecked == true,
            MicrophoneDevice = (MicCombo.SelectedItem as RecordableAudioCaptureDevice)?.DeviceName ?? "",
            Framerate = FpsCombo.SelectedIndex == 1 ? 60 : 30,
            BitrateKbps = FpsCombo.SelectedIndex == 1 ? 12000 : 8000
        };

        if (_kind == CaptureKind.Window)
        {
            cfg.WindowHandle = (WindowCombo.SelectedItem as RecordableWindow)!.Handle;
        }
        else if (_kind == CaptureKind.Region)
        {
            var display = DisplayCombo.SelectedItem as RecordableDisplay;
            var monitor = MonitorHelper.FindByDevice(display?.DeviceName ?? "");
            cfg.DisplayDeviceName = monitor?.DeviceName ?? display?.DeviceName ?? "";
            cfg.Region = _region;
        }
        else
        {
            cfg.DisplayDeviceName = (DisplayCombo.SelectedItem as RecordableDisplay)?.DeviceName ?? "";
        }

        try
        {
            _service.Start(cfg, path);
        }
        catch (Exception ex)
        {
            StatusText.Text = "启动失败：" + ex.Message;
            return;
        }

        _recording = true;
        _paused = false;
        _watch.Restart();
        _timer.Start();
        SetUiRecording(true);
        StatusText.Text = "正在录制…";
        _floating?.SetRecording(true);
    }

    private void StopRecording()
    {
        if (!_recording)
            return;
        _recording = false;
        _paused = false;
        _timer.Stop();
        _watch.Stop();
        _service.Stop();
        SetUiRecording(false);
        StatusText.Text = "正在保存…";
        _floating?.SetRecording(false);
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (!_recording)
            return;
        if (_paused)
        {
            _service.Resume();
            _watch.Start();
            _paused = false;
            PauseButton.Content = "暂停";
            StatusText.Text = "正在录制…";
        }
        else
        {
            _service.Pause();
            _watch.Stop();
            _paused = true;
            PauseButton.Content = "继续";
            StatusText.Text = "已暂停";
        }
        _floating?.SetPaused(_paused);
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => StopRecording();

    private void SetUiRecording(bool recording)
    {
        RecStop.Visibility = recording ? Visibility.Visible : Visibility.Collapsed;
        RecCore.Visibility = recording ? Visibility.Collapsed : Visibility.Visible;
        RecRing.Stroke = recording ? new SolidColorBrush(Color.FromRgb(0xF0, 0x47, 0x47)) : new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x2E));
        PauseButton.IsEnabled = recording;
        PauseButton.Content = "暂停";
        StopButton.IsEnabled = recording;
        RecordBtnGrid.Cursor = Cursors.Hand;
        SegFull.IsEnabled = SegWindow.IsEnabled = SegRegion.IsEnabled = !recording;
        DisplayCombo.IsEnabled = WindowCombo.IsEnabled = !recording;
    }

    private void UpdateTimer()
    {
        TimerText.Text = _watch.Elapsed.ToString(@"hh\:mm\:ss");
        _floating?.SetTime(_watch.Elapsed);
    }

    private void OnStatusChanged(RecorderStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusDot.Fill = status switch
            {
                RecorderStatus.Recording => (Brush)FindResource("Rec"),
                RecorderStatus.Paused => (Brush)FindResource("Accent"),
                _ => (Brush)FindResource("Muted")
            };
        });
    }

    private void OnCompleted(string path)
    {
        Dispatcher.Invoke(() =>
        {
            TimerText.Text = "00:00:00";
            StatusText.Text = "已保存";
            _floating?.SetTime(TimeSpan.Zero);
            AddItem(path);
        });
    }

    private void OnFailed(string error)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = "录制失败：" + error;
            _recording = false;
            _timer.Stop();
            _watch.Reset();
            SetUiRecording(false);
            _floating?.SetRecording(false);
        });
    }

    private void AddItem(string path) => ReloadItems();

    private void ReloadItems()
    {
        _items.Clear();
        try
        {
            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);
            foreach (var fi in new DirectoryInfo(_outputDir)
                         .GetFiles("*.mp4", SearchOption.TopDirectoryOnly)
                         .OrderByDescending(f => f.LastWriteTime))
            {
                _items.Add(new RecordingItem
                {
                    FilePath = fi.FullName,
                    Name = fi.Name,
                    Meta = $"{FormatSize(fi.Length)}  ·  {fi.LastWriteTime:MM-dd HH:mm}"
                });
            }
        }
        catch { }
        EmptyHint.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int i = 0;
        while (size >= 1024 && i < units.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.#} {units[i]}";
    }

    private void PlayItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is RecordingItem item && File.Exists(item.FilePath))
            Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not RecordingItem item)
            return;
        if (MessageBox.Show(this, $"删除文件？\n{item.Name}", "傻子录屏", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
            return;
        try { File.Delete(item.FilePath); } catch { }
        _items.Remove(item);
        EmptyHint.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_outputDir);
            Process.Start(new ProcessStartInfo(_outputDir) { UseShellExecute = true });
        }
        catch { }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "傻子录屏\n基于 Windows 图形采集 (WGC) + Media Foundation\n\n支持整屏 / 窗口 / 区域录制，系统声音与麦克风可独立开关。", "关于", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void FloatButton_Click(object sender, RoutedEventArgs e)
    {
        if (_floating == null)
        {
            _floating = new FloatingWindow();
            _floating.RecordToggleRequested += () => Dispatcher.Invoke(() => { if (_recording) StopRecording(); else StartRecording(); });
            _floating.ExpandRequested += () => Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
                _floating?.Hide();
            });
            _floating.SetRecording(_recording);
            _floating.SetTime(_watch.Elapsed);
            _floating.Closed += (_, _) => _floating = null;
            _floating.Show();
            var handle = new WindowInteropHelper(_floating).Handle;
            try { Recorder.SetExcludeFromCapture(handle, true); } catch { }
        }
        else
        {
            _floating.Show();
            _floating.Activate();
        }
        Hide();
    }
}
