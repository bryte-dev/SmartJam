namespace SmartJam.PocPitch;

/// <summary>
/// Lecture simple d'un fichier WAV (PCM 16 bits, mono ou stéréo).
/// Convertit en float[-1..1] et retourne le premier canal (mono).
/// </summary>
public static class WavReader
{
    /// <summary>
    /// Lit un fichier WAV et retourne (samples float[], sampleRate int).
    /// Gère PCM 16 bits (le format le plus courant).
    /// </summary>
    public static (float[] samples, int sampleRate) Read(string path)
    {
        using var fs = File.OpenRead(path);
        using var reader = new BinaryReader(fs);

        // Vérification entête RIFF
        string riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new InvalidDataException("Fichier WAV invalide : header RIFF manquant.");

        reader.ReadInt32(); // taille totale (ignorée)

        string wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
            throw new InvalidDataException("Fichier WAV invalide : marker WAVE manquant.");

        // Lire les chunks jusqu'à trouver fmt + data
        int sampleRate = 44100;
        int channels = 1;
        int bitsPerSample = 16;

        while (fs.Position < fs.Length)
        {
            string chunkId = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();

            if (chunkId == "fmt ")
            {
                reader.ReadInt16(); // audioFormat (1 = PCM)
                channels      = reader.ReadInt16();
                sampleRate    = reader.ReadInt32();
                reader.ReadInt32(); // byteRate
                reader.ReadInt16(); // blockAlign
                bitsPerSample = reader.ReadInt16();

                // Sauter les octets restants du chunk fmt si > 16
                int extraBytes = chunkSize - 16;
                if (extraBytes > 0) reader.ReadBytes(extraBytes);
            }
            else if (chunkId == "data")
            {
                if (bitsPerSample != 16)
                    throw new NotSupportedException($"Seul PCM 16 bits est supporté (trouvé : {bitsPerSample} bits).");

                int totalBytes   = chunkSize;
                int bytesPerSample = bitsPerSample / 8;
                int totalFrames  = totalBytes / (bytesPerSample * channels);

                float[] samples = new float[totalFrames];
                for (int i = 0; i < totalFrames; i++)
                {
                    // Lire le premier canal, ignorer les canaux supplémentaires
                    short s = reader.ReadInt16();
                    samples[i] = s / 32768f; // normaliser en [-1, 1]

                    for (int c = 1; c < channels; c++)
                        reader.ReadInt16(); // ignorer les autres canaux
                }

                return (samples, sampleRate);
            }
            else
            {
                // Chunk inconnu → sauter
                reader.ReadBytes(chunkSize);
            }
        }

        throw new InvalidDataException("Fichier WAV invalide : chunk 'data' introuvable.");
    }
}
