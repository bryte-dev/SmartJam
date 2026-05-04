using NAudio.Wave;

namespace SmartJam.Audio;

/// <summary>
/// Générateur de sinus (ISampleProvider).
/// Utilisé en mode TestOscillator pour produire un signal de test sans matériel.
/// Lève SamplesGenerated après chaque Read afin que l'AudioEngine puisse
/// alimenter OnAudioFrame et UpdateMeters.
/// </summary>
public class SineWaveProvider : ISampleProvider
{
    /// <summary>Fired après chaque appel Read. (samples, frameCount, sampleRate)</summary>
    public event Action<float[], int, int>? SamplesGenerated;

    public float Frequency { get; set; } = 440f;
    public float Amplitude  { get; set; } = 0.6f;

    private readonly WaveFormat _waveFormat;
    private double _phase;

    public SineWaveProvider(int sampleRate = 44100)
    {
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
    }

    public WaveFormat WaveFormat => _waveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        double inc = 2.0 * Math.PI * Frequency / _waveFormat.SampleRate;
        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = Amplitude * (float)Math.Sin(_phase);
            _phase += inc;
            if (_phase > 2.0 * Math.PI)
                _phase -= 2.0 * Math.PI;
        }

        if (count > 0 && SamplesGenerated != null)
        {
            var slice = new float[count];
            Array.Copy(buffer, offset, slice, 0, count);
            SamplesGenerated(slice, count, _waveFormat.SampleRate);
        }

        return count;
    }
}
