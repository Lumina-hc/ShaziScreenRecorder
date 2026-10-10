using System.Runtime.InteropServices;

namespace ScreenRecorder.Core;

public sealed class MonitorInfo
{
    public string DeviceName = "";
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public uint DpiX = 96;
    public uint DpiY = 96;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public double ScaleX => DpiX / 96.0;
    public double ScaleY => DpiY / 96.0;
}

public static class MonitorHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint x, out uint y);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    public static List<MonitorInfo> GetMonitors()
    {
        var list = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data) =>
        {
            var info = new MONITORINFOEX();
            info.cbSize = Marshal.SizeOf<MONITORINFOEX>();
            if (GetMonitorInfo(hMonitor, ref info))
            {
                var mi = new MonitorInfo
                {
                    DeviceName = info.szDevice ?? "",
                    Left = info.rcMonitor.Left,
                    Top = info.rcMonitor.Top,
                    Right = info.rcMonitor.Right,
                    Bottom = info.rcMonitor.Bottom
                };
                if (GetDpiForMonitor(hMonitor, 0, out uint dx, out uint dy) == 0)
                {
                    mi.DpiX = dx;
                    mi.DpiY = dy;
                }
                list.Add(mi);
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }

    public static MonitorInfo? FindByDevice(string deviceName)
    {
        var monitors = GetMonitors();
        if (monitors.Count == 0)
            return null;
        var match = monitors.FirstOrDefault(m => string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        return match ?? monitors[0];
    }
}
