import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

output_dir = r"d:\Project Unity\Survival\Assets\Flooded_Grounds\Materials\Cabin\Textures"
os.makedirs(output_dir, exist_ok=True)

# ─────────────────────────────────────────────────────────────
# 1. TEX_ECONOMYCOMFORT.PNG (1024 x 1024) - Ultra Crisp Resolution
# ─────────────────────────────────────────────────────────────
s = 1024
# Warm camel-orange base matching Image 2
base_color = (212, 126, 42, 255)
img_ec = Image.new("RGBA", (s, s), base_color)
draw_ec = ImageDraw.Draw(img_ec)

# Woven fabric micro-texture (subtle diagonal twill)
for y in range(s):
    for x in range(0, s, 4):
        if (x + y * 2) % 8 < 3:
            draw_ec.point((x, y), fill=(195, 112, 34, 255))
        elif (x - y) % 9 == 0:
            draw_ec.point((x, y), fill=(224, 138, 52, 255))

# Outer hemline / double stitch border
draw_ec.rectangle([24, 24, s - 25, s - 25], outline=(170, 95, 28, 255), width=4)
draw_ec.rectangle([34, 34, s - 35, s - 35], outline=(185, 105, 32, 255), width=2)

# Central Box (matching Image 2 signature logo box)
bw, bh = 540, 460
bx1 = (s - bw) // 2
by1 = (s - bh) // 2 + 15
bx2 = bx1 + bw
by2 = by1 + bh
# Thin dark chocolate outline box
draw_ec.rectangle([bx1, by1, bx2, by2], outline=(65, 32, 10, 240), width=6)
draw_ec.rectangle([bx1 + 3, by1 + 3, bx2 - 3, by2 - 3], outline=(95, 48, 16, 120), width=2)

# Clean typography: "Economy" and "Comfort"
font_ec = None
font_cf = None
for font_candidate in ["arial.ttf", "segoeui.ttf", "tahoma.ttf", "calibri.ttf"]:
    try:
        font_ec = ImageFont.truetype(font_candidate, 82)
        font_cf = ImageFont.truetype(font_candidate, 74)
        break
    except Exception:
        continue

text_color = (55, 28, 8, 250)

# Calculate text bounds to center perfectly
if font_ec and font_cf:
    # "Economy"
    bbox1 = draw_ec.textbbox((0, 0), "Economy", font=font_ec)
    w1 = bbox1[2] - bbox1[0]
    draw_ec.text(((s - w1) // 2, by1 + 105), "Economy", fill=text_color, font=font_ec)

    # "Comfort"
    bbox2 = draw_ec.textbbox((0, 0), "Comfort", font=font_cf)
    w2 = bbox2[2] - bbox2[0]
    draw_ec.text(((s - w2) // 2, by1 + 225), "Comfort", fill=text_color, font=font_cf)
else:
    draw_ec.text((s // 2 - 100, by1 + 120), "Economy", fill=text_color)
    draw_ec.text((s // 2 - 90, by1 + 230), "Comfort", fill=text_color)

ec_path = os.path.join(output_dir, "Tex_EconomyComfort.png")
img_ec.save(ec_path)
print(f"[OK] Saved {ec_path} (1024x1024)")

# ─────────────────────────────────────────────────────────────
# 2. TEX_SEATFABRIC_BLUE.PNG (512 x 512) - Rich Royal Blue Woven
# ─────────────────────────────────────────────────────────────
s_fb = 512
img_fb = Image.new("RGBA", (s_fb, s_fb), (22, 52, 128, 255))
draw_fb = ImageDraw.Draw(img_fb)

for y in range(s_fb):
    for x in range(s_fb):
        # Fine cross-hatch fabric weave
        if (x // 2 + y // 2) % 2 == 0:
            draw_fb.point((x, y), fill=(30, 68, 158, 255))
        elif (x + y * 3) % 7 == 0:
            draw_fb.point((x, y), fill=(16, 40, 102, 255))
        elif (x * 2 - y) % 11 == 0:
            draw_fb.point((x, y), fill=(38, 80, 175, 255))

fb_path = os.path.join(output_dir, "Tex_SeatFabric_Blue.png")
img_fb.save(fb_path)
print(f"[OK] Saved {fb_path} (512x512)")

# ─────────────────────────────────────────────────────────────
# 3. TEX_AISLECARPET.PNG (512 x 512) - Aviation Dot-Pattern Carpet
# ─────────────────────────────────────────────────────────────
s_cp = 512
img_cp = Image.new("RGBA", (s_cp, s_cp), (18, 32, 68, 255))
draw_cp = ImageDraw.Draw(img_cp)

# Subtle diamond / dot grid pattern common in commercial jetliners
for y in range(s_cp):
    for x in range(s_cp):
        dx = x % 16
        dy = y % 16
        if (dx == 8 and dy == 8) or (dx == 0 and dy == 0):
            draw_cp.point((x, y), fill=(60, 105, 175, 255))
        elif abs(dx - 8) + abs(dy - 8) == 3:
            draw_cp.point((x, y), fill=(35, 62, 118, 255))
        elif (x + y) % 4 == 0:
            draw_cp.point((x, y), fill=(14, 24, 52, 255))

cp_path = os.path.join(output_dir, "Tex_AisleCarpet.png")
img_cp.save(cp_path)
print(f"[OK] Saved {cp_path} (512x512)")

# ─────────────────────────────────────────────────────────────
# 4. TEX_AMBER_PSU.PNG (256 x 256) - Circular Amber Passenger Service Unit Light
# ─────────────────────────────────────────────────────────────
s_am = 256
img_am = Image.new("RGBA", (s_am, s_am), (230, 232, 235, 255))
draw_am = ImageDraw.Draw(img_am)

# Circular metallic bezel
draw_am.ellipse([16, 16, s_am - 17, s_am - 17], fill=(210, 212, 216, 255), outline=(160, 164, 170, 255), width=4)
# Glowing amber core
draw_am.ellipse([40, 40, s_am - 41, s_am - 41], fill=(255, 185, 30, 255), outline=(230, 140, 15, 255), width=3)
draw_am.ellipse([70, 70, s_am - 71, s_am - 71], fill=(255, 225, 120, 255))

am_path = os.path.join(output_dir, "Tex_Amber_PSU.png")
img_am.save(am_path)
print(f"[OK] Saved {am_path} (256x256)")
print("ALL TEXTURES CREATED SUCCESSFULLY!")
