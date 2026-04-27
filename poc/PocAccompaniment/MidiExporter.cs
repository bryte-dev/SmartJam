using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace SmartJam.PocAccompaniment;

/// <summary>
/// Exporte une progression d'accords vers un fichier MIDI standard (.mid).
///
/// Format MIDI utilisé :
///   - MIDI Type 0 (tout dans une seule piste)
///   - Canal 0 (piano)
///   - Tempo variable (SetTempoEvent)
///   - Notes : NoteOnEvent + NoteOffEvent pour chaque note de chaque accord
///
/// Structure d'une mesure en 4/4 :
///   PPQN = 480 ticks par noire
///   Mesure 4/4 = 4 * 480 = 1920 ticks
///   Accord plaqué sur toute la mesure (avec 10 ticks de marge)
///
/// DeltaTime : dans MIDI, chaque événement a un "delta time" = temps écoulé
/// depuis l'événement précédent (en ticks). C'est différent du temps absolu.
///
/// Lib : Melanchall.DryWetMidi (https://github.com/melanchall/drywetmidi)
/// </summary>
public static class MidiExporter
{
    // Résolution MIDI : 480 ticks par noire (valeur standard)
    private const int TicksPerQuarterNote = 480;

    // En 4/4, une mesure = 4 noires = 4 * 480 = 1920 ticks
    private const int TicksPerBar = 4 * TicksPerQuarterNote;

    // Durée des notes : légèrement moins qu'une mesure (10 ticks de "respiration")
    private const int NoteDuration = TicksPerBar - 10;

    // Vélocité des notes (0..127) — 80 = dynamique modérée
    private const byte NoteVelocity = 80;

    // Canal MIDI (0-indexed dans DryWetMIDI) — canal 0 = piano
    private static readonly FourBitNumber Channel0 = (FourBitNumber)0;

    /// <summary>
    /// Génère et écrit le fichier MIDI à partir de la progression d'accords.
    /// </summary>
    /// <param name="progression">Liste des accords à jouer (1 accord = 1 mesure)</param>
    /// <param name="bpm">Tempo en battements par minute</param>
    /// <param name="outputPath">Chemin du fichier .mid de sortie</param>
    public static void Export(List<Chord> progression, int bpm, string outputPath)
    {
        // Tempo en microsecondes par noire (formule MIDI standard)
        int microsecondsPerBeat = 60_000_000 / bpm;

        var events = new List<MidiEvent>();

        // --- Événements d'entête ---
        // SetTempo : définit la vitesse de lecture
        events.Add(new SetTempoEvent(microsecondsPerBeat) { DeltaTime = 0 });

        // TimeSignature 4/4 : numérateur=4, dénominateur=2 (valeur de 2^n → 2^2=4)
        events.Add(new TimeSignatureEvent(4, 2) { DeltaTime = 0 });

        // --- Événements de notes pour chaque accord ---
        // On alterne : NoteOn(chord) → NoteOff(chord) → NoteOn(next) → ...
        //
        // Chronologie (en deltas) :
        //   [0]  NoteOn note1   delta=0
        //   [0]  NoteOn note2   delta=0   (simultané)
        //   [0]  NoteOn note3   delta=0   (simultané)
        //   [NoteDuration] NoteOff note1  delta=NoteDuration
        //   [0]  NoteOff note2  delta=0   (simultané)
        //   [0]  NoteOff note3  delta=0   (simultané)
        //   [10] NoteOn note1'  delta=10  (nouvel accord, après 10 ticks de silence)
        //   ...

        bool firstChord = true;
        foreach (var chord in progression)
        {
            // NoteOn pour toutes les notes de l'accord
            bool firstNote = true;
            foreach (int midiNote in chord.MidiNotes)
            {
                long deltaOn = 0;
                if (firstNote && !firstChord)
                {
                    // L'écart entre le dernier NoteOff et ce NoteOn = 10 ticks
                    deltaOn = TicksPerBar - NoteDuration;
                }
                events.Add(new NoteOnEvent(
                    (SevenBitNumber)Math.Clamp(midiNote, 0, 127),
                    (SevenBitNumber)NoteVelocity
                ) { Channel = Channel0, DeltaTime = deltaOn });
                firstNote = false;
            }
            firstChord = false;

            // NoteOff pour toutes les notes (après NoteDuration ticks)
            bool firstOff = true;
            foreach (int midiNote in chord.MidiNotes)
            {
                long deltaOff = firstOff ? NoteDuration : 0;
                events.Add(new NoteOffEvent(
                    (SevenBitNumber)Math.Clamp(midiNote, 0, 127),
                    (SevenBitNumber)0
                ) { Channel = Channel0, DeltaTime = deltaOff });
                firstOff = false;
            }
        }

        // Construire la piste et le fichier MIDI
        // DryWetMIDI ajoute automatiquement EndOfTrack lors de l'écriture
        var trackChunk = new TrackChunk(events.ToArray());
        var midiFile   = new MidiFile(trackChunk)
        {
            TimeDivision = new TicksPerQuarterNoteTimeDivision(TicksPerQuarterNote)
        };

        midiFile.Write(outputPath, overwriteFile: true);
    }
}
