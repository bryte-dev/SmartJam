namespace SmartJam.Services;

/// <summary>
/// Représente un accord (triade) : nom + notes MIDI.
/// Ex. : C major = {60, 64, 67}
/// </summary>
public record Chord(string Name, int[] MidiNotes)
{
    public override string ToString() => Name;
}

/// <summary>
/// Service de génération de progression d'accords.
///
/// Algorithme "règles musicales" (pas d'IA) :
///   1. On calcule les notes de la gamme majeure pour la tonalité donnée.
///   2. On construit des triades sur les degrés de la gamme.
///   3. On applique un pattern de progression selon le style.
///
/// Exemple (Do majeur, style pop) : C – G – Am – F (I – V – vi – IV)
/// </summary>
public class AccompanimentGeneratorService
{
    // Intervalles de la gamme majeure (demi-tons depuis la tonique)
    private static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };

    // Qualité maj/min pour chaque degré de la gamme majeure
    private static readonly bool[] IsMajor = { true, false, false, true, true, false, false };

    // Numéro MIDI de la tonique par tonalité (C4 = 60)
    private static readonly Dictionary<string, int> KeyMidi = new()
    {
        {"C",60}, {"C#",61}, {"Db",61}, {"D",62}, {"D#",63}, {"Eb",63},
        {"E",64}, {"F",65}, {"F#",66}, {"Gb",66}, {"G",67}, {"G#",68},
        {"Ab",68}, {"A",69}, {"A#",70}, {"Bb",70}, {"B",71}
    };

    // Patterns de progression (degrés 0-indexed : I=0, II=1, ...)
    private static readonly Dictionary<string, int[]> Patterns = new()
    {
        {"pop",   new[]{0, 4, 5, 3}},  // I – V – vi – IV
        {"jazz",  new[]{1, 4, 0, 0}},  // ii – V – I – I
        {"blues", new[]{0, 3, 0, 4}},  // I – IV – I – V
    };

    private static readonly string[] DegreeNames = {"I","II","III","IV","V","VI","VII"};
    private static readonly string[] NoteNames   = {"C","C#","D","D#","E","F","F#","G","G#","A","A#","B"};

    /// <summary>
    /// Génère une progression pour le nombre de mesures demandé.
    /// </summary>
    public List<Chord> Generate(string key = "C", string style = "pop", int bars = 8)
    {
        int root = KeyMidi.GetValueOrDefault(key, 60);
        int[] pattern = Patterns.GetValueOrDefault(style.ToLower(), Patterns["pop"]);

        var result = new List<Chord>();
        for (int i = 0; i < bars; i++)
        {
            int degree    = pattern[i % pattern.Length];
            int interval  = MajorScale[degree % 7];
            bool major    = IsMajor[degree % 7];
            int chordRoot = root + interval;
            int third     = major ? chordRoot + 4 : chordRoot + 3;
            int fifth     = chordRoot + 7;

            string rootName = NoteNames[chordRoot % 12];
            string quality  = major ? "maj" : "min";
            string name     = $"{DegreeNames[degree % 7]} — {rootName} {quality}";

            result.Add(new Chord(name, new[]{ chordRoot, third, fifth }));
        }
        return result;
    }
}
