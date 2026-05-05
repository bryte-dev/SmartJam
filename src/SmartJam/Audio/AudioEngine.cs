using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SmartJam.Audio;

/// <summary>Driver audio à utiliser.</summary>
public enum AudioDriver
{
    WASAPI_Shared,
    WASAPI_Exclusive,
    ASIO
}

/// <summary>Mode de source audio.</summary>
public enum AudioMode
{
    /// <summary>Capture depuis le périphérique d'entrée sélectionné (micro / interface).</summary>
    Live,
    /// <summary>Génère un sinus interne pour tester sans matériel.</summary>
    TestOscillator
}

/// <summary>Fournisseur silence (IWaveProvider) — utilisé pour l'init ASIO.</summary>
internal class SilenceProvider : IWaveProvider
{
    public WaveFormat WaveFormat { get; }
    public SilenceProvider(int sampleRate, int channels)
        => WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    public int Read(byte[] buffer, int offset, int count)
    { Array.Clear(buffer, offset, count); return count; }
}

/// <summary>
/// Moteur audio SmartJam — adapté de AudioBlocks.App.Audio.AudioEngine.
///
/// Supprimé  : Effects, Recorder, Metronome.
/// Conservé  : monitoring WASAPI/ASIO (BufferedWaveProvider + WasapiOut / AsioOut).
/// Ajouté    : AudioMode (Live / TestOscillator), OnAudioFrame, OscillatorFrequency/Amplitude.
/// </summary>
public class AudioEngine
{
    // ── Configuration ────────────────────────────────────────────────────────

    public AudioDriver Driver   { get; set; } = AudioDriver.WASAPI_Shared;
    public AudioMode   Mode     { get; set; } = AudioMode.TestOscillator;
    public MMDevice?   InputDevice  { get; set; }
    public MMDevice?   OutputDevice { get; set; }
    public int SampleRate  { get; set; } = 44100;
    public int BufferSize  { get; set; } = 256;

    // ── Oscillateur (mode TestOscillator) ────────────────────────────────────

    private float _oscFrequency = 440f;
    private float _oscAmplitude = 0.6f;

    /// <summary>Fréquence du sinus de test (Hz). Mise à jour en temps réel si le moteur tourne.</summary>
    public float OscillatorFrequency
    {
        get => _oscFrequency;
        set
        {
            _oscFrequency = value;
            if (_sineProvider != null) _sineProvider.Frequency = value;
        }
    }

    /// <summary>Amplitude du sinus de test [0..1]. Mise à jour en temps réel.</summary>
    public float OscillatorAmplitude
    {
        get => _oscAmplitude;
        set
        {
            _oscAmplitude = value;
            if (_sineProvider != null) _sineProvider.Amplitude = value;
        }
    }

    // ── Événements ───────────────────────────────────────────────────────────

    /// <summary>
    /// Déclenché à chaque trame audio. Paramètres : (samples, frameCount, sampleRate).
    /// Sera utilisé par le détecteur de pitch.
    /// </summary>
    public event Action<float[], int, int>? OnAudioFrame;

    /// <summary>Niveaux RMS et Peak mis à jour. Paramètres : (rms, peak).</summary>
    public event Action<float, float>? OnMetersUpdated;

    /// <summary>Message de log émis par le moteur.</summary>
    public event Action<string>?        OnLog;

    /// <summary>Changement d'état du monitoring.</summary>
    public event Action<bool>?          OnMonitoringChanged;

    /// <summary>Surcharge CPU détectée.</summary>
    public event Action<bool>?          OnCpuOverloadChanged;

    // ── État ─────────────────────────────────────────────────────────────────

    public bool  IsMonitoring  { get; private set; }
    public float Level         { get; private set; }
    public float PeakLevel     { get; private set; }
    public bool  CpuOverload   { get; private set; }
    public double SmoothedProcessingMs => _processingEmaMs;

    // ── Objets NAudio — WASAPI ────────────────────────────────────────────────

    private WasapiCapture?        _capture;
    private IWavePlayer?          _playback;
    private BufferedWaveProvider? _buffer;
    private WaveFormat?           _wasapiFormat;
    private SineWaveProvider?     _sineProvider;

    // ── Objets NAudio — ASIO ─────────────────────────────────────────────────

    private string?  _asioDriverName;
    private AsioOut? _asio;
    private int _asioInputOffset, _asioOutputOffset;
    private int _asioInputCount = 1, _asioOutputCount = 2;

    // Ton de test ASIO — accès inter-threads via Interlocked / Volatile → pas besoin de volatile
    private bool _testActive;
    private int  _testRemainingSamples;
    private double _testPhase;
    private float  _testFrequency = 440f;
    private float  _testAmplitude = 0.6f;

    // ── Buffer flottant partagé ───────────────────────────────────────────────

    private float[]  _floatBuffer       = Array.Empty<float>();
    private bool     _floatBufferFromPool;
    private readonly ArrayPool<float> _pool = ArrayPool<float>.Shared;

    // ── Performances ─────────────────────────────────────────────────────────

    private readonly Stopwatch _processingTimer = new();
    private double _processingEmaMs;
    private readonly double _emaAlpha = 0.2;

    private float _peakHold;
    private int   _peakHoldSamples;
    private const int PeakHoldDuration = 48000; // ~1 s à 48 kHz

    // ── Énumération des périphériques ─────────────────────────────────────────

    public List<MMDevice> GetInputDevices()
        => new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

    public List<MMDevice> GetOutputDevices()
        => new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

    public static List<string> GetAsioDrivers()
        => AsioOut.GetDriverNames().ToList();

    public void SetAsioDriver(string driverName)    => _asioDriverName = driverName;

    public void SetAsioRouting(int inputOffset, int outputOffset, int inputCount, int outputCount)
    {
        _asioInputOffset  = Math.Max(0, inputOffset);
        _asioOutputOffset = Math.Max(0, outputOffset);
        _asioInputCount   = Math.Max(1, inputCount);
        _asioOutputCount  = Math.Max(1, outputCount);
    }

    public (int inputOffset, int outputOffset, int inputCount, int outputCount) GetAsioRouting()
        => (_asioInputOffset, _asioOutputOffset, _asioInputCount, _asioOutputCount);

    // ── Cycle de vie ─────────────────────────────────────────────────────────

    public void StartMonitoring() { if (!IsMonitoring) StartAudio(); }
    public void StopMonitoring()  { if  (IsMonitoring) StopAudio();  }

    public void RebuildAudioGraph()
    {
        bool wasRunning = IsMonitoring;
        StopAudio();
        if (wasRunning) StartAudio();
    }

    public void StartAudio()
    {
        try
        {
            if (Driver == AudioDriver.ASIO)
                StartAsio();
            else
                StartWasapi();

            IsMonitoring = true;
            OnMonitoringChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Log("[AudioEngine] Start error: " + ex.Message);
            StopAudio();
        }
    }

    public void StopAudio()
    {
        try { _capture?.StopRecording(); }  catch { }
        try { _playback?.Stop();         }  catch { }
        try { _asio?.Stop();             }  catch { }

        if (_capture != null)
        {
            try { _capture.DataAvailable -= OnWasapiData; } catch { }
        }
        if (_asio != null)
        {
            try { _asio.AudioAvailable -= OnAsioAudioAvailable; } catch { }
        }
        if (_sineProvider != null)
        {
            try { _sineProvider.SamplesGenerated -= OnOscillatorSamples; } catch { }
        }

        try { _capture?.Dispose();  } catch { }
        try { _playback?.Dispose(); } catch { }
        try { _asio?.Dispose();     } catch { }

        _capture      = null;
        _playback     = null;
        _asio         = null;
        _buffer       = null;
        _wasapiFormat = null;
        _sineProvider = null;

        ReturnFloatBuffer();

        IsMonitoring = false;
        OnMonitoringChanged?.Invoke(false);
    }

    // ── WASAPI ───────────────────────────────────────────────────────────────

    private void StartWasapi()
    {
        if (Mode == AudioMode.TestOscillator)
            StartWasapiOscillator();
        else
            StartWasapiLive();
    }

    private void StartWasapiLive()
    {
        var inputDevice  = InputDevice  ?? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Capture,  Role.Multimedia);
        var outputDevice = OutputDevice ?? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render,    Role.Multimedia);
        var shareMode    = Driver == AudioDriver.WASAPI_Exclusive
                           ? AudioClientShareMode.Exclusive
                           : AudioClientShareMode.Shared;

        try
        {
            StartWasapiLiveCore(inputDevice, outputDevice, shareMode);
        }
        catch (Exception ex) when (shareMode == AudioClientShareMode.Exclusive && IsAccessDenied(ex))
        {
            Log("WASAPI Exclusive refusé (E_ACCESSDENIED), fallback vers WASAPI Shared.");
            try { _capture?.Dispose(); } catch { }
            try { _playback?.Dispose(); } catch { }
            _capture = null;
            _playback = null;
            _buffer = null;
            StartWasapiLiveCore(inputDevice, outputDevice, AudioClientShareMode.Shared);
        }
    }

    private void StartWasapiLiveCore(MMDevice inputDevice, MMDevice outputDevice, AudioClientShareMode shareMode)
    {
        _capture      = new WasapiCapture(inputDevice) { ShareMode = shareMode };
        _wasapiFormat = _capture.WaveFormat;
        Log($"WASAPI Live: {_wasapiFormat.Encoding} {_wasapiFormat.SampleRate} Hz {_wasapiFormat.BitsPerSample} bit ×{_wasapiFormat.Channels}");

        if (_wasapiFormat.SampleRate != SampleRate)
        {
            Log($"Adapting SampleRate → {_wasapiFormat.SampleRate} Hz");
            SampleRate = _wasapiFormat.SampleRate;
        }

        _capture.DataAvailable += OnWasapiData;

        _buffer = new BufferedWaveProvider(_wasapiFormat)
        {
            BufferLength          = BufferSize * _wasapiFormat.BlockAlign * 10,
            DiscardOnBufferOverflow = true
        };

        int latencyMs = Math.Max(1, (int)((double)BufferSize / SampleRate * 1000.0));
        var wasapiOut = new WasapiOut(outputDevice, shareMode, false, latencyMs);
        wasapiOut.Init(_buffer);
        _playback = wasapiOut;

        _capture.StartRecording();
        _playback.Play();
    }

    private void StartWasapiOscillator()
    {
        var outputDevice = OutputDevice ?? new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var shareMode    = Driver == AudioDriver.WASAPI_Exclusive
                           ? AudioClientShareMode.Exclusive
                           : AudioClientShareMode.Shared;

        try
        {
            StartWasapiOscillatorCore(outputDevice, shareMode);
        }
        catch (Exception ex) when (shareMode == AudioClientShareMode.Exclusive && IsAccessDenied(ex))
        {
            Log("WASAPI Exclusive refusé (E_ACCESSDENIED), fallback vers WASAPI Shared.");
            try { _playback?.Dispose(); } catch { }
            _playback = null;
            _sineProvider = null;
            StartWasapiOscillatorCore(outputDevice, AudioClientShareMode.Shared);
        }
    }

    private void StartWasapiOscillatorCore(MMDevice outputDevice, AudioClientShareMode shareMode)
    {

        // Aligner sur le format mix du périphérique pour éviter les erreurs WASAPI
        int deviceSampleRate = SampleRate;
        int deviceChannels   = 1;
        try
        {
            var mixFmt       = outputDevice.AudioClient.MixFormat;
            deviceSampleRate = mixFmt.SampleRate;
            deviceChannels   = mixFmt.Channels;
            SampleRate       = deviceSampleRate;
        }
        catch { /* utilise les valeurs par défaut */ }

        _sineProvider = new SineWaveProvider(deviceSampleRate)
        {
            Frequency = _oscFrequency,
            Amplitude = _oscAmplitude
        };
        _sineProvider.SamplesGenerated += OnOscillatorSamples;

        // Convertir mono→stéréo si nécessaire
        ISampleProvider source = _sineProvider;
        if (deviceChannels >= 2 && _sineProvider.WaveFormat.Channels == 1)
            source = new MonoToStereoSampleProvider(_sineProvider);

        int latencyMs = Math.Max(20, (int)((double)BufferSize / SampleRate * 1000.0));
        var wasapiOut = new WasapiOut(outputDevice, shareMode, false, latencyMs);
        wasapiOut.Init(source);
        _playback = wasapiOut;
        _playback.Play();

        Log($"WASAPI Oscillator: {_oscFrequency} Hz @ {deviceSampleRate} Hz ×{deviceChannels}");
    }

    private void OnWasapiData(object? sender, WaveInEventArgs e)
    {
        _processingTimer.Restart();

        var wf = _wasapiFormat ?? _capture?.WaveFormat;
        if (wf == null) return;

        int frames = e.BytesRecorded / wf.BlockAlign;
        EnsureFloatBuffer(frames);

        try
        {
            DecodeToMono(e.Buffer, e.BytesRecorded, wf, _floatBuffer, frames);
            WriteToBuffer(frames, wf);
            UpdateMeters(frames);

            var slice = new float[frames];
            Array.Copy(_floatBuffer, slice, frames);
            OnAudioFrame?.Invoke(slice, frames, wf.SampleRate);
        }
        catch (Exception ex) { Log("[WASAPI] error: " + ex.Message); }
    }

    private void OnOscillatorSamples(float[] samples, int frames, int sampleRate)
    {
        EnsureFloatBuffer(frames);
        Array.Copy(samples, _floatBuffer, frames);
        UpdateMeters(frames);
        OnAudioFrame?.Invoke(samples, frames, sampleRate);
    }

    /// <summary>Décode les octets WASAPI en float mono [-1..1].</summary>
    private static void DecodeToMono(byte[] src, int bytesRecorded, WaveFormat wf, float[] dest, int frames)
    {
        int blockAlign = wf.BlockAlign;
        int channels   = wf.Channels;
        int bits       = wf.BitsPerSample;
        bool isFloat   = wf.Encoding == WaveFormatEncoding.IeeeFloat;

        if (bits == 32 && isFloat)
        {
            for (int f = 0, o = 0; f < frames; f++, o += blockAlign)
            {
                float s = 0f;
                for (int c = 0; c < channels; c++)
                    s += BitConverter.ToSingle(src, o + c * 4);
                dest[f] = s / channels;
            }
        }
        else if (bits == 16 && !isFloat)
        {
            for (int f = 0, o = 0; f < frames; f++, o += blockAlign)
            {
                int s = 0;
                for (int c = 0; c < channels; c++)
                {
                    int ix = o + c * 2;
                    s += (short)(src[ix] | (src[ix + 1] << 8));
                }
                dest[f] = (s / (float)channels) / 32768f;
            }
        }
        else
        {
            Array.Clear(dest, 0, frames);
        }
    }

    /// <summary>Ré-encode le buffer mono float vers le format WASAPI et l'ajoute au BufferedWaveProvider.</summary>
    private void WriteToBuffer(int frames, WaveFormat wf)
    {
        int channels = wf.Channels;
        int bits     = wf.BitsPerSample;
        bool isFloat = wf.Encoding == WaveFormatEncoding.IeeeFloat;
        byte[] outBuf = new byte[frames * wf.BlockAlign];

        if (bits == 32 && isFloat)
        {
            for (int f = 0, o = 0; f < frames; f++)
            {
                var b = BitConverter.GetBytes(Math.Clamp(_floatBuffer[f], -1f, 1f));
                for (int c = 0; c < channels; c++)
                {
                    outBuf[o++] = b[0]; outBuf[o++] = b[1];
                    outBuf[o++] = b[2]; outBuf[o++] = b[3];
                }
            }
        }
        else if (bits == 16 && !isFloat)
        {
            for (int f = 0, o = 0; f < frames; f++)
            {
                short s = (short)(Math.Clamp(_floatBuffer[f], -1f, 1f) * 32767);
                for (int c = 0; c < channels; c++)
                {
                    outBuf[o++] = (byte)(s & 0xFF);
                    outBuf[o++] = (byte)(s >> 8);
                }
            }
        }
        else return;

        _buffer?.AddSamples(outBuf, 0, outBuf.Length);
    }

    // ── ASIO ─────────────────────────────────────────────────────────────────

    public void StartAsioTest(int durationMs = 1000, float frequency = 800f, float amplitude = 0.5f)
    {
        _testFrequency = frequency;
        _testAmplitude = amplitude;
        Interlocked.Exchange(ref _testRemainingSamples, (int)(durationMs / 1000.0 * SampleRate));
        _testPhase = 0.0;
        Volatile.Write(ref _testActive, true);

        if (Driver != AudioDriver.ASIO) Driver = AudioDriver.ASIO;
        if (string.IsNullOrEmpty(_asioDriverName)) { Log("No ASIO driver set."); return; }

        if (!IsMonitoring)
        {
            try { StartAudio(); Log("ASIO test tone started."); }
            catch (Exception ex) { Log("ASIO test failed: " + ex.Message); Volatile.Write(ref _testActive, false); }
        }
    }

    public void StopAsioTest()
    {
        Volatile.Write(ref _testActive, false);
        Interlocked.Exchange(ref _testRemainingSamples, 0);
    }

    public (int inputCount, int outputCount) ProbeAsioChannels(string driverName)
    {
        try
        {
            using var probe = new AsioOut(driverName);
            var inProp  = typeof(AsioOut).GetProperty("DriverInputChannelCount");
            var outProp = typeof(AsioOut).GetProperty("DriverOutputChannelCount");
            if (inProp != null && outProp != null)
            {
                int inC  = (int)(inProp.GetValue(probe)  ?? 0);
                int outC = (int)(outProp.GetValue(probe) ?? 0);
                Log($"ProbeAsio '{driverName}': {inC} in, {outC} out");
                return (inC, outC);
            }
            return (0, 0);
        }
        catch (Exception ex) { Log($"ProbeAsio failed: {ex.Message}"); return (0, 0); }
    }

    private void StartAsio()
    {
        if (string.IsNullOrEmpty(_asioDriverName))
            throw new InvalidOperationException("No ASIO driver selected");

        _asio = new AsioOut(_asioDriverName);
        _asio.AudioAvailable += OnAsioAudioAvailable;

        int driverIn = 0, driverOut = 0;
        var ip = typeof(AsioOut).GetProperty("DriverInputChannelCount");
        var op = typeof(AsioOut).GetProperty("DriverOutputChannelCount");
        if (ip != null) driverIn  = (int)(ip.GetValue(_asio) ?? 0);
        if (op != null) driverOut = (int)(op.GetValue(_asio) ?? 0);
        Log($"ASIO driver: {driverIn} in, {driverOut} out");

        int inOff  = _asioInputOffset,  inCnt  = _asioInputCount;
        int outOff = _asioOutputOffset, outCnt = _asioOutputCount;

        if (driverIn  > 0) { if (inOff  >= driverIn)  inOff  = 0; if (inOff  + inCnt  > driverIn)  inCnt  = driverIn  - inOff;  if (inCnt  < 1) inCnt  = 1; }
        if (driverOut > 0) { if (outOff >= driverOut) outOff = 0; if (outOff + outCnt > driverOut) outCnt = driverOut - outOff; if (outCnt < 1) outCnt = 1; }

        _asio.InputChannelOffset = inOff;
        _asio.ChannelOffset      = outOff;
        Log($"ASIO Init: in[{inOff}+{inCnt}], out[{outOff}+{outCnt}], sr={SampleRate}");

        try
        {
            _asio.InitRecordAndPlayback(new SilenceProvider(SampleRate, outCnt), inCnt, SampleRate);
        }
        catch (Exception ex)
        {
            Log($"ASIO Init failed: {ex.Message} — fallback mono/stereo");
            _asio.Dispose();
            _asio = new AsioOut(_asioDriverName);
            _asio.AudioAvailable += OnAsioAudioAvailable;
            _asio.InputChannelOffset = 0;
            _asio.ChannelOffset      = 0;
            _asio.InitRecordAndPlayback(new SilenceProvider(SampleRate, 2), 1, SampleRate);
        }

        _asio.Play();
        Log($"ASIO running: {_asio.NumberOfInputChannels} in, {_asio.NumberOfOutputChannels} out");
    }

    private static unsafe void ReadAsioInput(IntPtr buf, float[] dest, int n, AsioSampleType t)
    {
        switch (t)
        {
            case AsioSampleType.Int32LSB:
            {
                int* s = (int*)buf;
                for (int i = 0; i < n; i++) dest[i] = s[i] / (float)int.MaxValue;
            } break;
            case AsioSampleType.Int24LSB:
            {
                byte* s = (byte*)buf;
                for (int i = 0; i < n; i++)
                {
                    int v = s[i*3] | (s[i*3+1] << 8) | (s[i*3+2] << 16);
                    if ((v & 0x800000) != 0) v |= unchecked((int)0xFF000000);
                    dest[i] = v / 8388608f;
                }
            } break;
            case AsioSampleType.Int16LSB:
            {
                short* s = (short*)buf;
                for (int i = 0; i < n; i++) dest[i] = s[i] / 32768f;
            } break;
            case AsioSampleType.Float32LSB:
                Marshal.Copy(buf, dest, 0, n);
                break;
            default:
                Array.Clear(dest, 0, n);
                break;
        }
    }

    private static unsafe void WriteAsioOutput(IntPtr buf, float[] src, int n, AsioSampleType t)
    {
        switch (t)
        {
            case AsioSampleType.Int32LSB:
            {
                int* d = (int*)buf;
                for (int i = 0; i < n; i++) d[i] = (int)(Math.Clamp(src[i], -1f, 1f) * int.MaxValue);
            } break;
            case AsioSampleType.Int24LSB:
            {
                byte* d = (byte*)buf;
                for (int i = 0; i < n; i++)
                {
                    int v = (int)(Math.Clamp(src[i], -1f, 1f) * 8388607f);
                    d[i*3]   = (byte)(v & 0xFF);
                    d[i*3+1] = (byte)((v >> 8)  & 0xFF);
                    d[i*3+2] = (byte)((v >> 16) & 0xFF);
                }
            } break;
            case AsioSampleType.Int16LSB:
            {
                short* d = (short*)buf;
                for (int i = 0; i < n; i++) d[i] = (short)(Math.Clamp(src[i], -1f, 1f) * 32767f);
            } break;
            case AsioSampleType.Float32LSB:
                Marshal.Copy(src, 0, buf, n);
                break;
        }
    }

    private unsafe void OnAsioAudioAvailable(object? sender, AsioAudioAvailableEventArgs e)
    {
        try
        {
            _processingTimer.Restart();
            int samples  = e.SamplesPerBuffer;
            var localIn  = e.InputBuffers;
            var localOut = e.OutputBuffers;
            int inCh  = localIn?.Length  ?? 0;
            int outCh = localOut?.Length ?? 0;
            var st    = e.AsioSampleType;
            EnsureFloatBuffer(samples);

            // Mode TestOscillator : génère un sinus et l'écrit dans les sorties ASIO
            if (Mode == AudioMode.TestOscillator)
            {
                double freq = _oscFrequency;
                double amp  = _oscAmplitude;
                double step = 2.0 * Math.PI * freq / SampleRate;
                for (int i = 0; i < samples; i++)
                {
                    _floatBuffer[i] = (float)(amp * Math.Sin(_testPhase));
                    _testPhase     += step;
                    if (_testPhase > 2.0 * Math.PI) _testPhase -= 2.0 * Math.PI;
                }
                if (localOut != null)
                    for (int c = 0; c < outCh; c++)
                        if (localOut[c] != IntPtr.Zero)
                            WriteAsioOutput(localOut[c], _floatBuffer, samples, st);

                UpdateMeters(samples);
                var oscSlice = new float[samples];
                Array.Copy(_floatBuffer, oscSlice, samples);
                OnAudioFrame?.Invoke(oscSlice, samples, SampleRate);
                e.WrittenToOutputBuffers = true;
                return;
            }

            // Ton de test ASIO (StartAsioTest — durée limitée)
            if (Volatile.Read(ref _testActive))
            {
                for (int i = 0; i < samples; i++)
                {
                    _floatBuffer[i] = (float)(_testAmplitude * Math.Sin(_testPhase));
                    _testPhase     += 2 * Math.PI * _testFrequency / SampleRate;
                    if (_testPhase > 2 * Math.PI) _testPhase -= 2 * Math.PI;
                }
                if (localOut != null)
                    for (int c = 0; c < outCh; c++)
                        if (localOut[c] != IntPtr.Zero)
                            WriteAsioOutput(localOut[c], _floatBuffer, samples, st);

                if (Volatile.Read(ref _testRemainingSamples) > 0
                    && Interlocked.Add(ref _testRemainingSamples, -samples) <= 0)
                    Volatile.Write(ref _testActive, false);

                UpdateMeters(samples);
                var testSlice = new float[samples];
                Array.Copy(_floatBuffer, testSlice, samples);
                OnAudioFrame?.Invoke(testSlice, samples, SampleRate);
                e.WrittenToOutputBuffers = true;
                return;
            }

            // Mode normal : capture entrée → sortie (monitoring)
            if (inCh >= 1 && localIn != null && localIn[0] != IntPtr.Zero)
                ReadAsioInput(localIn[0], _floatBuffer, samples, st);
            else
                Array.Clear(_floatBuffer, 0, samples);

            if (localOut != null)
                for (int c = 0; c < outCh; c++)
                    if (localOut[c] != IntPtr.Zero)
                        WriteAsioOutput(localOut[c], _floatBuffer, samples, st);

            UpdateMeters(samples);
            var slice = new float[samples];
            Array.Copy(_floatBuffer, slice, samples);
            OnAudioFrame?.Invoke(slice, samples, SampleRate);
            e.WrittenToOutputBuffers = true;
        }
        catch (Exception ex)
        {
            try { Log("[ASIO] error: " + ex.Message); } catch { }
            if (e != null) e.WrittenToOutputBuffers = false;
        }
    }

    public bool ShowAsioControlPanel()
    {
        if (_asio != null)
        {
            try { _asio.ShowControlPanel(); return true; } catch { }
        }
        else if (!string.IsNullOrEmpty(_asioDriverName))
        {
            try { using var t = new AsioOut(_asioDriverName); t.ShowControlPanel(); return true; } catch { }
        }
        Log("ASIO control panel not available.");
        return false;
    }

    // ── Mesures RMS / Peak ────────────────────────────────────────────────────

    private void UpdateMeters(int samples)
    {
        float sumSq = 0f, peak = 0f;
        for (int i = 0; i < samples; i++)
        {
            float abs = MathF.Abs(_floatBuffer[i]);
            sumSq += _floatBuffer[i] * _floatBuffer[i];
            if (abs > peak) peak = abs;
        }

        Level = MathF.Sqrt(sumSq / samples);

        if (peak > _peakHold)
        {
            _peakHold        = peak;
            _peakHoldSamples = PeakHoldDuration;
        }
        else if (_peakHoldSamples > 0)
        {
            _peakHoldSamples -= samples;
        }
        else
        {
            _peakHold *= 0.995f;
        }
        PeakLevel = _peakHold;

        _processingTimer.Stop();
        double ms = _processingTimer.Elapsed.TotalMilliseconds;
        _processingEmaMs = _processingEmaMs <= 0.0
                           ? ms
                           : _emaAlpha * ms + (1.0 - _emaAlpha) * _processingEmaMs;

        double bufMs = (double)BufferSize / SampleRate * 1000.0;
        bool ov = _processingEmaMs > bufMs;
        if (ov != CpuOverload) { CpuOverload = ov; OnCpuOverloadChanged?.Invoke(ov); }

        OnMetersUpdated?.Invoke(Level, PeakLevel);
    }

    // ── Latence calculée ─────────────────────────────────────────────────────

    public double CalculatedLatencyMs
    {
        get
        {
            if (Driver == AudioDriver.ASIO)
                return (double)BufferSize / SampleRate * 1000.0 + _processingEmaMs;

            double capture = (double)BufferSize / SampleRate * 1000.0;
            double bufferMs = 0;
            if (_buffer != null && _wasapiFormat != null && _wasapiFormat.BlockAlign > 0)
                bufferMs = (double)(_buffer.BufferedBytes / _wasapiFormat.BlockAlign) / _wasapiFormat.SampleRate * 1000.0;
            return capture + bufferMs + _processingEmaMs;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureFloatBuffer(int frames)
    {
        if (_floatBuffer.Length >= frames) return;
        int newSize = Math.Max(frames, Math.Max(BufferSize * 2, _floatBuffer.Length * 2));
        var newBuf = _pool.Rent(newSize);
        if (_floatBufferFromPool) _pool.Return(_floatBuffer, clearArray: true);
        _floatBuffer       = newBuf;
        _floatBufferFromPool = true;
    }

    private void ReturnFloatBuffer()
    {
        if (_floatBufferFromPool)
        {
            try { _pool.Return(_floatBuffer, clearArray: true); } catch { }
        }
        _floatBuffer        = Array.Empty<float>();
        _floatBufferFromPool = false;
    }

    private static bool IsAccessDenied(Exception ex)
        => ex is UnauthorizedAccessException || ex.HResult == unchecked((int)0x80070005);

    private void Log(string message) => OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
}
