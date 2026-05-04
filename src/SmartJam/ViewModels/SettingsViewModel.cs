using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.CoreAudioApi;
using SmartJam.Audio;

namespace SmartJam.ViewModels;

/// <summary>
/// ViewModel de la fenêtre de paramètres audio.
/// Expose les listes de drivers/périphériques et permet d'appliquer la configuration à l'AudioEngine.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly AudioEngine _engine;

    // ── Drivers ──────────────────────────────────────────────────────────────

    public IReadOnlyList<string> AudioDrivers { get; } =
        ["WASAPI Shared", "WASAPI Exclusive", "ASIO"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWasapiSelected))]
    [NotifyPropertyChangedFor(nameof(IsAsioSelected))]
    private string _selectedAudioDriver = "WASAPI Shared";

    public bool IsWasapiSelected => !IsAsioSelected;
    public bool IsAsioSelected   => SelectedAudioDriver == "ASIO";

    // ── WASAPI — périphériques ────────────────────────────────────────────────

    public ObservableCollection<string> InputDevices  { get; } = [];
    public ObservableCollection<string> OutputDevices { get; } = [];

    // Raw device lists (parallel to display names)
    private readonly List<MMDevice> _inputDeviceObjects  = [];
    private readonly List<MMDevice> _outputDeviceObjects = [];

    [ObservableProperty]
    private string? _selectedInputDevice;

    [ObservableProperty]
    private string? _selectedOutputDevice;

    // ── ASIO — drivers ───────────────────────────────────────────────────────

    public ObservableCollection<string> AsioDrivers { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AsioMaxInputOffset))]
    [NotifyPropertyChangedFor(nameof(AsioMaxOutputOffset))]
    private string? _selectedAsioDriver;

    // ── ASIO — canaux ────────────────────────────────────────────────────────

    [ObservableProperty]
    private int _asioInputOffset;

    [ObservableProperty]
    private int _asioOutputOffset;

    private int _asioMaxInputOffset  = 7;
    private int _asioMaxOutputOffset = 7;

    public int AsioMaxInputOffset
    {
        get => _asioMaxInputOffset;
        private set => SetProperty(ref _asioMaxInputOffset, value);
    }

    public int AsioMaxOutputOffset
    {
        get => _asioMaxOutputOffset;
        private set => SetProperty(ref _asioMaxOutputOffset, value);
    }

    // ── Sample Rate ───────────────────────────────────────────────────────────

    public IReadOnlyList<int> SampleRates { get; } = [44100, 48000, 96000];

    [ObservableProperty]
    private int _selectedSampleRate = 44100;

    // ── Buffer Size ───────────────────────────────────────────────────────────

    public IReadOnlyList<int> BufferSizes { get; } = [64, 128, 256, 512, 1024];

    [ObservableProperty]
    private int _selectedBufferSize = 256;

    // ── Status ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // ── Construction ─────────────────────────────────────────────────────────

    public SettingsViewModel(AudioEngine engine)
    {
        _engine = engine;

        // Initialise les listes de périphériques
        RefreshDevices();

        // Pré-sélectionne les valeurs actuelles du moteur
        SelectedSampleRate  = engine.SampleRate;
        SelectedBufferSize  = engine.BufferSize;
        SelectedAudioDriver = engine.Driver switch
        {
            AudioDriver.WASAPI_Exclusive => "WASAPI Exclusive",
            AudioDriver.ASIO             => "ASIO",
            _                            => "WASAPI Shared"
        };

        var routing = engine.GetAsioRouting();
        AsioInputOffset  = routing.inputOffset;
        AsioOutputOffset = routing.outputOffset;

        // Si un driver ASIO est déjà sélectionné, on sonde ses canaux
        if (SelectedAsioDriver != null)
            ProbeAndUpdateAsioChannels(SelectedAsioDriver);
    }

    // ── Handlers de changement de propriété ──────────────────────────────────

    partial void OnSelectedAsioDriverChanged(string? value)
    {
        if (value != null)
            ProbeAndUpdateAsioChannels(value);
    }

    private void ProbeAndUpdateAsioChannels(string driverName)
    {
        try
        {
            var (inCnt, outCnt) = _engine.ProbeAsioChannels(driverName);
            AsioMaxInputOffset  = Math.Max(0, inCnt  - 1);
            AsioMaxOutputOffset = Math.Max(0, outCnt - 1);
            if (AsioInputOffset  > AsioMaxInputOffset)  AsioInputOffset  = 0;
            if (AsioOutputOffset > AsioMaxOutputOffset) AsioOutputOffset = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Impossible de sonder les canaux ASIO : {ex.Message}";
        }
    }

    // ── Commandes ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenAsioControlPanel()
    {
        bool ok = _engine.ShowAsioControlPanel();
        StatusMessage = ok ? "Panneau de contrôle ASIO ouvert." : "Panneau ASIO indisponible ou non supporté par ce driver.";
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        // WASAPI
        _inputDeviceObjects.Clear();
        _outputDeviceObjects.Clear();
        InputDevices.Clear();
        OutputDevices.Clear();

        try
        {
            foreach (var dev in _engine.GetInputDevices())
            {
                _inputDeviceObjects.Add(dev);
                InputDevices.Add(dev.FriendlyName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Impossible de lister les entrées : {ex.Message}";
        }

        try
        {
            foreach (var dev in _engine.GetOutputDevices())
            {
                _outputDeviceObjects.Add(dev);
                OutputDevices.Add(dev.FriendlyName);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Impossible de lister les sorties : {ex.Message}";
        }

        // Pré-sélectionner les périphériques actuellement assignés dans le moteur
        if (_engine.InputDevice != null)
            SelectedInputDevice = _engine.InputDevice.FriendlyName;
        else if (InputDevices.Count > 0)
            SelectedInputDevice = InputDevices[0];

        if (_engine.OutputDevice != null)
            SelectedOutputDevice = _engine.OutputDevice.FriendlyName;
        else if (OutputDevices.Count > 0)
            SelectedOutputDevice = OutputDevices[0];

        // ASIO
        AsioDrivers.Clear();
        try
        {
            foreach (var drv in AudioEngine.GetAsioDrivers())
                AsioDrivers.Add(drv);
        }
        catch (PlatformNotSupportedException)
        { /* ASIO pas disponible sur tous les systèmes (Linux, etc.) */ }
        catch (Exception ex)
        {
            StatusMessage = $"Impossible de lister les drivers ASIO : {ex.Message}";
        }

        if (SelectedAsioDriver == null && AsioDrivers.Count > 0)
            SelectedAsioDriver = AsioDrivers[0];
    }

    /// <summary>Applique les paramètres sélectionnés à l'AudioEngine.</summary>
    [RelayCommand]
    private void Apply()
    {
        _engine.SampleRate  = SelectedSampleRate;
        _engine.BufferSize  = SelectedBufferSize;

        _engine.Driver = SelectedAudioDriver switch
        {
            "WASAPI Exclusive" => AudioDriver.WASAPI_Exclusive,
            "ASIO"             => AudioDriver.ASIO,
            _                  => AudioDriver.WASAPI_Shared
        };

        if (IsAsioSelected && SelectedAsioDriver != null)
        {
            _engine.SetAsioDriver(SelectedAsioDriver);
            _engine.SetAsioRouting(AsioInputOffset, AsioOutputOffset,
                                   inputCount: 1, outputCount: 2);
        }

        if (IsWasapiSelected)
        {
            int inIdx  = SelectedInputDevice  != null ? InputDevices.IndexOf(SelectedInputDevice)  : -1;
            int outIdx = SelectedOutputDevice != null ? OutputDevices.IndexOf(SelectedOutputDevice) : -1;

            _engine.InputDevice  = inIdx  >= 0 ? _inputDeviceObjects[inIdx]  : null;
            _engine.OutputDevice = outIdx >= 0 ? _outputDeviceObjects[outIdx] : null;
        }

        _engine.RebuildAudioGraph();
        StatusMessage = "Paramètres appliqués.";
    }
}
