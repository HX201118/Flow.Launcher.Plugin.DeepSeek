"""生成插件图标 Images/app.ico（蓝底圆角 + 白色 DS）。"""

import os

from PIL import Image, ImageDraw, ImageFont

SIZES = [16, 24, 32, 48, 64, 128, 256]
FONT_CANDIDATES = [
    r"C:\Windows\Fonts\segoeuib.ttf",
    r"C:\Windows\Fonts\segoeui.ttf",
    r"C:\Windows\Fonts\arialbd.ttf",
    r"C:\Windows\Fonts\arial.ttf",
]

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "Flow.Launcher.Plugin.DeepSeek", "Images")


def load_font(size: int) -> ImageFont.FreeTypeFont:
    for path in FONT_CANDIDATES:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue
    return ImageFont.load_default()


def vertical_gradient(size: int, top, bottom) -> Image.Image:
    grad = Image.new("RGB", (1, size))
    px = grad.load()
    for y in range(size):
        t = y / max(size - 1, 1)
        px[0, y] = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
    return grad.resize((size, size))


def rounded_mask(size: int, radius_ratio: float) -> Image.Image:
    mask = Image.new("L", (size * 4, size * 4), 0)
    d = ImageDraw.Draw(mask)
    d.rounded_rectangle([0, 0, size * 4 - 1, size * 4 - 1], radius=int(size * 4 * radius_ratio), fill=255)
    return mask.resize((size, size), Image.LANCZOS)


def render(size: int) -> Image.Image:
    bg = vertical_gradient(size, (93, 122, 255), (45, 74, 214))
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    img.paste(bg, (0, 0), rounded_mask(size, 0.24))

    draw = ImageDraw.Draw(img)
    font_size = max(int(size * 0.46), 6)
    font = load_font(font_size)
    text = "DS"

    box = draw.textbbox((0, 0), text, font=font)
    w, h = box[2] - box[0], box[3] - box[1]
    x = (size - w) / 2 - box[0]
    y = (size - h) / 2 - box[1] - size * 0.02
    draw.text((x, y), text, font=font, fill=(255, 255, 255, 255))
    return img


def main() -> None:
    os.makedirs(OUT_DIR, exist_ok=True)
    images = [render(s) for s in SIZES]

    # ico：插件运行时使用（多尺寸）
    ico = os.path.join(OUT_DIR, "app.ico")
    images[-1].save(ico, format="ICO", sizes=[(s, s) for s in SIZES])
    print("icon written:", ico, os.path.getsize(ico), "bytes")

    # png：插件商店展示图，走 jsdelivr CDN
    repo_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    png_dir = os.path.join(repo_dir, "Images")
    os.makedirs(png_dir, exist_ok=True)
    png = os.path.join(png_dir, "app.png")
    images[-1].save(png, format="PNG")
    print("store icon written:", png, os.path.getsize(png), "bytes")


if __name__ == "__main__":
    main()
