"""Draws the application icon (an original design: a photo card and a
conversion badge) and writes it as a multi-size .ico file.

    python3 make_icon.py OUTPUT.ico [PREVIEW.png]
"""
import sys
from PIL import Image, ImageDraw

S = 1024  # drawing size; every icon size is a reduction of it


def gradient(size, top_left, bottom_right):
    """Diagonal gradient between two RGB colors."""
    img = Image.new("RGB", (size, size))
    px = img.load()
    for y in range(size):
        for x in range(size):
            t = (x + y) / (2 * (size - 1))
            px[x, y] = tuple(round(a + (b - a) * t) for a, b in zip(top_left, bottom_right))
    return img


def draw_icon():
    icon = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # Background: rounded square, blue to teal.
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle((40, 40, S - 40, S - 40), radius=210, fill=255)
    small = gradient(128, (37, 99, 235), (13, 148, 136)).resize((S, S), Image.BILINEAR)
    icon.paste(small, (0, 0), mask)

    d = ImageDraw.Draw(icon)

    # The photo: a white card with a sky, a sun and two hills.
    card = (170, 215, 760, 690)
    d.rounded_rectangle(card, radius=60, fill=(255, 255, 255, 255))
    inner = (210, 255, 720, 650)
    sky = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sky)
    sd.rounded_rectangle(inner, radius=30, fill=(186, 230, 253, 255))
    sd.ellipse((560, 300, 660, 400), fill=(251, 191, 36, 255))
    sd.polygon([(210, 650), (400, 400), (560, 650)], fill=(22, 163, 74, 255))
    sd.polygon([(430, 650), (590, 470), (720, 620), (720, 650)], fill=(21, 128, 61, 255))
    clip = Image.new("L", (S, S), 0)
    ImageDraw.Draw(clip).rounded_rectangle(inner, radius=30, fill=255)
    icon.paste(sky, (0, 0), clip)

    # The conversion badge: a white disc with two opposite arrows.
    d = ImageDraw.Draw(icon)
    cx, cy, r = 700, 700, 215
    d.ellipse((cx - r - 26, cy - r - 26, cx + r + 26, cy + r + 26), fill=(13, 120, 130, 255))
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 255, 255, 255))
    ink = (30, 64, 175, 255)
    # upper arrow, pointing right
    d.rounded_rectangle((cx - 120, cy - 82, cx + 40, cy - 38), radius=22, fill=ink)
    d.polygon([(cx + 30, cy - 128), (cx + 128, cy - 60), (cx + 30, cy + 8)], fill=ink)
    # lower arrow, pointing left
    d.rounded_rectangle((cx - 40, cy + 38, cx + 120, cy + 82), radius=22, fill=ink)
    d.polygon([(cx - 30, cy - 8), (cx - 128, cy + 60), (cx - 30, cy + 128)], fill=ink)
    return icon


def main():
    out = sys.argv[1]
    icon = draw_icon()
    sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
    base = icon.resize((256, 256), Image.LANCZOS)
    # Classic bitmap entries: readable by every tool (resource compilers,
    # installers), unlike PNG-compressed small sizes.
    base.save(out, format="ICO", sizes=[(s, s) for s in sizes], bitmap_format="bmp")
    if len(sys.argv) > 2:
        icon.resize((512, 512), Image.LANCZOS).save(sys.argv[2])


if __name__ == "__main__":
    main()
