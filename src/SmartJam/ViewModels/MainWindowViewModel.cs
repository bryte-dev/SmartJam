using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartJam.Audio;
using SmartJam.Services;
using SmartJam.Views;

namespace SmartJam.ViewModels;

/// <summary>
/// ViewModel principal de SmartJam — MVVM avec CommunityToolkit.
///
/// Responsabilités :
///   - Gérer AudioEngine (démarrer / arrêter le monitoring).
///   - Exposer les bindings UI (mode, oscillateur, RMS, pitch, historique, log).
///   - Accumuler les trames audio et déclencher PitchDetectorService.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    // ── Services ─────────────────────────────────────────────────────────────

    private readonly AudioEngine          _engine       = new();
    private readonly PitchDetectorService _pitchService = new(sampleRate: 44100);
    // Cache des services pitch pour d'autres fréquences d'échantillonnage
    private readonly Dictionary<int, PitchDetectorService> _pitchServiceCache = new();

    // Buffer d'accumulation pour la détection de pitch
    private readonly List<float> _sampleBuffer = new();
    private const int AnalysisWindowSize = 4096;   // ~93 ms à 44100 Hz
    private const int MaxPlayedNotes     = 20;
    private const int MaxLogMessages     = 150;
    // Facteur d'échelle RMS → ProgressBar (signal typique ~0.1–0.3 RMS)
    private const double RmsVisualScale  = 4.0;

    private bool _disposed;

    // ── Mode ─────────────────────────────────────────────────────────────────

    public IReadOnlyList<string> Modes { get; } = ["Live", "TestOscillator"];

    [ObservableProperty]
    private string _selectedMode = "TestOscillator";

    [ObservableProperty]
    private bool _isOscillatorPanelVisible = true;

    // ── Paramètres oscillateur ────────────────────────────────────────────────

    [ObservableProperty]
    private double _oscFrequency = 440.0;

    [ObservableProperty]
    private double _oscAmplitude = 0.5;

    // ── Monitoring ───────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _monitoringButtonText = "Monitoring ON";

    // ── Mesures ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private double _inputLevel;   // RMS, [0..1]

    [ObservableProperty]
    private double _peakLevel;    // Peak hold, [0..1]

    // ── Pitch détecté ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _detectedFrequency = "— Hz";

    [ObservableProperty]
    private string _detectedNote = "—";

    // ── Analyse (placeholders — futurs services) ──────────────────────────────

    [ObservableProperty]
    private string _possibleKey = "—";

    [ObservableProperty]
    private string _possibleChord = "—";

    // ── Collections ──────────────────────────────────────────────────────────

    public ObservableCollection<string> PlayedNotes  { get; } = [];
    public ObservableCollection<string> LogMessages  { get; } = [];

    // ── Constructeur ─────────────────────────────────────────────────────────

    public MainWindowViewModel()
    {
        _engine.OnAudioFrame     += HandleAudioFrame;
        _engine.OnMetersUpdated  += HandleMeters;
        _engine.OnLog            += HandleEngineLog;
        AddLog("SmartJam prêt. Sélectionner un mode et cliquer sur Monitoring ON.");
    }

    // ── Handlers de changement de propriété (CommunityToolkit partial) ────────

    partial void OnSelectedModeChanged(string value)
    {
        IsOscillatorPanelVisible = value == "TestOscillator";

        if (IsMonitoring)
        {
            // Redémarrer avec le nouveau mode
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

    partial void OnOscFrequencyChanged(double value)
        => _engine.OscillatorFrequency = (float)value;

    partial void OnOscAmplitudeChanged(double value)
        => _engine.OscillatorAmplitude = (float)value;

    // ── Commandes ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _engine.StopMonitoring();
            IsMonitoring         = false;
            MonitoringButtonText = "Monitoring ON";
            InputLevel           = 0;
            PeakLevel            = 0;
        }
        else
        {
            ApplyEngineSettings();
            try
            {
                _engine.StartMonitoring();
                IsMonitoring         = true;
                MonitoringButtonText = "Monitoring OFF";
            }
            catch (Exception ex)
            {
                AddLog($"Impossible de démarrer : {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        var settingsVm = new SettingsViewModel(_engine);
        var window     = new SettingsWindow { DataContext = settingsVm };

        // Récupère la fenêtre principale comme owner pour centrer la modale
        var mainWin = Avalonia.Application.Current?
            .ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime
            ? lifetime.MainWindow
            : null;

        if (mainWin != null)
            await window.ShowDialog(mainWin);
        else
            window.Show();

        if (settingsVm.StatusMessage == "Paramètres appliqués.")
        {
            AddLog($"Settings appliqués : {settingsVm.SelectedAudioDriver}, " +
                   $"{settingsVm.SelectedSampleRate} Hz, buffer {settingsVm.SelectedBufferSize}");

            if (IsMonitoring)
            {
                _engine.StopMonitoring();
                IsMonitoring         = false;
                MonitoringButtonText = "Monitoring ON";
                InputLevel           = 0;
                PeakLevel            = 0;
                AddLog("Monitoring arrêté — relancer après avoir changé les settings.");
            }
        }
    }

    // ── Logique audio (callbacks sur thread NAudio) ───────────────────────────

    private void HandleAudioFrame(float[] samples, int frames, int sampleRate)
    {
        lock (_sampleBuffer)
        {
            // Évite l'allocation d'un énumérateur LINQ — utilise ArraySegment
            _sampleBuffer.AddRange(new ArraySegment<float>(samples, 0, Math.Min(frames, samples.Length)));

            if (_sampleBuffer.Count >= AnalysisWindowSize)
            {
                var batch = _sampleBuffer.GetRange(0, AnalysisWindowSize).ToArray();
                _sampleBuffer.RemoveRange(0, AnalysisWindowSize);

                // Détection de pitch sur un thread séparé (non bloquant)
                Task.Run(() => RunPitchDetection(batch, sampleRate));
            }
        }
    }

    private void HandleMeters(float rms, float peak)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // RmsVisualScale : amplifie le RMS pour une meilleure lisibilité (signal typique ~0.1–0.3)
            InputLevel = Math.Min(1.0, rms * RmsVisualScale);
            PeakLevel  = Math.Min(1.0, peak);
        });
    }

    private void HandleEngineLog(string message)
        => Dispatcher.UIThread.Post(() => AddLog(message));

    // ── Détection de pitch ────────────────────────────────────────────────────

    private void RunPitchDetection(float[] samples, int sampleRate)
    {
        // Récupère ou crée un service pour ce taux d'échantillonnage
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
                DetectedNote      = note;

                // Ajouter à l'historique seulement si la note change
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
                DetectedNote      = "—";
            }
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ApplyEngineSettings()
    {
        _engine.Mode                 = SelectedMode == "Live" ? AudioMode.Live : AudioMode.TestOscillator;
        _engine.OscillatorFrequency  = (float)OscFrequency;
        _engine.OscillatorAmplitude  = (float)OscAmplitude;
    }

    private void AddLog(string message)
    {
        LogMessages.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        while (LogMessages.Count > MaxLogMessages)
            LogMessages.RemoveAt(LogMessages.Count - 1);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _engine.OnAudioFrame    -= HandleAudioFrame;
        _engine.OnMetersUpdated -= HandleMeters;
        _engine.OnLog           -= HandleEngineLog;
        _engine.StopAudio();
    }
}

