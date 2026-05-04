using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartJam.Services;

namespace SmartJam.ViewModels;

/// <summary>
/// ViewModel principal de SmartJam.
///
/// Suit le pattern MVVM (Model-View-ViewModel) :
///   - Les propriétés bindées (ObservableProperty) mettent à jour l'UI automatiquement.
///   - Les commandes (RelayCommand) sont déclenchées par les boutons.
///   - Les services (PitchDetectorService, AccompanimentGeneratorService) contiennent
///     la logique métier — le ViewModel ne fait que les appeler et exposer les résultats.
///
/// Sources audio disponibles (mode offline — sans micro) :
///   Sine : génère un sinus à la fréquence choisie
///   WAV  : (prévu — décommenter quand WAV reader intégré)
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    // --- Services (logique métier) ---
    private readonly PitchDetectorService        _pitchService      = new(sampleRate: 44100);
    private readonly AccompanimentGeneratorService _accompanimentService = new();

    // =========================================================
    // Propriétés "Source audio" (entrée)
    // =========================================================

    /// <summary>Source sélectionnée : "Sine" ou "WAV".</summary>
    [ObservableProperty]
    private string _selectedSource = "Sine";

    /// <summary>Fréquence du sinus généré (Hz). Visible uniquement en mode Sine.</summary>
    [ObservableProperty]
    private float _sineFrequency = 440f;

    // =========================================================
    // Propriétés "Pitch détecté" (sortie du service pitch)
    // =========================================================

    /// <summary>Fréquence fondamentale détectée (ex. "440.0 Hz").</summary>
    [ObservableProperty]
    private string _detectedFrequency = "— Hz";

    /// <summary>Nom de la note détectée (ex. "A4").</summary>
    [ObservableProperty]
    private string _detectedNote = "—";

    // =========================================================
    // Propriétés "Accompagnement" (paramètres + sortie)
    // =========================================================

    /// <summary>Tonalité musicale (ex. "C", "G", "Am").</summary>
    [ObservableProperty]
    private string _selectedKey = "C";

    /// <summary>Style musical : "pop", "jazz" ou "blues".</summary>
    [ObservableProperty]
    private string _selectedStyle = "pop";

    /// <summary>Tempo en BPM.</summary>
    [ObservableProperty]
    private int _tempo = 120;

    /// <summary>Nombre de mesures à générer.</summary>
    [ObservableProperty]
    private int _bars = 8;

    /// <summary>Progression générée (affichée dans l'UI).</summary>
    [ObservableProperty]
    private string _generatedProgression = "(aucune progression — cliquer sur Générer)";

    /// <summary>Message de statut affiché en bas de l'écran.</summary>
    [ObservableProperty]
    private string _statusMessage = "Prêt.";

    // =========================================================
    // Commandes (déclenchées par les boutons de l'UI)
    // =========================================================

    /// <summary>
    /// Analyse le pitch depuis la source sélectionnée (sinus ou WAV).
    /// Met à jour DetectedFrequency et DetectedNote.
    /// </summary>
    [RelayCommand]
    private void AnalysePitch()
    {
        StatusMessage = "Analyse du pitch en cours...";

        float[] samples = SelectedSource == "Sine"
            ? SineAudioSource.Generate(SineFrequency, sampleRate: 44100, durationSeconds: 1.0f)
            : Array.Empty<float>(); // WAV non implémenté dans ce squelette

        if (samples.Length == 0)
        {
            StatusMessage = "Erreur : source vide ou non supportée.";
            DetectedFrequency = "— Hz";
            DetectedNote = "—";
            return;
        }

        var (hz, noteName) = _pitchService.DetectPitch(samples);

        if (hz > 0)
        {
            DetectedFrequency = $"{hz:F1} Hz";
            DetectedNote      = noteName;
            StatusMessage     = $"Pitch détecté : {hz:F1} Hz → {noteName}";
        }
        else
        {
            DetectedFrequency = "— Hz";
            DetectedNote      = "—";
            StatusMessage     = "Aucun pitch détecté (silence ou bruit).";
        }
    }

    /// <summary>
    /// Génère une progression d'accords selon les paramètres actuels.
    /// Met à jour GeneratedProgression.
    /// </summary>
    [RelayCommand]
    private void GenerateProgression()
    {
        StatusMessage = "Génération de la progression...";

        var chords = _accompanimentService.Generate(
            key:   SelectedKey,
            style: SelectedStyle,
            bars:  Bars
        );

        GeneratedProgression = string.Join("\n", chords.Select(c => c.ToString()));
        StatusMessage = $"Progression {SelectedStyle.ToUpper()} en {SelectedKey} générée ({Bars} mesures, {Tempo} BPM).";
    }
}

