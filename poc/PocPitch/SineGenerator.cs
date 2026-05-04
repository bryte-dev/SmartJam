namespace SmartJam.PocPitch;

/// <summary>
/// Génère un signal sinusoïdal pur (test sans micro).
/// </summary>
public static class SineGenerator
{
    /// <summary>
    /// Crée un tableau de float représentant un sinus à la fréquence donnée.
    /// </summary>
    /// <param name="frequency">Fréquence en Hz (ex. 440 pour La4)</param>
    /// <param name="sampleRate">Taux d'échantillonnage en Hz (ex. 44100)</param>
    /// <param name="durationSeconds">Durée du signal en secondes</param>
    public static float[] Generate(float frequency, int sampleRate, float durationSeconds = 3.0f)
    {
        int totalSamples = (int)(sampleRate * durationSeconds);
        float[] signal = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            // x(t) = A * sin(2π * f * t)
            double t = (double)i / sampleRate;
            signal[i] = (float)Math.Sin(2.0 * Math.PI * frequency * t);
        }

        return signal;
    }
}
