using System.Windows;
using ScreenRecorderLib;

namespace ScreenRecorder.Core;

public enum CaptureKind
{
    FullScreen,
    Window,
    Region
}

public sealed class RecordingConfig
{
    public CaptureKind Kind { get; set; } = CaptureKind.FullScreen;
    public string DisplayDeviceName { get; set; } = "";
    public IntPtr WindowHandle { get; set; }
    public ScreenRect Region { get; set; } = ScreenRect.Empty;
    public bool SystemAudio { get; set; } = true;
    public bool Microphone { get; set; }
    public string PlaybackDevice { get; set; } = "";
    public string MicrophoneDevice { get; set; } = "";
    public int Framerate { get; set; } = 30;
    public int BitrateKbps { get; set; } = 8000;
    public bool HardwareEncoding { get; set; } = true;
    public string Encoder { get; set; } = "H264";
}

public sealed class RecorderService : IDisposable
{
    private Recorder? _recorder;

    public event Action<RecorderStatus>? StatusChanged;
    public event Action<string>? Completed;
    public event Action<string>? Failed;

    public RecorderStatus Status => _recorder?.Status ?? RecorderStatus.Idle;

    public void Start(RecordingConfig cfg, string path)
    {
        Dispose();

        var source = BuildSource(cfg);

        var options = new RecorderOptions
        {
            SourceOptions = new SourceOptions
            {
                RecordingSources = new List<RecordingSourceBase> { source }
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                Encoder = BuildEncoder(cfg),
                Framerate = cfg.Framerate,
                Bitrate = cfg.BitrateKbps,
                IsHardwareEncodingEnabled = cfg.HardwareEncoding,
                IsLowLatencyEnabled = true,
                IsFixedFramerate = false,
                IsThrottlingDisabled = false,
                IsMp4FastStartEnabled = true,
                IsFragmentedMp4Enabled = false
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = cfg.SystemAudio || cfg.Microphone,
                Channels = AudioChannels.Stereo,
                Bitrate = AudioBitrate.bitrate_192kbps,
                AudioSources = BuildAudioSources(cfg)
            },
            MouseOptions = new MouseOptions(),
            OutputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video
            },
            LogOptions = new LogOptions
            {
                IsLogEnabled = false
            }
        };

        _recorder = Recorder.CreateRecorder(options);
        _recorder.OnStatusChanged += (_, e) => StatusChanged?.Invoke(e.Status);
        _recorder.OnRecordingComplete += (_, e) => Completed?.Invoke(e.FilePath);
        _recorder.OnRecordingFailed += (_, e) => Failed?.Invoke(e.Error);
        _recorder.Record(path);
    }

    public void Stop()
    {
        _recorder?.Stop();
    }

    public void Pause()
    {
        _recorder?.Pause();
    }

    public void Resume()
    {
        _recorder?.Resume();
    }

    private static IVideoEncoder BuildEncoder(RecordingConfig cfg)
    {
        if (string.Equals(cfg.Encoder, "H265", StringComparison.OrdinalIgnoreCase))
            return new H265VideoEncoder { BitrateMode = H265BitrateControlMode.Quality, EncoderProfile = H265Profile.Main };
        return new H264VideoEncoder { BitrateMode = H264BitrateControlMode.Quality, EncoderProfile = H264Profile.High };
    }

    private static RecordingSourceBase BuildSource(RecordingConfig cfg)
    {
        switch (cfg.Kind)
        {
            case CaptureKind.Window:
                return new WindowRecordingSource(cfg.WindowHandle)
                {
                    IsCursorCaptureEnabled = true,
                    IsBorderRequired = false
                };
            case CaptureKind.Region:
            {
                var display = new DisplayRecordingSource(cfg.DisplayDeviceName)
                {
                    IsCursorCaptureEnabled = true,
                    IsBorderRequired = false
                };
                if (!cfg.Region.Equals(ScreenRect.Empty))
                    display.SourceRect = cfg.Region;
                return display;
            }
            default:
                return new DisplayRecordingSource(cfg.DisplayDeviceName)
                {
                    IsCursorCaptureEnabled = true,
                    IsBorderRequired = false
                };
        }
    }

    private static List<AudioSourceBase> BuildAudioSources(RecordingConfig cfg)
    {
        var list = new List<AudioSourceBase>();
        if (cfg.SystemAudio)
        {
            var loop = string.IsNullOrEmpty(cfg.PlaybackDevice) ? LoopbackAudioSource.Default : new LoopbackAudioSource(cfg.PlaybackDevice);
            loop.IsAudioCaptureEnabled = true;
            loop.Volume = 1f;
            list.Add(loop);
        }
        if (cfg.Microphone)
        {
            var mic = string.IsNullOrEmpty(cfg.MicrophoneDevice) ? CaptureAudioSource.Default : new CaptureAudioSource(cfg.MicrophoneDevice);
            mic.IsAudioCaptureEnabled = true;
            mic.Volume = 1f;
            list.Add(mic);
        }
        return list;
    }

    public void Dispose()
    {
        if (_recorder == null)
            return;
        try { _recorder.Dispose(); } catch { }
        _recorder = null;
    }
}
