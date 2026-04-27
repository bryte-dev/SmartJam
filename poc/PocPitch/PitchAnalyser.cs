using NWaves.Features;
using NWaves.Signals;

namespace SmartJam.PocPitch;

/// <summary>
/// Analyse de pitch sur un signal audio en utilisant l'algorithme YIN (NWaves).
///
/// YIN est une méthode classique de détection de pitch basée sur la fonction
/// de différence cumulée normalisée (CMDF). Elle est robuste sur les sons
/// "périodiques" comme les instruments à cordes ou la voix.
///
/// Référence : de Cheveigné & Kawahara (2002) "YIN, a fundamental frequency estimator"
/// Lib      : ar1st0crat/NWaves (https://github.com/ar1st0crat/NWaves)
/// API      : NWaves.Features.Pitch.FromYin(signal, startPos, endPos, low, high, threshold)
/// </summary>
public static class PitchAnalyser
{
    // Taille d'une fenêtre d'analyse (en échantillons).
    // À 44100 Hz, 4096 samples ≈ 93ms — bon compromis vitesse/précision.
    private const int WindowSize = 4096;

    // Décalage entre deux fenêtres consécutives (50 % de recouvrement).
    private const int HopSize = 2048;

    // Seuil CMDF de YIN : plus la valeur est faible, plus l'algo est exigeant.
    // 0.15 est une valeur typique (0 = parfait sinus, 1 = bruit pur).
    private const float YinThreshold = 0.15f;

    // Fréquence minimum attendue (en Hz). En dessous, on ignore.
    private const float MinHz = 60f;

    // Fréquence maximum attendue (en Hz). Au dessus, on ignore.
    private const float MaxHz = 1200f;

    /// <summary>
    /// Découpe le signal en fenêtres, applique YIN sur chaque fenêtre et
    /// affiche le résultat (temps, Hz, note) dans la console.
    /// </summary>
    public static void Analyse(float[] samples, int sampleRate)
    {
        // Encapsuler le tableau dans un DiscreteSignal NWaves
        var signal = new DiscreteSignal(sampleRate, samples);

        Console.WriteLine("Temps (s) | Fréquence (Hz) | Note");
        Console.WriteLine("----------+----------------+------");

        // Parcourir le signal fenêtre par fenêtre
        for (int start = 0; start + WindowSize <= samples.Length; start += HopSize)
        {
            int end = start + WindowSize;
            float timeSeconds = (float)start / sampleRate;

            // Appel YIN : retourne la fréquence fondamentale en Hz (0 si non trouvée)
            float hz = Pitch.FromYin(signal, start, end, MinHz, MaxHz, YinThreshold);

            if (hz < MinHz || hz > MaxHz)
            {
                // Silence ou absence de pitch clair dans cette fenêtre
                Console.WriteLine($"{timeSeconds,9:F2}s | {"—",14} | {"—",4}");
                continue;
            }

            string note = HzToNoteName(hz);
            Console.WriteLine($"{timeSeconds,9:F2}s | {hz,14:F1} Hz | {note}");
        }

        Console.WriteLine();
        Console.WriteLine("Analyse terminée.");
    }

    /// <summary>
    /// Convertit une fréquence en Hz vers le nom de la note la plus proche.
    /// Ex. : 440 Hz → "A4", 261.6 Hz → "C4"
    ///
    /// Formule : noteIndex = round(12 * log2(hz / 440)) + 69
    /// (convention MIDI : A4 = 69)
    /// </summary>
    public static string HzToNoteName(float hz)
    {
        if (hz <= 0) return "—";

        // Calculer le numéro MIDI le plus proche
        double midiNote = 12.0 * Math.Log2(hz / 440.0) + 69.0;
        int midiRounded = (int)Math.Round(midiNote);

        // Nom de la note (modulo 12)
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int noteClass = ((midiRounded % 12) + 12) % 12;
        int octave    = midiRounded / 12 - 1;

        return $"{noteNames[noteClass]}{octave}";
    }
}
