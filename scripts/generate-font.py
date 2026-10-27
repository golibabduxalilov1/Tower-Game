"""Build a reproducible Unicode glyph atlas from the bundled OFL Noto Sans.

Run from anywhere: py scripts/generate-font.py
Output: assets/generated/font.png + font.json (consumed by Game.Rendering.Painter).
Each glyph is rendered into its own cell image and clipped, with an empty 2 px gutter,
so bilinear filtering at small sizes never picks up pixels of neighbouring glyphs.
Glyphs missing from Noto Sans (stars, check marks, arrows) are drawn as geometry in code.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "generated"
OUT.mkdir(parents=True, exist_ok=True)
SIZE = 48
CELL_W, CELL_H, COLUMNS, GUTTER = 60, 80, 16, 2
PEN_X, PEN_Y = 4, 6  # pen position inside a cell, gutter included
extra = [0x2018, 0x2019, 0x02BB, 0x02BC, 0x2013, 0x2014, 0x2022, 0x2026, 0x2039, 0x203A]
codes = list(range(32, 127)) + list(range(0xA0, 0x100)) + extra
characters = "".join(chr(c) for c in codes)
font = ImageFont.truetype(str(ROOT / "assets/fonts/NotoSans.ttf"), SIZE)
font.set_variation_by_axes([560, 100])
rows = (len(characters) + COLUMNS - 1) // COLUMNS
atlas = Image.new("RGBA", (COLUMNS * CELL_W, rows * CELL_H))
widths = []
for i, char in enumerate(characters):
    inner = Image.new("RGBA", (CELL_W - 2 * GUTTER, CELL_H - 2 * GUTTER))
    ImageDraw.Draw(inner).text((PEN_X - GUTTER, PEN_Y - GUTTER), char, font=font, fill="white")
    atlas.paste(inner, ((i % COLUMNS) * CELL_W + GUTTER, (i // COLUMNS) * CELL_H + GUTTER))
    widths.append(round(font.getlength(char), 2))
atlas.save(OUT / "font.png")
(OUT / "font.json").write_text(json.dumps({
    "characters": characters, "size": SIZE, "cellWidth": CELL_W, "cellHeight": CELL_H,
    "columns": COLUMNS, "offsetX": PEN_X, "offsetY": PEN_Y, "widths": widths
}, ensure_ascii=True), encoding="utf-8")
print(f"{len(characters)} glyphs, {atlas.size[0]}x{atlas.size[1]}")

# A separate heavy, condensed display face carries the game title and key counters.
display_size, display_w, display_h = 72, 100, 116
display_font = ImageFont.truetype(str(ROOT / "assets/fonts/NotoSans.ttf"), display_size)
display_font.set_variation_by_axes([850, 88])
display_atlas = Image.new("RGBA", (COLUMNS * display_w, rows * display_h))
display_widths = []
for i, char in enumerate(characters):
    tile = Image.new("RGBA", (display_w - 6, display_h - 6))
    ImageDraw.Draw(tile).text((3, 5), char, font=display_font, fill="white")
    display_atlas.paste(tile, ((i % COLUMNS) * display_w + 3, (i // COLUMNS) * display_h + 3))
    display_widths.append(round(display_font.getlength(char), 2))
display_atlas.save(OUT / "display.png")
(OUT / "display.json").write_text(json.dumps({
    "characters": characters, "size": display_size, "cellWidth": display_w,
    "cellHeight": display_h, "columns": COLUMNS, "offsetX": 6,
    "offsetY": 8, "widths": display_widths
}, ensure_ascii=True), encoding="utf-8")
print(f"Display: {len(characters)} glyphs, {display_atlas.size[0]}x{display_atlas.size[1]}")
