namespace SmartJam.PocAccompaniment;

/// <summary>
/// Représente un accord (une combinaison de notes musicales jouées ensemble).
/// Ex. : accord de Do majeur = {C, E, G}
/// </summary>
public class Chord
{
    /// <summary>Nom de l'accord (ex. "C maj", "G maj", "A min").</summary>
    public string Name { get; }

    /// <summary>
    /// Notes MIDI de l'accord (valeurs 0..127).
    /// Convention MIDI : Do du milieu (C4) = 60.
    /// </summary>
    public int[] MidiNotes { get; }

    public Chord(string name, int[] midiNotes)
    {
        Name      = name;
        MidiNotes = midiNotes;
    }

    public override string ToString() => $"{Name} [{string.Join(", ", MidiNotes)}]";
}

/// <summary>
/// Génère une progression d'accords algorithmiquement.
///
/// Principe :
/// - Chaque tonalité (C, D, E...) a une gamme majeure définie par des intervalles.
/// - On construit des triades (accords à 3 notes) sur les degrés de la gamme.
/// - On applique un pattern de progression selon le style musical.
///
/// Gamme majeure : intervalles = [0, 2, 4, 5, 7, 9, 11] (en demi-tons depuis la tonique)
/// Triades :
///   Majeur = root + 4 demi-tons + 7 demi-tons
///   Mineur = root + 3 demi-tons + 7 demi-tons
///
/// Progressions préfinies :
///   pop   : I – V – vi – IV   (ex. C – G – Am – F)
///   jazz  : ii – V – I – I    (ex. Dm – G – C – C)
///   blues : I – IV – I – V    (ex. C – F – C – G)
/// </summary>
public class AccompanimentGenerator
{
    // Intervalles de la gamme majeure (en demi-tons depuis la tonique)
    private static readonly int[] MajorScaleIntervals = { 0, 2, 4, 5, 7, 9, 11 };

    // Qualité de chaque degré de la gamme majeure (maj/min)
    // I=maj, ii=min, iii=min, IV=maj, V=maj, vi=min, vii°=dim (simplifié en min)
    private static readonly bool[] DegreeIsMajor = { true, false, false, true, true, false, false };

    // Numéros MIDI de la tonique de chaque gamme (octave 4)
    // C4=60, C#4=61, D4=62, D#4=63, E4=64, F4=65, F#4=66, G4=67, G#4=68, A4=69, A#4=70, B4=71
    private static readonly Dictionary<string, int> KeyToMidi = new()
    {
        {"C",  60}, {"C#", 61}, {"Db", 61},
        {"D",  62}, {"D#", 63}, {"Eb", 63},
        {"E",  64},
        {"F",  65}, {"F#", 66}, {"Gb", 66},
        {"G",  67}, {"G#", 68}, {"Ab", 68},
        {"A",  69}, {"A#", 70}, {"Bb", 70},
        {"B",  71},
    };

    // Patterns de progression : liste de degrés (0-indexed, donc I=0, II=1, etc.)
    private static readonly Dictionary<string, int[]> StylePatterns = new()
    {
        { "pop",   new[] {0, 4, 5, 3} },   // I – V – vi – IV
        { "jazz",  new[] {1, 4, 0, 0} },   // ii – V – I – I
        { "blues", new[] {0, 3, 0, 4} },   // I – IV – I – V
    };

    // Noms des degrés pour l'affichage
    private static readonly string[] DegreeNames = { "I", "II", "III", "IV", "V", "VI", "VII" };

    /// <summary>
    /// Construit une progression d'accords pour le nombre de mesures demandé.
    /// Le pattern est répété si le nombre de mesures est supérieur au pattern.
    /// </summary>
    /// <param name="key">Tonalité (ex. "C", "G", "Am" → on garde la tonique)</param>
    /// <param name="style">Style musical : "pop", "jazz" ou "blues"</param>
    /// <param name="bars">Nombre de mesures (1 mesure = 1 accord ici)</param>
    public List<Chord> BuildProgression(string key, string style, int bars)
    {
        // Normaliser la tonalité
        if (!KeyToMidi.TryGetValue(key, out int rootMidi))
        {
            Console.Error.WriteLine($"Tonalité inconnue '{key}', utilisation de C.");
            rootMidi = 60;
            key = "C";
        }

        // Récupérer le pattern de progression
        if (!StylePatterns.TryGetValue(style.ToLower(), out int[]? pattern) || pattern is null)
        {
            Console.Error.WriteLine($"Style inconnu '{style}', utilisation de pop.");
            pattern = StylePatterns["pop"];
        }

        // Construire les accords du pattern
        var result = new List<Chord>();
        for (int i = 0; i < bars; i++)
        {
            int degree = pattern[i % pattern.Length];
            result.Add(BuildChord(key, rootMidi, degree));
        }

        return result;
    }

    /// <summary>
    /// Construit un accord (triade) sur un degré donné de la gamme.
    /// </summary>
    private static Chord BuildChord(string keyName, int rootMidi, int degree)
    {
        // Intervalle depuis la tonique pour ce degré
        int interval = MajorScaleIntervals[degree % 7];
        bool isMajor = DegreeIsMajor[degree % 7];

        // Note fondamentale de l'accord (en MIDI)
        int chordRoot = rootMidi + interval;

        // Triade : fondamentale + tierce + quinte
        int third  = isMajor ? chordRoot + 4 : chordRoot + 3; // tierce maj ou min
        int fifth  = chordRoot + 7;                            // quinte juste (toujours)

        // Noms des notes pour l'affichage
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        string rootName = noteNames[chordRoot % 12];
        string quality  = isMajor ? "maj" : "min";
        string degreeName = DegreeNames[degree % 7];

        return new Chord(
            name:      $"{degreeName} — {rootName} {quality}",
            midiNotes: new[] { chordRoot, third, fifth }
        );
    }
}
