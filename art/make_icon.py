"""Archotech Harness icon: a glowing >_ prompt on a dark plate.

Usage: make_icon.py png OUT SIZE    one PNG (the mod icon)
       make_icon.py ico OUT         multi-size app icon, each size rendered separately
"""
import sys
from PIL import Image, ImageDraw, ImageFilter

S = 1024
GLOW, WHITE, DARK = (80, 215, 255, 255), (238, 252, 255, 255), (16, 21, 30, 255)


def render(size):
    small = size <= 32
    margin = 8 if small else 40
    fill = 0.78 if small else 0.66
    glow_strength = 1 if small else 2

    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(img).rounded_rectangle((margin, margin, S - margin, S - margin), radius=170 if small else 200, fill=DARK)

    # Glyphs drawn once, then scaled so their width fills the plate.
    glyphs = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    halo = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d, h = ImageDraw.Draw(glyphs), ImageDraw.Draw(halo)
    stroke = 150 if small else 130
    chev = [(200, 290), (450, 510), (200, 730)]
    h.line(chev, fill=GLOW, width=stroke + 80, joint="curve")
    d.line(chev, fill=WHITE, width=stroke, joint="curve")
    bar = (540, 620, 860, 620 + stroke - 20)
    h.rectangle((bar[0] - 40, bar[1] - 40, bar[2] + 40, bar[3] + 40), fill=GLOW)
    d.rectangle(bar, fill=WHITE)

    box = glyphs.getchannel("A").getbbox()
    scale = (S * fill) / (box[2] - box[0])
    w, hgt = int((box[2] - box[0]) * scale), int((box[3] - box[1]) * scale)
    pad = 120
    region = (box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad)
    size_scaled = (w + int(2 * pad * scale), hgt + int(2 * pad * scale))
    glyphs = glyphs.crop(region).resize(size_scaled, Image.LANCZOS)
    halo = halo.crop(region).resize(size_scaled, Image.LANCZOS).filter(ImageFilter.GaussianBlur(40 * scale))
    at = ((S - size_scaled[0]) // 2, (S - size_scaled[1]) // 2)

    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    layer.paste(halo, at)
    for _ in range(glow_strength):
        img = Image.alpha_composite(img, layer)
    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    layer.paste(glyphs, at)
    img = Image.alpha_composite(img, layer)
    return img.resize((size, size), Image.LANCZOS)


if sys.argv[1] == "png":
    render(int(sys.argv[3])).save(sys.argv[2])
else:
    sizes = [16, 24, 32, 48, 64, 128, 256]
    frames = [render(s) for s in sizes]
    frames[-1].save(sys.argv[2], format="ICO", sizes=[(s, s) for s in sizes], append_images=frames[:-1])
print("wrote", sys.argv[2])
