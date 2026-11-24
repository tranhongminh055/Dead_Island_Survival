import os
import math
import random
import struct
import wave

OUTPUT_DIR = r"d:\Project Unity\Survival\Assets\Flooded_Grounds\Resources\Fracture"
os.makedirs(OUTPUT_DIR, exist_ok=True)
SAMPLE_RATE = 44100

def write_wav(filename, samples):
    filepath = os.path.join(OUTPUT_DIR, filename)
    with wave.open(filepath, "w") as wav_file:
        wav_file.setnchannels(1)      # Mono
        wav_file.setsampwidth(2)      # 16-bit
        wav_file.setframerate(SAMPLE_RATE)
        
        raw_data = bytearray()
        for s in samples:
            val = max(-1.0, min(1.0, s))
            int_val = int(val * 32767.0)
            raw_data.extend(struct.pack("<h", int_val))
        wav_file.writeframes(raw_data)
    print(f"Generated Audio: {filepath} ({len(samples)} samples)")

# 1. VISCERAL HUMAN AGONIZING SCREAM & GROAN (Tiếng rên la thất thanh đau đớn khi gãy xương)
# Structure: Sudden sharp high-pitch agony yell "ARRRRGHHH!!", into trembling vocal groan "UUUGH... AHHH..."
def generate_agony_scream():
    duration = 2.1
    total_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_samples
    random.seed(314)
    
    for i in range(total_samples):
        t = i / float(SAMPLE_RATE)
        
        # Agony Pitch Curve:
        # Starts with shock burst: high vocal pitch ~280Hz -> screams at 240Hz -> trembling descends to 140Hz -> 90Hz groan
        if t < 0.25:
            # Sudden explosive vocal burst
            pitch = 280.0 - 40.0 * (t / 0.25)
        elif t < 0.95:
            # Sustained agonizing scream with trembling vibrato (convulsion)
            tremor = math.sin(2.0 * math.pi * 7.5 * t) * 18.0
            pitch = 240.0 - 80.0 * ((t - 0.25) / 0.70) + tremor
        else:
            # Pained exhausted groan / panting breath
            pitch = 160.0 - 70.0 * ((t - 0.95) / (duration - 0.95))
            
        # Vocal Glottal Wave (Pulse with asymmetric harmonics)
        phase = (t * pitch) % 1.0
        # Glottal pulse: open quotient ~ 0.6
        if phase < 0.6:
            glottal = math.sin(phase / 0.6 * math.pi)
        else:
            glottal = -0.2 * math.sin((phase - 0.6) / 0.4 * math.pi)
            
        # Vocal Tract Formants for /AAAH/ -> /UUUGH/
        # /AAAH/ (t < 0.8): F1 ~ 750Hz, F2 ~ 1250Hz, F3 ~ 2400Hz
        # /UUUGH/ (t >= 0.8): F1 ~ 500Hz, F2 ~ 950Hz, F3 ~ 2200Hz
        blend = min(1.0, max(0.0, (t - 0.4) / 0.8))
        f1_freq = (1.0 - blend) * 780.0 + blend * 490.0
        f2_freq = (1.0 - blend) * 1280.0 + blend * 920.0
        f3_freq = 2400.0
        
        formant1 = math.sin(2.0 * math.pi * f1_freq * t) * 0.45
        formant2 = math.sin(2.0 * math.pi * f2_freq * t) * 0.35
        formant3 = math.sin(2.0 * math.pi * f3_freq * t) * 0.18
        
        # Vocal Fry & Harsh Distortion (Rough vocal cords screaming in shock)
        fry_rate = 35.0
        fry = math.sin(2.0 * math.pi * fry_rate * t) * 0.25
        voice = (glottal + formant1 + formant2 + formant3) * (1.0 + fry)
        
        # Heavy turbulent breath & air expulsion through throat
        breath_turb = (random.random() * 2.0 - 1.0) * 0.30
        
        # Amplitude Envelope:
        # 0.0 - 0.03: Rapid sharp attack (instant impact yell)
        # 0.03 - 0.6: Full blast scream (1.0)
        # 0.6 - 1.4: Trembling groan decay
        # 1.4 - 2.1: Heavy dying breath out
        if t < 0.03:
            env = t / 0.03
        elif t < 0.65:
            env = 1.0
        elif t < 1.35:
            env = 1.0 - 0.55 * ((t - 0.65) / 0.70)
        else:
            env = 0.45 * math.exp(-(t - 1.35) * 3.8)
            
        samples[i] = (voice * 0.75 + breath_turb * 0.25) * env
        
    # Soft limiter & normalize
    max_amp = max(abs(s) for s in samples) or 1.0
    samples = [math.tanh(s / max_amp * 1.3) * 0.95 for s in samples]
    return samples

# 2. WATER PLUNGE / DIVE SPLASH SOUND (Tiếng nhảy ùm xuống nước an toàn)
def generate_water_splash():
    duration = 1.1
    total_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_samples
    random.seed(555)
    
    for i in range(total_samples):
        t = i / float(SAMPLE_RATE)
        
        # Deep underwater body plunge (80Hz down to 40Hz)
        sub_freq = 90.0 - 50.0 * min(1.0, t * 5.0)
        sub_body = math.sin(2.0 * math.pi * sub_freq * t) * math.exp(-t * 8.0) * 0.8
        
        # Surface water sheet crash & spray noise
        spray_noise = (random.random() * 2.0 - 1.0)
        # Lowpass / bandpass filtering simulation for watery whoosh
        water_whoosh = spray_noise * math.exp(-t * 14.0) * 0.65
        
        # Water bubbles & bubbly gurgles (multi-frequency droplets)
        bubbles = 0.0
        for b_freq in [420.0, 680.0, 950.0, 1350.0]:
            bubbles += math.sin(2.0 * math.pi * b_freq * (t + math.sin(t * 40.0) * 0.02)) * math.exp(-t * 6.5) * 0.08
            
        env = math.exp(-t * 3.5)
        samples[i] = (sub_body + water_whoosh + bubbles) * env
        
    max_amp = max(abs(s) for s in samples) or 1.0
    samples = [s / max_amp * 0.92 for s in samples]
    return samples

# Tiếng la hét đã chuyển sang generate_voice_audio.py (mô hình giọng người source-filter)
# Tiếng nước bắn dùng file thu âm thật từ Pixabay (water_splash_fall.mp3), không tạo bằng code nữa
print("Nothing to generate - real recordings from Pixabay are used instead.")
