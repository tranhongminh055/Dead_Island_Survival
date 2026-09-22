import os
import numpy as np
from PIL import Image, ImageDraw

output_dir = r"d:\Project Unity\Survival\Assets\Flooded_Grounds\Materials\Wreckage\Textures"
os.makedirs(output_dir, exist_ok=True)

# 512 x 512 Soft Billowing Smoke Puff Texture
size = 512
img = Image.new("RGBA", (size, size), (0, 0, 0, 0))

# Generate multi-lobed organic cloud/smoke puff
center = (size // 2, size // 2)
radius = size * 0.42

pixels = np.zeros((size, size, 4), dtype=np.uint8)

# Center positions for sub-puffs to create organic cloud shape
sub_puffs = [
    (256, 256, 170),
    (200, 240, 130),
    (310, 245, 125),
    (235, 190, 115),
    (280, 200, 120),
    (250, 310, 135),
    (185, 300, 100),
    (320, 295, 105),
]

y_coords, x_coords = np.ogrid[:size, :size]

accum_density = np.zeros((size, size), dtype=np.float32)

for px, py, pr in sub_puffs:
    dist = np.sqrt((x_coords - px) ** 2 + (y_coords - py) ** 2)
    # Smooth cosine/gaussian falloff
    d_norm = np.clip(dist / pr, 0.0, 1.0)
    density = (np.cos(d_norm * np.pi) + 1.0) * 0.5
    accum_density = np.maximum(accum_density, density)

# Noise modulation for cloud fluffiness
np.random.seed(42)
noise = np.random.uniform(0.85, 1.0, (size, size))
accum_density = np.clip(accum_density * noise, 0.0, 1.0)

# Radial edge fade to guarantee 0 alpha at boundaries
dist_from_center = np.sqrt((x_coords - center[0]) ** 2 + (y_coords - center[1]) ** 2)
edge_mask = np.clip(1.0 - (dist_from_center / (size * 0.48)), 0.0, 1.0)
edge_mask = edge_mask ** 1.5
final_alpha = np.clip(accum_density * edge_mask * 255.0, 0, 255).astype(np.uint8)

# Dark charcoal/ashen smoke color
pixels[:, :, 0] = 60
pixels[:, :, 1] = 60
pixels[:, :, 2] = 65
pixels[:, :, 3] = final_alpha

img = Image.fromarray(pixels, "RGBA")
out_path = os.path.join(output_dir, "Tex_SmokePuff.png")
img.save(out_path)
print(f"[OK] Saved {out_path} (512x512)")
