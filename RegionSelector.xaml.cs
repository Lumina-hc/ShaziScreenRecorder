using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScreenRecorder.Core;
using ScreenRecorderLib;

namespace ScreenRecorder;

public partial class RegionSelector : Window
{
    private readonly MonitorInfo _monitor;
    private Point _start;
    private bool _dragging;
    private Rect _rect = Rect.Empty;

    public ScreenRect Result { get; private set; } = ScreenRect.Empty;

    public RegionSelector(MonitorInfo monitor)
    {
        InitializeComponent();
        _monitor = monitor;
        Left = monitor.Left / monitor.ScaleX;
        Top = monitor.Top / monitor.ScaleY;
        Width = monitor.Width / monitor.ScaleX;
        Height = monitor.Height / monitor.ScaleY;
        Loaded += (_, _) => { UpdateMask(null); Activate(); Focus(); };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
        else if (e.Key == Key.Enter && _rect.Width > 2 && _rect.Height > 2)
        {
            Confirm();
        }
        base.OnKeyDown(e);
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(RootCanvas);
        _dragging = true;
        _rect = new Rect(_start, new Size(0, 0));
        UpdateMask(_rect);
    }

    private void Root_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;
        var p = e.GetPosition(RootCanvas);
        _rect = new Rect(_start, p);
        UpdateMask(_rect);
    }

    private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;
        _dragging = false;
        if (_rect.Width > 4 && _rect.Height > 4)
            Confirm();
    }

    private void Confirm()
    {
        var x = _monitor.Left + _rect.X * _monitor.ScaleX;
        var y = _monitor.Top + _rect.Y * _monitor.ScaleY;
        var w = _rect.Width * _monitor.ScaleX;
        var h = _rect.Height * _monitor.ScaleY;
        Result = new ScreenRect(Math.Round(x), Math.Round(y), Math.Round(w), Math.Round(h));
        DialogResult = true;
        Close();
    }

    private void UpdateMask(Rect? rect)
    {
        var full = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
        if (rect is null || rect.Value.Width < 1 || rect.Value.Height < 1)
        {
            Mask.Data = full;
            Sel.Visibility = Visibility.Collapsed;
            InfoBox.Visibility = Visibility.Collapsed;
            return;
        }

        var r = rect.Value;
        var sel = new RectangleGeometry(r);
        var clip = new CombinedGeometry(GeometryCombineMode.Exclude, full, sel);
        Mask.Data = clip;

        Sel.Visibility = Visibility.Visible;
        Canvas.SetLeft(Sel, r.X);
        Canvas.SetTop(Sel, r.Y);
        Sel.Width = r.Width;
        Sel.Height = r.Height;

        InfoBox.Visibility = Visibility.Visible;
        InfoText.Text = $"{(int)(r.Width * _monitor.ScaleX)} × {(int)(r.Height * _monitor.ScaleY)}";
        var infoTop = r.Y - 32;
        if (infoTop < 4)
            infoTop = r.Y + 8;
        Canvas.SetLeft(InfoBox, r.X);
        Canvas.SetTop(InfoBox, infoTop);
    }
}
