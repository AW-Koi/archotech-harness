"""Archotech Harness mod icon: a glowing >_ prompt centred on a dark archotech plate."""
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter

out = sys.argv[1]
S = 1024
glow_col, white, dark = (90, 220, 255, 230), (235, 252, 255, 255), (14, 18, 26, 255)

mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle((24, 24, S - 24, S - 24), radius=190, fill=255)
img = Image.new("RGBA", (S, S), dark)
ImageDraw.Draw(img).rounded_rectangle((24, 24, S - 24, S - 24), radius=190, outline=(70, 200, 230, 255), width=22)

glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
shapes = Image.new("RGBA", (S, S), (0, 0, 0, 0))
g, d = ImageDraw.Draw(glow), ImageDraw.Draw(shapes)

chev = [(200, 300), (440, 510), (200, 720)]
g.line(chev, fill=glow_col, width=170, joint="curve")
d.line(chev, fill=white, width=112, joint="curve")
bar = (530, 640, 840, 736)
g.rectangle((bar[0] - 12, bar[1] - 12, bar[2] + 12, bar[3] + 12), fill=glow_col)
d.rectangle(bar, fill=white)

# Centre the drawn glyphs so the margins match on every side.
left, top, right, bottom = shapes.getchannel("A").getbbox()
dx, dy = (S - (left + right)) // 2, (S - (top + bottom)) // 2
shapes = ImageChops.offset(shapes, dx, dy)
glow = ImageChops.offset(glow, dx, dy)

img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(34)))
img = Image.alpha_composite(img, shapes)
img.putalpha(mask)
img.resize((256, 256), Image.LANCZOS).save(out)
print("wrote", out, "offset", dx, dy)
