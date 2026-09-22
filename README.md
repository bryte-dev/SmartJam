# SmartJam 🎵

SmartJam is my **TPI project**: a desktop application designed to analyze musical notes in real time and assist musicians by suggesting musical information and accompaniment ideas.

The application captures an audio signal, estimates its fundamental frequency, converts that frequency into a musical note, and displays the result in a graphical interface.

SmartJam can operate either with:

- a built-in test oscillator;
- a real microphone or audio interface;
- supported WASAPI or ASIO audio drivers.

> **Project status: functional TPI prototype**
>
> The main real-time pitch detection workflow is implemented and testable. The project includes an audio engine, a graphical interface, pitch detection using the YIN algorithm, note conversion, signal meters, note history, audio device configuration, probable key and chord information, and a rule-based accompaniment generator.
>
> However, SmartJam remains a prototype. The accompaniment generation and some advanced musical features are not yet integrated into a complete automatic accompaniment playback workflow.

---

## TPI Context

SmartJam was developed as my **TPI project**.

The purpose of the TPI was to design and implement an application capable of:

1. receiving or generating an audio signal;
2. analyzing that signal in real time;
3. identifying the dominant pitch;
4. converting the frequency into a musical note;
5. displaying useful information to the user;
6. exploring the possibility of generating musical accompaniment from the detected information.

This project combines several technical areas:

- desktop application development;
- audio capture;
- digital signal processing;
- pitch detection;
- musical note conversion;
- audio device management;
- graphical user interface development;
- music theory;
- software architecture;
- debugging and real-time event handling.

The application was designed for musicians, students, and anyone interested in visualizing the notes being played by an instrument or audio source.

---

## Project Goal

The main goal of SmartJam is to create a tool that can listen to an audio signal and help the user understand its musical content.

The core workflow is:

```text
Audio signal
    ↓
Audio engine
    ↓
Audio buffer
    ↓
Pitch detection
    ↓
Frequency estimation
    ↓
Musical note conversion
    ↓
Graphical display
```

For example:

```text
440 Hz → A4
110 Hz → A2
```

The application can also use the detected notes as a starting point for estimating:

- a possible musical key;
- a possible chord;
- a simple chord progression;
- a potential musical accompaniment.

---

## Main Features

### Real-Time Pitch Detection

SmartJam analyzes incoming audio buffers and estimates the fundamental frequency.

The pitch detection service is implemented in:

```text
Services/PitchDetectorService.cs
```

It uses the YIN algorithm through NWaves.

The detector:

- receives normalized audio samples;
- ignores buffers that are too short;
- searches for frequencies between 35 Hz and 1200 Hz;
- detects the fundamental frequency;
- converts the frequency to a note name;
- returns an empty result when no reliable pitch is found.

The result contains:

```text
Frequency in Hz
Musical note name
```

### Frequency-to-Note Conversion

The detected frequency is converted to a musical note using MIDI note calculations.

The conversion is based on the standard tuning reference:

```text
A4 = 440 Hz
```

Examples:

```text
440 Hz → A4
261.63 Hz → C4
110 Hz → A2
```

The application displays the detected note and frequency in the main window.

### Test Oscillator

SmartJam includes an internal sine-wave oscillator.

This makes it possible to test the pitch detection system without connecting a microphone or an instrument.

The test oscillator allows the user to:

- choose a frequency;
- change the frequency while monitoring is running;
- change the amplitude;
- observe the detected note;
- verify whether the pitch detection is working correctly.

The oscillator is implemented through:

```text
Audio/SineWaveProvider.cs
Services/SineAudioSource.cs
```

This mode is useful for controlled testing.

For example:

```text
Oscillator frequency: 440 Hz
Expected result: A4
```

### Live Audio Input

The application can also use a real audio input.

The live mode can receive audio from:

- a microphone;
- an instrument pickup;
- an external audio interface;
- another compatible input device.

The live mode depends on:

- the selected input device;
- the selected audio driver;
- the installed drivers;
- the quality of the incoming signal;
- the stability of the audio buffer;
- background noise;
- whether the played note is sufficiently clear.

### Audio Drivers

SmartJam supports several audio driver modes:

```text
WASAPI Shared
WASAPI Exclusive
ASIO
```

The audio engine is implemented in:

```text
Audio/AudioEngine.cs
```

The settings window allows the user to configure:

- audio driver;
- input device;
- output device;
- sample rate;
- buffer size;
- ASIO driver;
- ASIO input channel;
- ASIO output channel.

### Audio Monitoring

The user can start or stop audio monitoring.

During monitoring, the application processes audio frames and updates:

- the detected frequency;
- the detected note;
- the input level;
- the peak level;
- the note history;
- diagnostic logs.

### Input and Peak Meters

The main window displays signal levels using RMS and peak values.

These meters help the user understand whether:

- the input signal is too weak;
- the signal is strong enough;
- the input is clipping;
- the audio device is working correctly.

The interface displays both visual meters and decibel values.

### Played Notes History

SmartJam keeps a short history of recently detected notes.

The history can be reset using the interface.

The project limits the number of stored played notes in order to avoid keeping an unlimited amount of data in memory.

### Possible Key and Chord Information

The main window includes properties for:

```text
PossibleKey
PossibleChord
```

The application is designed to provide musical context in addition to a single detected note.

This feature is related to the analysis of:

- recent notes;
- note sequences;
- major scales;
- minor scales;
- possible tonal centers;
- possible chord structures.

The implementation is part of the current prototype and should be considered an evolving feature rather than a complete music analysis engine.

### Accompaniment Generation

SmartJam includes a rule-based accompaniment generator in:

```text
Services/AccompanimentGeneratorService.cs
```

The generator creates chord progressions according to:

- a musical key;
- a style;
- a number of bars.

Supported progression styles include:

```text
pop
jazz
blues
```

Examples of supported patterns include:

```text
Pop:
I – V – vi – IV

Jazz:
ii – V – I – I

Blues:
I – IV – I – V
```

The generator creates triads and returns chord names with MIDI note values.

This is a music-theory-based system and does not use artificial intelligence or machine learning.

### Settings Window

The settings window is implemented with:

```text
Views/SettingsWindow.axaml
Views/SettingsWindow.axaml.cs
ViewModels/SettingsViewModel.cs
```

It allows the user to:

- refresh available audio devices;
- select a WASAPI input device;
- select a WASAPI output device;
- select an ASIO driver;
- configure ASIO channels;
- choose a sample rate;
- choose a buffer size;
- apply new settings;
- open an ASIO control panel when supported.

---

## Technology Stack

### Language

- **C#**

### Runtime

- **.NET 10**
- **Windows desktop application**

### User Interface

- **Avalonia UI**
- **XAML**
- **CommunityToolkit.Mvvm**

### Audio Processing

- **NAudio**
- **NWaves**

### Audio Drivers

- WASAPI Shared;
- WASAPI Exclusive;
- ASIO.

---

## Project Structure

```text
SmartJam/
├── src/
│   └── SmartJam/
│       ├── App.axaml                  Avalonia application definition
│       ├── App.axaml.cs               Application initialization
│       ├── Program.cs                 Desktop application entry point
│       ├── SmartJam.csproj            .NET project configuration
│       ├── app.manifest               Windows application manifest
│       │
│       ├── Audio/
│       │   ├── AudioEngine.cs          Audio capture and playback engine
│       │   └── SineWaveProvider.cs     Internal test oscillator
│       │
│       ├── Services/
│       │   ├── PitchDetectorService.cs Pitch detection using YIN
│       │   ├── SineAudioSource.cs      Offline sine-wave generation
│       │   └── AccompanimentGeneratorService.cs
│       │                                Rule-based chord progression generation
│       │
│       ├── ViewModels/
│       │   └── SettingsViewModel.cs    Audio settings and device management
│       │
│       ├── Views/
│       │   ├── MainWindow.axaml        Main graphical interface
│       │   ├── MainWindow.axaml.cs     Main window logic and bindings
│       │   ├── SettingsWindow.axaml    Audio settings interface
│       │   └── SettingsWindow.axaml.cs Settings window logic
│       │
│       └── Assets/                     Application resources
│
├── SmartJam.slnx                       .NET solution file
├── README.md
└── .gitignore
```

---

## Architecture

SmartJam follows a desktop MVVM-oriented architecture.

The general flow is:

```text
Program.cs
    ↓
Avalonia application
    ↓
MainWindow
    ↓
AudioEngine
    ↓
Audio frames
    ↓
PitchDetectorService
    ↓
MainWindow data bindings
    ↓
Detected note and frequency displayed to the user
```

### Application Entry Point

The application starts in:

```text
Program.cs
```

Avalonia is configured with:

- platform detection;
- desktop lifetime support;
- Inter font;
- debug developer tools in debug builds;
- trace logging.

### Application Initialization

The main application is initialized in:

```text
App.axaml.cs
```

The application creates:

```text
MainWindow
```

as its main desktop window.

### Main Window

The main user interface is implemented in:

```text
Views/MainWindow.axaml
Views/MainWindow.axaml.cs
```

The main window manages:

- audio monitoring;
- mode selection;
- oscillator controls;
- signal meters;
- detected note display;
- detected frequency display;
- played notes;
- possible key;
- possible chord;
- logs;
- settings access.

### Audio Engine

The `AudioEngine` class is responsible for:

- selecting the audio mode;
- managing audio drivers;
- configuring input and output devices;
- starting and stopping monitoring;
- receiving audio samples;
- generating test oscillator samples;
- exposing audio frames;
- calculating RMS and peak levels;
- managing WASAPI and ASIO routing.

The engine supports two main modes:

```text
Live
TestOscillator
```

### Pitch Detection Service

The `PitchDetectorService` receives an audio buffer and applies YIN pitch detection.

The current frequency range is:

```text
35 Hz to 1200 Hz
```

Audio buffers shorter than 512 samples are ignored.

The detection process is:

```text
Audio samples
    ↓
YIN pitch estimation
    ↓
Frequency validation
    ↓
MIDI conversion
    ↓
Note name
```

### Accompaniment Generator

The `AccompanimentGeneratorService` is based on musical rules.

It:

1. identifies the tonic;
2. builds a major scale;
3. determines the chord quality of each degree;
4. selects a progression pattern;
5. generates triads;
6. returns chord names and MIDI notes.

The generator is deterministic and rule-based.

It is not an AI model.

---

## Supported Audio Modes

### Test Oscillator Mode

The internal oscillator generates a sine wave in memory.

Advantages:

- no external hardware required;
- predictable input;
- easy pitch detection testing;
- useful for demonstrations;
- useful for regression testing.

Example test:

```text
Frequency: 440 Hz
Expected note: A4
```

### Live Mode

Live mode captures audio from the selected input device.

It can be used with:

- a microphone;
- a guitar;
- a bass;
- a keyboard;
- an audio interface;
- another monophonic instrument.

For the best results, the user should play one clear note at a time.

---

## What Currently Works

Based on the current implementation, the following features are present and functional in the prototype.

### Application Startup

- Avalonia desktop application startup;
- main window creation;
- Windows desktop manifest;
- .NET project configuration;
- application styling;
- main UI rendering.

### Test Oscillator

- internal sine-wave generation;
- adjustable oscillator frequency;
- adjustable oscillator amplitude;
- real-time oscillator updates;
- pitch analysis without external hardware.

### Pitch Detection

- audio buffer processing;
- YIN-based pitch estimation;
- frequency range filtering;
- conversion from frequency to note name;
- handling of silence or invalid pitch;
- detection of notes such as A4 and A2.

### Monitoring

- start and stop monitoring;
- audio frame events;
- real-time UI updates;
- RMS level calculation;
- peak level calculation;
- audio logs;
- monitoring state display.

### Audio Configuration

- WASAPI Shared mode;
- WASAPI Exclusive mode;
- ASIO mode;
- input device listing;
- output device listing;
- ASIO driver listing;
- ASIO channel configuration;
- sample-rate configuration;
- buffer-size configuration;
- settings application;
- ASIO control panel access when supported.

### User Interface

- frequency display;
- detected note display;
- signal meters;
- peak meters;
- oscillator controls;
- played notes history;
- reset played notes action;
- settings window;
- status and diagnostic messages;
- light user interface theme;
- resizable desktop window.

### Musical Analysis

- note-to-MIDI conversion;
- possible key properties;
- possible chord properties;
- major and minor scale interval definitions;
- stable detection filtering;
- note history collection.

### Accompaniment Prototype

- key-based chord generation;
- major-scale triads;
- pop chord progression;
- jazz chord progression;
- blues chord progression;
- configurable number of bars;
- MIDI note output for generated chords.

---

## What Does Not Fully Work or Is Not Fully Implemented

SmartJam is functional as a real-time note detection prototype, but several areas remain incomplete or limited.

### 1. Full Automatic Accompaniment Playback Is Not Implemented

The project contains an accompaniment generation service, but it does not yet provide a complete end-to-end accompaniment player.

The current generator can produce:

```text
Chord name
MIDI note values
```

However, a full accompaniment workflow would also require:

- playback of generated chords;
- timing and tempo management;
- rhythm patterns;
- instrument sounds;
- MIDI output or synthesized audio;
- synchronization with the detected notes;
- start, pause, and stop controls;
- accompaniment volume control.

The current project should therefore be described as having an **accompaniment generation prototype**, not a complete automatic accompaniment system.

### 2. Key and Chord Detection Is Still Limited

The application contains possible key and chord concepts, but accurate musical analysis is difficult.

The current system is primarily based on detected notes and predefined musical intervals.

It does not represent a complete harmonic analysis engine capable of reliably handling:

- complex chords;
- polyphonic audio;
- inversions;
- modulations;
- chromatic melodies;
- ambiguous tonal centers;
- noisy recordings;
- multiple simultaneous instruments.

### 3. Polyphonic Detection Is Not the Main Target

The pitch detector is designed primarily for a dominant or fundamental pitch.

It works best with:

- one note at a time;
- monophonic instruments;
- stable signals;
- clean input;
- limited background noise.

It is not designed to fully separate several simultaneous notes from a complete chord or band recording.

### 4. Real Audio Input Depends on the Environment

Live mode depends on the computer and its audio configuration.

Potential problems may come from:

- missing audio drivers;
- unavailable ASIO drivers;
- unsupported devices;
- incorrect input selection;
- incorrect ASIO channel routing;
- insufficient input level;
- excessive noise;
- audio permissions;
- incompatible sample rates;
- buffer size problems;
- driver conflicts.

The internal oscillator is therefore the most reliable way to demonstrate the pitch detection workflow.

### 5. ASIO Is Platform and Driver Dependent

ASIO support depends on:

- Windows;
- the installed ASIO driver;
- the audio interface;
- the number of available input and output channels;
- the driver's control panel;
- correct channel offsets.

The project handles unsupported ASIO environments, but not every ASIO device can be guaranteed to work identically.

### 6. No Complete Automated Test Suite Is Visible

The repository contains test-oriented code such as:

```text
SineAudioSource
```

and the internal oscillator, which are useful for manual verification.

However, there is no clearly visible comprehensive automated test suite covering:

- pitch detection accuracy;
- multiple frequencies;
- noisy signals;
- silence;
- audio engine lifecycle;
- ASIO routing;
- accompaniment generation;
- note history;
- key detection;
- UI behavior.

### 7. Hardware Testing Is Required for Live Mode

The test oscillator can validate the software pipeline, but it cannot fully validate:

- microphone behavior;
- audio interface latency;
- real instrument signals;
- driver compatibility;
- input gain;
- background noise;
- physical audio routing.

Live mode must therefore be tested on the target computer and with the intended audio hardware.

### 8. Configuration Persistence Is Limited

The settings window allows the user to apply audio settings, but persistent storage of user preferences is not clearly implemented.

Settings may not automatically survive application restarts unless additional persistence is added.

### 9. The Interface Is Focused on Demonstration and Analysis

The interface presents the core information clearly, but it is still a technical prototype.

Possible future improvements include:

- richer visual feedback;
- a tuner-style pitch indicator;
- cents deviation;
- note confidence;
- chord confidence;
- audio waveform visualization;
- spectrogram visualization;
- clearer setup instructions;
- user profiles;
- project saving;
- export functionality.

### 10. Windows Is the Main Target

The project uses:

```text
net10.0
Avalonia
NAudio
WASAPI
ASIO
```

Although Avalonia itself is cross-platform, the current audio implementation and application manifest are primarily oriented toward Windows.

WASAPI and ASIO features are Windows-specific.

---

## Requirements

To use SmartJam, the recommended environment is:

- Windows 10 or Windows 11;
- .NET 10 SDK;
- a compatible microphone or audio interface for live mode;
- appropriate audio drivers;
- an ASIO driver if ASIO mode is required.

To check the installed .NET version:

```bash
dotnet --version
```

---

## Installation

Clone the repository:

```bash
git clone https://github.com/bryte-dev/SmartJam.git
cd SmartJam
```

Restore the project dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

---

## Running the Application

From the repository root:

```bash
cd src/SmartJam
dotnet run
```

The application should open in a desktop window.

---

## Quick Start: Test Oscillator

The internal oscillator is the easiest way to test SmartJam.

1. Start the application.
2. Select `TestOscillator`.
3. Set the oscillator frequency to `440 Hz`.
4. Set the amplitude to a suitable value.
5. Click the monitoring button.
6. Check the detected frequency and note.
7. Confirm that the result is approximately:

```text
440 Hz → A4
```

Another useful test is:

```text
110 Hz → A2
```

The test oscillator is recommended before testing a real microphone or instrument.

---

## Quick Start: Live Audio Input

To test a real audio source:

1. Start the application.
2. Open the audio settings.
3. Select `Live` mode.
4. Select an input device.
5. Choose an audio driver.
6. Select a sample rate.
7. Select an appropriate buffer size.
8. Apply the settings.
9. Start monitoring.
10. Play one stable note.
11. Observe the detected frequency and note.

For best results:

- play one note at a time;
- avoid background noise;
- avoid audio clipping;
- use a clear and stable signal;
- start with a moderate input level;
- test the oscillator first if the live input does not work.

---

## Audio Settings

The available sample rates are:

```text
44100 Hz
48000 Hz
96000 Hz
```

The available buffer sizes are:

```text
64
128
256
512
1024
```

Smaller buffer sizes can reduce latency but may increase CPU usage or cause instability.

Larger buffer sizes are generally more stable but may increase latency.

---

## Development Commands

From the project root:

```bash
dotnet restore
```

Restores NuGet dependencies.

```bash
dotnet build
```

Builds the application.

```bash
dotnet run --project src/SmartJam/SmartJam.csproj
```

Runs the application directly from the project file.

```bash
dotnet clean
```

Cleans generated build files.

---

## Example Tests

### Test 1: A4

```text
Input frequency: 440 Hz
Expected note: A4
```

### Test 2: A2

```text
Input frequency: 110 Hz
Expected note: A2
```

### Test 3: C4

```text
Input frequency: approximately 261.63 Hz
Expected note: C4
```

### Test 4: Silence

```text
Input: silence or very low signal
Expected result: no reliable note
```

### Test 5: Chord Progression

Using the accompaniment generator:

```text
Key: C
Style: pop
Bars: 8
```

Expected type of progression:

```text
C – G – Am – F
```

The exact output depends on the implementation and selected parameters.

---

## Design and Technical Decisions

### Why Use an Internal Oscillator?

The internal oscillator provides:

- reproducible tests;
- hardware-independent validation;
- a way to debug the pitch detector;
- a simple demonstration mode;
- a reference signal with known frequency.

This makes it easier to separate software problems from hardware or driver problems.

### Why Use the YIN Algorithm?

The YIN algorithm is designed for fundamental frequency estimation.

It is useful for:

- monophonic instruments;
- voice-like signals;
- clean audio;
- real-time pitch detection.

It is not a complete polyphonic music transcription system.

### Why Use Avalonia?

Avalonia provides:

- a desktop graphical interface;
- XAML-based UI development;
- MVVM-compatible patterns;
- modern styling;
- cross-platform UI capabilities.

The current audio implementation remains mainly Windows-oriented because of NAudio, WASAPI, and ASIO.

### Why Use Rule-Based Accompaniment?

The accompaniment generator uses music theory rather than machine learning.

This makes the behavior:

- predictable;
- explainable;
- easy to debug;
- easy to demonstrate during the TPI;
- independent of a training dataset.

The limitation is that rule-based generation cannot understand every musical context as flexibly as a more advanced AI or machine-learning system.

---

## TPI Deliverable Perspective

SmartJam demonstrates several competencies relevant to a TPI project:

### Software Development

- C# application development;
- .NET project organization;
- solution and project configuration;
- separation of concerns;
- service-oriented code structure;
- MVVM-oriented UI logic.

### Audio Processing

- audio device management;
- sample buffers;
- audio drivers;
- sample rates;
- buffer sizes;
- RMS and peak measurements;
- real-time processing.

### Digital Signal Processing

- fundamental frequency detection;
- YIN pitch detection;
- signal validation;
- frequency-to-note conversion;
- note stability filtering.

### User Interface Development

- Avalonia XAML;
- data binding;
- controls and layouts;
- settings windows;
- visual meters;
- status messages;
- user interaction.

### Music Theory

- note names;
- MIDI note values;
- scales;
- chords;
- chord qualities;
- chord progressions;
- tonal keys.

### Testing and Debugging

- internal signal generation;
- known-frequency validation;
- audio logs;
- device refresh;
- error messages;
- reset functionality;
- manual test procedures.

---

## Project Status Summary

### Implemented

- desktop application startup;
- Avalonia graphical interface;
- .NET 10 project;
- real-time audio engine;
- internal sine-wave oscillator;
- live audio mode;
- WASAPI Shared support;
- WASAPI Exclusive support;
- ASIO support;
- input and output device selection;
- sample-rate selection;
- buffer-size selection;
- audio level meters;
- pitch detection;
- YIN algorithm;
- frequency-to-note conversion;
- played note history;
- reset notes functionality;
- probable key and chord properties;
- rule-based chord progression generation;
- pop, jazz, and blues progression patterns;
- settings window;
- diagnostic logging;
- Windows application manifest.

### Partially Implemented or Still Limited

- automatic key detection;
- automatic chord recognition;
- polyphonic analysis;
- stable detection with complex signals;
- accompaniment integration;
- accompaniment playback;
- MIDI output;
- rhythm generation;
- synchronization between detected notes and accompaniment;
- settings persistence;
- cross-platform audio support;
- automated testing;
- complete user documentation.

---

## Conclusion

SmartJam is my TPI project: a C# desktop application for real-time musical note analysis and accompaniment experimentation.

Its main functional workflow is already present:

```text
Capture or generate audio
    ↓
Analyze the signal
    ↓
Detect the fundamental frequency
    ↓
Convert frequency to a musical note
    ↓
Display the result in real time
```

The project also includes the foundations of a musical accompaniment system through rule-based chord progression generation.

The most reliable part of the application is the pitch detection workflow, especially when using the internal test oscillator. Live audio input is supported but depends on the computer, audio drivers, input device, and signal quality.

SmartJam should be considered a **functional TPI prototype** rather than a finished commercial product. It successfully demonstrates real-time audio analysis and musical note detection, while more advanced features such as complete chord recognition, automatic accompaniment playback, polyphonic analysis, and cross-platform audio support remain future improvements.
