namespace SmartJam.Services;

/// <summary>
/// Génère un signal sinusoïdal pur en mémoire (test offline sans micro).
/// Permet de simuler l'entrée audio pour la détection de pitch.
/// </summary>
public static class SineAudioSource
{
    /// <summary>
    /// Génère un tableau de float[] représentant un sinus pur.
    /// </summary>
    /// <param name="frequency">Fréquence en Hz (ex. 440 pour La4)</param>
    /// <param name="sampleRate">Taux d'échantillonnage (ex. 44100)</param>
    /// <param name="durationSeconds">Durée en secondes</param>
    public static float[] Generate(float frequency, int sampleRate = 44100, float durationSeconds = 1.0f)
    {
        int count = (int)(sampleRate * durationSeconds);
        float[] buf = new float[count];
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / sampleRate;
            buf[i] = (float)Math.Sin(2.0 * Math.PI * frequency * t);
        }
        return buf;
    }
}
