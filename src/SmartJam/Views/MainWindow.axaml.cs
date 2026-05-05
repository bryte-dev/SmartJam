using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SmartJam.Audio;
using SmartJam.Services;

namespace SmartJam.Views;

public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
{
    private readonly AudioEngine _engine = new();
    private readonly PitchDetectorService _pitchService = new(sampleRate: 44100);
    private readonly Dictionary<int, PitchDetectorService> _pitchServiceCache = new();
    private readonly List<float> _sampleBuffer = new();

    private const int AnalysisWindowSize = 4096;
    private const int MaxPlayedNotes = 20;
    private const int MaxLogMessages = 150;
    private const double MinDb = -60.0;

    private bool _disposed;
    private bool _isDraggingOscFrequency;
    private Point _oscDragStartPoint;
    private double _oscDragStartFrequency;
    private int _oscDragAccumulatedPixels;

    private const int OscDragPixelsPerStep = 2;
    private const double OscDragStepHz = 2.0;
    private const double OscDragStepHzFast = 10.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> Modes { get; } = ["Live", "TestOscillator"];

    private string _selectedMode = "TestOscillator";
    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (!SetField(ref _selectedMode, value))
                return;

            IsOscillatorPanelVisible = value == "TestOscillator";

            if (IsMonitoring)
            {
                _engine.StopAudio();
                ApplyEngineSettings();

                try
                {
                    _engine.StartAudio();
                }
                catch (Exception ex)
                {
                    AddLog($"Erreur redémarrage : {ex.Message}");
                    IsMonitoring = false;
                    MonitoringButtonText = "Monitoring ON";
                }
            }
        }
    }

    private bool _isOscillatorPanelVisible = true;
    public bool IsOscillatorPanelVisible
    {
        get => _isOscillatorPanelVisible;
        set => SetField(ref _isOscillatorPanelVisible, value);
    }

    private double? _oscFrequency = 440.0;
    public double? OscFrequency
    {
        get => _oscFrequency;
        set
        {
            if (!SetField(ref _oscFrequency, value) || value is null)
                return;

            _engine.OscillatorFrequency = (float)value.Value;
        }
    }

    private double _oscAmplitude = 0.5;
    public double OscAmplitude
    {
        get => _oscAmplitude;
        set
        {
            if (!SetField(ref _oscAmplitude, value))
                return;

            _engine.OscillatorAmplitude = (float)value;
        }
    }

    private bool _isMonitoring;
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetField(ref _isMonitoring, value);
    }

    private string _monitoringButtonText = "Monitoring ON";
    public string MonitoringButtonText
    {
        get => _monitoringButtonText;
        set => SetField(ref _monitoringButtonText, value);
    }

    private double _inputLevel;
    public double InputLevel
    {
        get => _inputLevel;
        set => SetField(ref _inputLevel, value);
    }

    private double _peakLevel;
    public double PeakLevel
    {
        get => _peakLevel;
        set => SetField(ref _peakLevel, value);
    }

    private string _inputLevelDb = "-inf dBFS";
    public string InputLevelDb
    {
        get => _inputLevelDb;
        set => SetField(ref _inputLevelDb, value);
    }

    private string _peakLevelDb = "-inf dBFS";
    public string PeakLevelDb
    {
        get => _peakLevelDb;
        set => SetField(ref _peakLevelDb, value);
    }

    private string _rmsMeterColor = "#1BB155";
    public string RmsMeterColor
    {
        get => _rmsMeterColor;
        set => SetField(ref _rmsMeterColor, value);
    }

    private string _peakMeterColor = "#25B860";
    public string PeakMeterColor
    {
        get => _peakMeterColor;
        set => SetField(ref _peakMeterColor, value);
    }

    private string _detectedFrequency = "— Hz";
    public string DetectedFrequency
    {
        get => _detectedFrequency;
        set => SetField(ref _detectedFrequency, value);
    }

    private string _detectedNote = "—";
    public string DetectedNote
    {
        get => _detectedNote;
        set => SetField(ref _detectedNote, value);
    }

    private string _possibleKey = "—";
    public string PossibleKey
    {
        get => _possibleKey;
        set => SetField(ref _possibleKey, value);
    }

    private string _possibleChord = "—";
    public string PossibleChord
    {
        get => _possibleChord;
        set => SetField(ref _possibleChord, value);
    }

    private string _audioDriverLabel = "WASAPI Shared";
    public string AudioDriverLabel
    {
        get => _audioDriverLabel;
        set => SetField(ref _audioDriverLabel, value);
    }

    private string _sampleRateLabel = "44100 Hz";
    public string SampleRateLabel
    {
        get => _sampleRateLabel;
        set => SetField(ref _sampleRateLabel, value);
    }

    private string _bufferSizeLabel = "256";
    public string BufferSizeLabel
    {
        get => _bufferSizeLabel;
        set => SetField(ref _bufferSizeLabel, value);
    }

    public ObservableCollection<string> PlayedNotes { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    public IRelayCommand ToggleMonitoringCommand { get; }
    public IAsyncRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand ResetPlayedNotesCommand { get; }

    public MainWindow()
    {
        InitializeComponent();

        ToggleMonitoringCommand = new RelayCommand(ToggleMonitoring);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync);
        ResetPlayedNotesCommand = new RelayCommand(ResetPlayedNotes);

        DataContext = this;

        _engine.OnAudioFrame += HandleAudioFrame;
        _engine.OnMetersUpdated += HandleMeters;
        _engine.OnLog += HandleEngineLog;

        RefreshSettingsLabels();
        AddLog("SmartJam prêt. Sélectionner un mode et cliquer sur Monitoring ON.");
    }

    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _engine.StopMonitoring();
            IsMonitoring = false;
            MonitoringButtonText = "Monitoring ON";
            InputLevel = 0;
            PeakLevel = 0;
            return;
        }

        ApplyEngineSettings();
        try
        {
            _engine.StartMonitoring();
            IsMonitoring = true;
            MonitoringButtonText = "Monitoring OFF";
        }
        catch (Exception ex)
        {
            AddLog($"Impossible de démarrer : {ex.Message}");
        }
    }

    private async Task OpenSettingsAsync()
    {
        var settingsVm = new ViewModels.SettingsViewModel(_engine);
        var window = new SettingsWindow { DataContext = settingsVm };

        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var mainWin = lifetime?.MainWindow;

        if (mainWin != null)
            await window.ShowDialog(mainWin);
        else
            window.Show();

        if (settingsVm.StatusMessage == "Paramètres appliqués.")
        {
            AddLog($"Settings appliqués : {settingsVm.SelectedAudioDriver}, {settingsVm.SelectedSampleRate} Hz, buffer {settingsVm.SelectedBufferSize}");
            IsMonitoring = _engine.IsMonitoring;
            MonitoringButtonText = _engine.IsMonitoring ? "Monitoring OFF" : "Monitoring ON";
            RefreshSettingsLabels();
        }
    }

    private void ResetPlayedNotes()
    {
        PlayedNotes.Clear();
        AddLog("Historique des notes réinitialisé.");
    }

    private void HandleAudioFrame(float[] samples, int frames, int sampleRate)
    {
        lock (_sampleBuffer)
        {
            _sampleBuffer.AddRange(new ArraySegment<float>(samples, 0, Math.Min(frames, samples.Length)));

            if (_sampleBuffer.Count < AnalysisWindowSize)
                return;

            var batch = _sampleBuffer.GetRange(0, AnalysisWindowSize).ToArray();
            _sampleBuffer.RemoveRange(0, AnalysisWindowSize);

            Task.Run(() => RunPitchDetection(batch, sampleRate));
        }
    }

    private void HandleMeters(float rms, float peak)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var rmsDb = ToDb(rms);
            var peakDb = ToDb(peak);

            InputLevel = DbToMeterValue(rmsDb);
            PeakLevel = DbToMeterValue(peakDb);

            InputLevelDb = FormatDb(rmsDb);
            PeakLevelDb = FormatDb(peakDb);

            RmsMeterColor = GetMeterColor(rmsDb);
            PeakMeterColor = GetMeterColor(peakDb);
        });
    }

    private void HandleEngineLog(string message)
        => Dispatcher.UIThread.Post(() => AddLog(message));

    private void OnOscFrequencyPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control dragZone)
            return;

        var point = e.GetCurrentPoint(dragZone);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        _isDraggingOscFrequency = true;
        _oscDragStartPoint = point.Position;
        _oscDragStartFrequency = OscFrequency ?? 440.0;
        _oscDragAccumulatedPixels = 0;
        e.Pointer.Capture(dragZone);
        e.Handled = true;
    }

    private void OnOscFrequencyPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDraggingOscFrequency || sender is not Control dragZone)
            return;

        var point = e.GetCurrentPoint(dragZone);
        int deltaPixels = (int)Math.Round(_oscDragStartPoint.Y - point.Position.Y);
        int netPixels = deltaPixels - _oscDragAccumulatedPixels;

        if (Math.Abs(netPixels) < OscDragPixelsPerStep)
            return;

        int steps = netPixels / OscDragPixelsPerStep;
        _oscDragAccumulatedPixels += steps * OscDragPixelsPerStep;

        double stepHz = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? OscDragStepHzFast : OscDragStepHz;
        double next = (_oscDragStartFrequency + (steps * stepHz));
        next = Math.Clamp(next, 20.0, 4000.0);

        _oscDragStartFrequency = next;
        OscFrequency = next;
        e.Handled = true;
    }

    private void OnOscFrequencyPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control)
            e.Pointer.Capture(null);

        _isDraggingOscFrequency = false;
        _oscDragAccumulatedPixels = 0;
    }

    private void OnOscFrequencyPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isDraggingOscFrequency = false;
        _oscDragAccumulatedPixels = 0;
    }

    private void RunPitchDetection(float[] samples, int sampleRate)
    {
        PitchDetectorService service;
        if (sampleRate == 44100)
        {
            service = _pitchService;
        }
        else
        {
            lock (_pitchServiceCache)
            {
                if (!_pitchServiceCache.TryGetValue(sampleRate, out service!))
                {
                    service = new PitchDetectorService(sampleRate);
                    _pitchServiceCache[sampleRate] = service;
                }
            }
        }

        var (hz, note) = service.DetectPitch(samples);

        Dispatcher.UIThread.Post(() =>
        {
            if (hz > 0)
            {
                DetectedFrequency = $"{hz:F2} Hz";
                DetectedNote = note;

                if (PlayedNotes.Count == 0 || PlayedNotes[0] != note)
                {
                    PlayedNotes.Insert(0, note);
                    if (PlayedNotes.Count > MaxPlayedNotes)
                        PlayedNotes.RemoveAt(PlayedNotes.Count - 1);
                }
            }
            else
            {
                DetectedFrequency = "— Hz";
                DetectedNote = "—";
            }
        });
    }

    private void ApplyEngineSettings()
    {
        _engine.Mode = SelectedMode == "Live" ? AudioMode.Live : AudioMode.TestOscillator;
        _engine.OscillatorFrequency = (float)(OscFrequency ?? 440.0);
        _engine.OscillatorAmplitude = (float)OscAmplitude;
    }

    private void AddLog(string message)
    {
        LogMessages.Insert(0, message);
        while (LogMessages.Count > MaxLogMessages)
            LogMessages.RemoveAt(LogMessages.Count - 1);
    }

    private void RefreshSettingsLabels()
    {
        AudioDriverLabel = _engine.Driver switch
        {
            AudioDriver.WASAPI_Exclusive => "WASAPI Exclusive",
            AudioDriver.ASIO => "ASIO",
            _ => "WASAPI Shared"
        };
        SampleRateLabel = $"{_engine.SampleRate} Hz";
        BufferSizeLabel = _engine.BufferSize.ToString();
    }

    private static double ToDb(float value)
    {
        var safe = Math.Max(0.000001f, value);
        return 20.0 * Math.Log10(safe);
    }

    private static double DbToMeterValue(double db)
    {
        var clamped = Math.Clamp(db, MinDb, 0.0);
        return (clamped - MinDb) / -MinDb;
    }

    private static string FormatDb(double db)
    {
        if (db <= MinDb)
            return "-inf dBFS";

        return $"{db.ToString("F1", CultureInfo.InvariantCulture)} dBFS";
    }

    private static string GetMeterColor(double db)
    {
        if (db >= -6.0)
            return "#D44141";
        if (db >= -18.0)
            return "#F1C43A";
        return "#1BB155";
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _engine.OnAudioFrame -= HandleAudioFrame;
        _engine.OnMetersUpdated -= HandleMeters;
        _engine.OnLog -= HandleEngineLog;
        _engine.StopAudio();
    }
}