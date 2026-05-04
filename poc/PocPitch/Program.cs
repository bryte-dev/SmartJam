// ============================================================
// POC Pitch — SmartJam
// ============================================================
// Objectif : démontrer la détection de hauteur (pitch) offline,
// sans microphone ni carte son.
//
// Deux modes :
//   --source sine  --freq 440      génère un sinus à 440 Hz
//   --source wav   --path <fichier> lit un fichier .wav mono 16 bits
//
// Algorithme : YIN via la bibliothèque NWaves.
// Sortie     : Hz détectés + nom de la note (ex. "A4") dans la console.
// ============================================================

using SmartJam.PocPitch;

// --- Lecture des arguments ---
string source = "sine"; // "sine" ou "wav"
float sineFreq = 440f;  // Hz (utilisé uniquement si source=sine)
string wavPath = "";    // chemin WAV (utilisé uniquement si source=wav)

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--source": source = args[++i]; break;
        case "--freq":   sineFreq = float.Parse(args[++i]); break;
        case "--path":   wavPath  = args[++i]; break;
    }
}

Console.WriteLine("=== SmartJam — POC Pitch Detection ===");
Console.WriteLine($"Source : {source}");

// --- Préparer le signal audio (float[], 44100 Hz) ---
float[] samples;
int sampleRate = 44100;

if (source == "sine")
{
    Console.WriteLine($"Génération d'un sinus à {sineFreq} Hz pendant 3 secondes...");
    samples = SineGenerator.Generate(sineFreq, sampleRate, durationSeconds: 3.0f);
}
else if (source == "wav")
{
    if (string.IsNullOrEmpty(wavPath))
    {
        Console.Error.WriteLine("Erreur : --path requis avec --source wav");
        return 1;
    }
    Console.WriteLine($"Lecture du fichier WAV : {wavPath}");
    (samples, sampleRate) = WavReader.Read(wavPath);
}
else
{
    Console.Error.WriteLine($"Source inconnue : {source}. Utilisez 'sine' ou 'wav'.");
    return 1;
}

Console.WriteLine($"Signal prêt : {samples.Length} échantillons @ {sampleRate} Hz ({samples.Length / (float)sampleRate:F1}s)");
Console.WriteLine();

// --- Détection du pitch par fenêtres glissantes ---
PitchAnalyser.Analyse(samples, sampleRate);

return 0;
