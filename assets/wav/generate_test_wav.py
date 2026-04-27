#!/usr/bin/env python3
"""
Génère un fichier WAV de test pour le POC Pitch.
WAV PCM 16 bits, mono, 44100 Hz, 3 secondes à 440 Hz (La4/A4).

Usage : python3 generate_test_wav.py
Output : ../../assets/wav/A4_440Hz_3s.wav
"""

import struct
import math
import os

SAMPLE_RATE = 44100
DURATION    = 3.0
FREQUENCY   = 440.0    # La4 / A4
AMPLITUDE   = 0.8      # 80% du max pour éviter le clipping

output_path = os.path.join(os.path.dirname(__file__), "A4_440Hz_3s.wav")

total_samples = int(SAMPLE_RATE * DURATION)

# Générer les échantillons PCM 16 bits
pcm_data = bytearray()
for i in range(total_samples):
    t = i / SAMPLE_RATE
    sample = AMPLITUDE * math.sin(2 * math.pi * FREQUENCY * t)
    # Convertir en int16 (-32768..32767)
    int_sample = int(sample * 32767)
    int_sample = max(-32768, min(32767, int_sample))
    pcm_data += struct.pack('<h', int_sample)

data_size   = len(pcm_data)
bits_per_sample = 16
channels    = 1
byte_rate   = SAMPLE_RATE * channels * bits_per_sample // 8
block_align = channels * bits_per_sample // 8

# Ecrire l'entête WAV RIFF
with open(output_path, 'wb') as f:
    # RIFF header
    f.write(b'RIFF')
    f.write(struct.pack('<I', 36 + data_size))  # taille totale - 8
    f.write(b'WAVE')
    # fmt chunk
    f.write(b'fmt ')
    f.write(struct.pack('<I', 16))              # taille du chunk fmt
    f.write(struct.pack('<H', 1))               # PCM = 1
    f.write(struct.pack('<H', channels))
    f.write(struct.pack('<I', SAMPLE_RATE))
    f.write(struct.pack('<I', byte_rate))
    f.write(struct.pack('<H', block_align))
    f.write(struct.pack('<H', bits_per_sample))
    # data chunk
    f.write(b'data')
    f.write(struct.pack('<I', data_size))
    f.write(pcm_data)

print(f"WAV généré : {output_path}")
print(f"  {SAMPLE_RATE} Hz, {bits_per_sample} bits, mono, {DURATION}s, {FREQUENCY} Hz (A4)")
