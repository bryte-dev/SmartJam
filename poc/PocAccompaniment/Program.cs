// ============================================================
// POC Accompagnement — SmartJam
// ============================================================
// Objectif : démontrer la génération algorithmique d'une progression
// d'accords et son export au format MIDI (.mid), sans carte son.
//
// Usage :
//   dotnet run -- --tempo 120 --key C --style pop --bars 8
//
// Arguments :
//   --tempo  BPM (battements par minute, défaut : 120)
//   --key    Tonalité : C, D, E, F, G, A, B  (défaut : C)
//   --style  Style    : pop, jazz, blues       (défaut : pop)
//   --bars   Nombre de mesures (défaut : 8)
//   --output Chemin du fichier .mid (défaut : output/progression.mid)
//
// Lib : Melanchall.DryWetMidi (https://github.com/melanchall/drywetmidi)
// ============================================================

using SmartJam.PocAccompaniment;

// --- Lecture des arguments ---
int    tempo  = 120;
string key    = "C";
string style  = "pop";
int    bars   = 8;
string output = Path.Combine("output", "progression.mid");

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--tempo":  tempo  = int.Parse(args[++i]);    break;
        case "--key":    key    = args[++i];               break;
        case "--style":  style  = args[++i];               break;
        case "--bars":   bars   = int.Parse(args[++i]);    break;
        case "--output": output = args[++i];               break;
    }
}

Console.WriteLine("=== SmartJam — POC Accompagnement ===");
Console.WriteLine($"Tonalité : {key}  |  Style : {style}  |  Tempo : {tempo} BPM  |  Mesures : {bars}");
Console.WriteLine();

// --- Générer la progression d'accords ---
var generator = new AccompanimentGenerator();
var progression = generator.BuildProgression(key, style, bars);

Console.WriteLine($"Progression générée ({progression.Count} accords) :");
foreach (var chord in progression)
    Console.WriteLine($"  {chord}");

Console.WriteLine();

// --- Exporter en MIDI ---
Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "output");
MidiExporter.Export(progression, tempo, output);

Console.WriteLine($"Fichier MIDI exporté : {Path.GetFullPath(output)}");
Console.WriteLine();
Console.WriteLine("Pour l'écouter :");
Console.WriteLine("  - Windows : double-clic sur le fichier .mid (Windows Media Player)");
Console.WriteLine("  - VLC     : ouvrir avec VLC");
Console.WriteLine("  - DAW     : importer dans REAPER, Ableton, GarageBand, etc.");
