"""Contact sheet of all fish icons (images/sprites/FishSort/Image/Fish/fish_N.png) with ids."""
import os, re, glob
from PIL import Image, ImageDraw, ImageFont
ROOT = r"D:\ExtractApk\FishJamFever"
SRC = os.path.join(ROOT, "images", "sprites", "FishSort", "Image", "Fish")
OUT = os.path.join(ROOT, "images", "fish_contact_sheet.png")
files = []
for p in glob.glob(os.path.join(SRC, "*.png")):
    m = re.search(r"fish_(\d+)\.png$", os.path.basename(p))
    if m: files.append((int(m.group(1)), p))
files.sort()
CELL, LABEL, COLS = 96, 16, 20
rows = (len(files) + COLS - 1) // COLS
sheet = Image.new("RGBA", (COLS * CELL, rows * (CELL + LABEL)), (30, 30, 40, 255))
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("arial.ttf", 12)
except Exception:
    font = ImageFont.load_default()
for i, (fid, p) in enumerate(files):
    im = Image.open(p).convert("RGBA")
    im.thumbnail((CELL - 8, CELL - 8))
    x = (i % COLS) * CELL; y = (i // COLS) * (CELL + LABEL)
    sheet.alpha_composite(im, (x + (CELL - im.width) // 2, y + (CELL - im.height) // 2))
    draw.text((x + 4, y + CELL), f"fish_{fid}", fill=(255, 255, 255, 255), font=font)
sheet.save(OUT)
print("fish icons:", len(files), "->", OUT, sheet.size)
