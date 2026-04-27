using NWaves.Features;
using NWaves.Signals;

namespace SmartJam.Services;

/// <summary>
/// Service de détection de pitch (hauteur sonore) utilisant l'algorithme YIN (NWaves).
///
/// Utilisation :
///   1) Crée une instance : var service = new PitchDetectorService(sampleRate: 44100);
///   2) Appelle DetectPitch(samples) à chaque buffer audio reçu.
///   3) Le résultat est (Hz, noteName) ou (0, "—") si rien n'est détecté.
///
/// Algorithme YIN :
///   - Prend un tableau de float (échantillons audio normalisés [-1..1])
///   - Retourne la fréquence fondamentale en Hz
///   - Référence : de Cheveigné & Kawahara (2002)
/// </summary>
public class PitchDetectorService
{
    private readonly int _sampleRate;

    // Fréquences limites : en dehors, le pitch est ignoré
    private const float MinHz = 60f;
    private const float MaxHz = 1200f;

    // Seuil CMDF YIN (0.15 = bon équilibre précision/bruit)
    private const float YinThreshold = 0.15f;

    // Noms des notes pour la conversion Hz → note
    private static readonly string[] NoteNames =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public PitchDetectorService(int sampleRate = 44100)
    {
        _sampleRate = sampleRate;
    }

    /// <summary>
    /// Détecte le pitch dans un buffer de samples.
    /// </summary>
    /// <param name="samples">Buffer audio (float[], normalisé -1..1)</param>
    /// <returns>
    /// (hz, noteName) : fréquence détectée et nom de la note.
    /// Si aucun pitch clair, retourne (0f, "—").
    /// </returns>
    public (float Hz, string NoteName) DetectPitch(float[] samples)
    {
        if (samples.Length < 512) return (0f, "—");

        var signal = new DiscreteSignal(_sampleRate, samples);
        float hz = Pitch.FromYin(signal, 0, samples.Length, MinHz, MaxHz, YinThreshold);

        if (hz < MinHz || hz > MaxHz)
            return (0f, "—");

        return (hz, HzToNoteName(hz));
    }

    /// <summary>
    /// Convertit une fréquence Hz en nom de note (ex. "A4", "C4").
    /// </summary>
    public static string HzToNoteName(float hz)
    {
        if (hz <= 0) return "—";
        double midi    = 12.0 * Math.Log2(hz / 440.0) + 69.0;
        int midiNote   = (int)Math.Round(midi);
        int noteClass  = ((midiNote % 12) + 12) % 12;
        int octave     = midiNote / 12 - 1;
        return $"{NoteNames[noteClass]}{octave}";
    }
}
