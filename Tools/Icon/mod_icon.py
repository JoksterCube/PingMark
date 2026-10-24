# Builds the PingMark icon on top of the shared base (Thunderstore/icon_base.png): a corner badge with a ping marker.
# The badge frame matches FairCraftingLuck's clover badge so the mod icons read as one family.
# Usage (needs Pillow + numpy): python mod_icon.py <icon_base.png> <output icon.png> [preview.png]
import sys
from PIL import Image, ImageDraw, ImageFilter

W, SS = 256, 4
DARK = (24, 20, 16, 255)

def badge_frame(d, X, Y, R):
    # dark rim, gold ring, dark line, cream face
    d.ellipse([X - R, Y - R, X + R, Y + R], fill=DARK)
    R2 = R - 3.5 * SS
    d.ellipse([X - R2, Y - R2, X + R2, Y + R2], fill=(176, 132, 62, 255))
    R3 = R2 - 5 * SS
    d.ellipse([X - R3, Y - R3, X + R3, Y + R3], fill=DARK)
    R4 = R3 - 2.5 * SS
    d.ellipse([X - R4, Y - R4, X + R4, Y + R4], fill=(238, 226, 190, 255))

def ping_pin(d, X, Y):
    # ripple rings around the pin tip, then a teardrop pin with a light center dot
    red, red_hi, ring = (206, 64, 44, 255), (232, 96, 66, 255), (196, 120, 60, 255)
    tx, ty = X, Y + 16 * SS
    for rx, ry, w in [(19, 6.2, 2.2), (11, 3.6, 2.4)]:
        box = [tx - rx * SS, ty - ry * SS, tx + rx * SS, ty + ry * SS]
        d.ellipse(box, outline=DARK, width=int((w + 2.2) * SS))
        d.ellipse([box[0] + 1.1 * SS, box[1] + 1.1 * SS, box[2] - 1.1 * SS, box[3] - 1.1 * SS], outline=ring, width=int(w * SS))
    hx, hy, hr = X, Y - 6 * SS, 12.5 * SS
    for pad, col in [(1.8 * SS, DARK), (0, red)]:
        r = hr + pad
        d.ellipse([hx - r, hy - r, hx + r, hy + r], fill=col)
        d.polygon([(hx - r * 0.82, hy + r * 0.57), (hx + r * 0.82, hy + r * 0.57), (tx, ty + pad * 1.4)], fill=col)
    hl = hr * 0.72
    d.chord([hx - hl, hy - hl, hx + hl, hy + hl], 200, 290, fill=red_hi)
    dr = 5.2 * SS
    d.ellipse([hx - dr - 1.6 * SS, hy - dr - 1.6 * SS, hx + dr + 1.6 * SS, hy + dr + 1.6 * SS], fill=DARK)
    d.ellipse([hx - dr, hy - dr, hx + dr, hy + dr], fill=(250, 242, 214, 255))

def ping_badge(img, cx=204, cy=204, r=40):
    layer = Image.new("RGBA", (W * SS, W * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    X, Y, R = cx * SS, cy * SS, r * SS
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([X - R, Y - R + 3 * SS, X + R, Y + R + 3 * SS], fill=(0, 0, 0, 120))
    sh = sh.filter(ImageFilter.GaussianBlur(4 * SS))
    badge_frame(d, X, Y, R)
    ping_pin(d, X, Y)
    combined = Image.alpha_composite(sh, layer).resize((W, W), Image.LANCZOS)
    return Image.alpha_composite(img, combined)

base = Image.open(sys.argv[1]).convert("RGBA")
icon = ping_badge(base)
icon.save(sys.argv[2], optimize=True)

if len(sys.argv) > 3:
    sheet = Image.new("RGBA", (W * 2, W * 2), (255, 255, 255, 255))
    sheet.paste((28, 30, 34, 255), (0, W, W * 2, W * 2))
    for row in range(2):
        for i, im in enumerate([base, icon]):
            sheet.alpha_composite(im, (i * W, row * W))
    sheet.convert("RGB").save(sys.argv[3])
