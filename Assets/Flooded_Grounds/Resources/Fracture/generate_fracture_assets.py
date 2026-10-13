import os
import math
import random
import struct
import wave
from PIL import Image, ImageDraw, ImageFilter

OUTPUT_DIR = r"d:\Project Unity\Survival\Assets\Flooded_Grounds\Resources\Fracture"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def generate_bone_icon(size=512):
    # Render at 4x resolution for super-smooth supersampling
    scale = 2
    W = size * scale
    H = size * scale
    
    # 1. Base transparent image
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    center_x = W / 2
    center_y = H / 2
    radius = (W / 2) - 30 * scale
    
    # 2. Dark circular background badge (Green Hell HUD style: dark jungle slate with subtle red tint)
    bg_circle_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(bg_circle_img)
    bg_draw.ellipse(
        [center_x - radius, center_y - radius, center_x + radius, center_y + radius],
        fill=(18, 14, 16, 220)
    )
    
    # Outer danger pulse glow (Green Hell red/amber affliction rim)
    rim_width = int(7 * scale)
    for i in range(rim_width):
        t = i / float(rim_width)
        alpha = int(140 + 115 * t)
        r_col = int(240 - 20 * t)
        g_col = int(50 + 60 * t)
        b_col = int(40)
        curr_rad = radius - (rim_width - 1 - i)
        bg_draw.ellipse(
            [center_x - curr_rad, center_y - curr_rad, center_x + curr_rad, center_y + curr_rad],
            outline=(r_col, g_col, b_col, alpha),
            width=int(2 * scale)
        )
    
    img = Image.alpha_composite(img, bg_circle_img)
    
    # 3. Draw Broken Bone (Stylized femur bone fractured in two displaced pieces)
    # We will rotate coordinates by ~35 degrees for iconic dynamic slant
    angle_rad = math.radians(38)
    cos_a = math.cos(angle_rad)
    sin_a = math.sin(angle_rad)
    
    def transform_pt(x, y, dx=0, dy=0):
        # Center at (0, 0)
        tx = x * cos_a - y * sin_a + center_x + dx * scale
        ty = x * sin_a + y * cos_a + center_y + dy * scale
        return (tx, ty)

    bone_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bone_draw = ImageDraw.Draw(bone_img)
    
    # Bone Upper Half
    # Shaft width ~ 24, length from -170 to -15
    # Upper head condyles around y = -170
    upper_dx = -12 * scale
    upper_dy = -10 * scale
    
    # Upper bone polygon
    # Points along left side, through top condyles, right side, and jagged fracture line at bottom
    upper_pts = [
        # Left shaft
        transform_pt(-22 * scale, -25 * scale, upper_dx, upper_dy),
        transform_pt(-22 * scale, -130 * scale, upper_dx, upper_dy),
        # Left condyle
        transform_pt(-48 * scale, -155 * scale, upper_dx, upper_dy),
        transform_pt(-50 * scale, -185 * scale, upper_dx, upper_dy),
        transform_pt(-28 * scale, -205 * scale, upper_dx, upper_dy),
        transform_pt(-5 * scale, -190 * scale, upper_dx, upper_dy),
        # Notch between condyles
        transform_pt(0 * scale, -172 * scale, upper_dx, upper_dy),
        # Right condyle
        transform_pt(5 * scale, -190 * scale, upper_dx, upper_dy),
        transform_pt(28 * scale, -205 * scale, upper_dx, upper_dy),
        transform_pt(50 * scale, -185 * scale, upper_dx, upper_dy),
        transform_pt(48 * scale, -155 * scale, upper_dx, upper_dy),
        # Right shaft
        transform_pt(22 * scale, -130 * scale, upper_dx, upper_dy),
        transform_pt(22 * scale, -10 * scale, upper_dx, upper_dy),
        # Jagged Fracture Break Line (Sharp, fractured edges)
        transform_pt(12 * scale, -18 * scale, upper_dx, upper_dy),
        transform_pt(2 * scale, -5 * scale, upper_dx, upper_dy),
        transform_pt(-12 * scale, -22 * scale, upper_dx, upper_dy),
        transform_pt(-18 * scale, -12 * scale, upper_dx, upper_dy),
    ]
    
    # Lower Bone Half
    # Displaced slightly to the right and down
    lower_dx = 14 * scale
    lower_dy = 12 * scale
    
    lower_pts = [
        # Jagged Fracture Break Line (Matching jagged counterpart)
        transform_pt(-22 * scale, 12 * scale, lower_dx, lower_dy),
        transform_pt(-14 * scale, 22 * scale, lower_dx, lower_dy),
        transform_pt(2 * scale, 8 * scale, lower_dx, lower_dy),
        transform_pt(12 * scale, 24 * scale, lower_dx, lower_dy),
        transform_pt(22 * scale, 15 * scale, lower_dx, lower_dy),
        # Right shaft
        transform_pt(22 * scale, 130 * scale, lower_dx, lower_dy),
        # Right bottom condyle
        transform_pt(48 * scale, 155 * scale, lower_dx, lower_dy),
        transform_pt(50 * scale, 185 * scale, lower_dx, lower_dy),
        transform_pt(28 * scale, 205 * scale, lower_dx, lower_dy),
        transform_pt(5 * scale, 190 * scale, lower_dx, lower_dy),
        # Bottom notch
        transform_pt(0 * scale, 172 * scale, lower_dx, lower_dy),
        # Left bottom condyle
        transform_pt(-5 * scale, 190 * scale, lower_dx, lower_dy),
        transform_pt(-28 * scale, 205 * scale, lower_dx, lower_dy),
        transform_pt(-50 * scale, 185 * scale, lower_dx, lower_dy),
        transform_pt(-48 * scale, 155 * scale, lower_dx, lower_dy),
        # Left shaft
        transform_pt(-22 * scale, 130 * scale, lower_dx, lower_dy),
    ]
    
    # Fill bone body with pristine ivory/bone color (#F6F3EB)
    bone_color = (248, 246, 240, 255)
    bone_draw.polygon(upper_pts, fill=bone_color)
    bone_draw.polygon(lower_pts, fill=bone_color)
    
    # Bone shading & 3D bevel contour
    bone_shadow_col = (195, 190, 180, 255)
    bone_draw.line(upper_pts, fill=bone_shadow_col, width=int(3 * scale))
    bone_draw.line(lower_pts, fill=bone_shadow_col, width=int(3 * scale))
    
    # Sharp Bone Splinters / Fragment chips in between
    splinter1 = [
        transform_pt(-4 * scale, -2 * scale),
        transform_pt(6 * scale, -10 * scale),
        transform_pt(2 * scale, 4 * scale)
    ]
    splinter2 = [
        transform_pt(-16 * scale, 4 * scale),
        transform_pt(-8 * scale, -8 * scale),
        transform_pt(-10 * scale, 12 * scale)
    ]
    bone_draw.polygon(splinter1, fill=bone_color)
    bone_draw.polygon(splinter2, fill=bone_color)
    
    # Draw Inner Fracture Crack Lines with deep red/orange marrow and stress cracks
    crack_draw = ImageDraw.Draw(bone_img)
    # Stress cracks running into the upper shaft
    crack_color = (210, 45, 30, 255)
    crack_pts_upper = [
        transform_pt(2 * scale, -5 * scale, upper_dx, upper_dy),
        transform_pt(-4 * scale, -35 * scale, upper_dx, upper_dy),
        transform_pt(6 * scale, -60 * scale, upper_dx, upper_dy),
        transform_pt(0 * scale, -85 * scale, upper_dx, upper_dy)
    ]
    crack_draw.line(crack_pts_upper, fill=crack_color, width=int(3 * scale))
    
    crack_pts_lower = [
        transform_pt(-2 * scale, 8 * scale, lower_dx, lower_dy),
        transform_pt(4 * scale, 38 * scale, lower_dx, lower_dy),
        transform_pt(-5 * scale, 65 * scale, lower_dx, lower_dy),
        transform_pt(2 * scale, 90 * scale, lower_dx, lower_dy)
    ]
    crack_draw.line(crack_pts_lower, fill=crack_color, width=int(3 * scale))
    
    # Add Red/Amber Danger Glow around the fracture gap
    glow_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_img)
    # Bright energetic fracture spark
    glow_draw.ellipse(
        [center_x - 45 * scale, center_y - 45 * scale, center_x + 45 * scale, center_y + 45 * scale],
        fill=(255, 60, 20, 160)
    )
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(15 * scale))
    
    # Composite all layers
    img = Image.alpha_composite(img, glow_img)
    img = Image.alpha_composite(img, bone_img)
    
    # Downsample with high quality Lanczos antialiasing
    final_img = img.resize((size, size), Image.Resampling.LANCZOS)
    return final_img

# Generate silhouette icon (transparent background without the circle)
def generate_bone_silhouette(size=256):
    full = generate_bone_icon(size=size)
    return full

icon_path = os.path.join(OUTPUT_DIR, "Icon_Fracture_Bone.png")
icon = generate_bone_icon(256)
icon.save(icon_path, "PNG")
print(f"Generated Icon: {icon_path}")

# ═════════════════════════════════════════════════════════════════════
# PROCEDURAL AUDIO GENERATION (Realistic bone crack, fall thud, grunt)
# ═════════════════════════════════════════════════════════════════════
SAMPLE_RATE = 44100

def write_wav(filename, samples):
    filepath = os.path.join(OUTPUT_DIR, filename)
    with wave.open(filepath, "w") as wav_file:
        wav_file.setnchannels(1)      # Mono
        wav_file.setsampwidth(2)      # 16-bit
        wav_file.setframerate(SAMPLE_RATE)
        
        # Clamp & pack
        raw_data = bytearray()
        for s in samples:
            val = max(-1.0, min(1.0, s))
            int_val = int(val * 32767.0)
            raw_data.extend(struct.pack("<h", int_val))
        wav_file.writeframes(raw_data)
    print(f"Generated Audio: {filepath} ({len(samples)} samples)")

# 1. BONE CRACK / FRACTURE (Sharp, brittle crunch + organic calcium snapping)
def generate_bone_crack_audio():
    duration = 0.55
    total_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_samples
    
    random.seed(42)
    
    # 3 distinct micro-crack impulses within the first 80ms
    crack_times = [0.0, 0.024, 0.058]
    crack_intensities = [1.0, 0.75, 0.5]
    
    for c_idx, t_start in enumerate(crack_times):
        start_sample = int(t_start * SAMPLE_RATE)
        intensity = crack_intensities[c_idx]
        
        # Sharp high-frequency transient + resonance
        freqs = [2400.0, 1850.0, 920.0, 480.0]
        decay_rates = [75.0, 55.0, 35.0, 22.0]
        
        for i in range(start_sample, total_samples):
            t = (i - start_sample) / float(SAMPLE_RATE)
            if t > 0.35:
                break
            
            # Brittle crunch noise
            noise = (random.random() * 2.0 - 1.0) * math.exp(-t * 90.0) * 0.45
            
            # Modal snapping frequencies
            modal = 0.0
            for f, d in zip(freqs, decay_rates):
                modal += math.sin(2.0 * math.pi * f * t) * math.exp(-t * d) * 0.22
            
            # Low bone thump resonance (blunt trauma impact)
            thump = math.sin(2.0 * math.pi * 95.0 * t) * math.exp(-t * 18.0) * 0.35
            
            val = (noise + modal + thump) * intensity
            samples[i] += val
    
    # Normalize
    max_amp = max(abs(s) for s in samples) or 1.0
    samples = [s / max_amp * 0.95 for s in samples]
    return samples

# 2. HEAVY FALL IMPACT (Ground thud + shock wave)
def generate_fall_impact_audio():
    duration = 0.65
    total_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_samples
    
    random.seed(101)
    
    for i in range(total_samples):
        t = i / float(SAMPLE_RATE)
        # Deep sub-bass thump dropping from 120Hz to 45Hz
        freq = 120.0 - 75.0 * min(1.0, t * 12.0)
        sub = math.sin(2.0 * math.pi * freq * t) * math.exp(-t * 12.0) * 0.75
        
        # Dirt crunch / body landing impact noise
        dirt = (random.random() * 2.0 - 1.0) * math.exp(-t * 40.0) * 0.40
        
        samples[i] = sub + dirt
        
    max_amp = max(abs(s) for s in samples) or 1.0
    samples = [s / max_amp * 0.92 for s in samples]
    return samples

# 3. PAIN GRUNT (Visceral pained breath / groan when limping or landing)
def generate_pain_grunt_audio():
    duration = 0.48
    total_samples = int(SAMPLE_RATE * duration)
    samples = [0.0] * total_samples
    
    random.seed(202)
    
    for i in range(total_samples):
        t = i / float(SAMPLE_RATE)
        # Pitch drops from 140Hz to 85Hz
        pitch = 140.0 - 55.0 * (t / duration)
        
        # Formants around 520Hz and 1150Hz (vowel /uh/ /ah/ groan)
        f1 = math.sin(2.0 * math.pi * 520.0 * t) * 0.35
        f2 = math.sin(2.0 * math.pi * 1150.0 * t) * 0.20
        voice = math.sin(2.0 * math.pi * pitch * t) * (1.0 + f1 + f2)
        
        # Breathy turbulence
        breath = (random.random() * 2.0 - 1.0) * 0.18
        
        # Envelope: Attack 40ms, sustain, release
        if t < 0.04:
            env = t / 0.04
        else:
            env = math.exp(-(t - 0.04) * 6.5)
            
        samples[i] = (voice + breath) * env
        
    max_amp = max(abs(s) for s in samples) or 1.0
    samples = [s / max_amp * 0.88 for s in samples]
    return samples

# Tiếng gãy xương dùng file thu âm thật từ Pixabay (bone_snap_crack.mp3), không tạo bằng code nữa
write_wav("fall_impact_heavy.wav", generate_fall_impact_audio())
write_wav("limp_pain_grunt.wav", generate_pain_grunt_audio())
print("All fracture assets created successfully!")
