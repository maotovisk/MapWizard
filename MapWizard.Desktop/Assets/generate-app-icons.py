#!/usr/bin/env python3
"""Regenerate packaged icons from the canonical mapwizard.svg artwork.

The SVG is the untouched Inkscape source; this script only controls how it is
rasterized. Each target size is rendered from the vector at a supersampled
resolution and downscaled with a proper prefilter, then handed to Pillow as an
exact frame (`append_images`). That avoids Pillow's single-pass resize of one
1024px raster, which aliased badly on the small ICO/ICNS sizes.

Requires rsvg-convert and Pillow. Run from any directory with:
    python3 MapWizard.Desktop/Assets/generate-app-icons.py
"""

from io import BytesIO
from pathlib import Path
import subprocess

from PIL import Image


ASSETS = Path(__file__).resolve().parent
SOURCE = ASSETS / "mapwizard.svg"

ICO_SIZES = (16, 20, 24, 28, 32, 40, 48, 64, 96, 128, 256)
# Pillow's ICNS writer maps these widths onto ic07..ic14; 16/24 have no slot.
ICNS_SIZES = (32, 64, 128, 256, 512, 1024)
MASTER_SIZE = 1024

# Small in-app logos. Rendering these from the vector (rather than letting the
# UI downscale the 1024px master at runtime) avoids the hard aliasing that a
# large single-pass reduction produces on the thin strokes.
UI_SIZES = (64, 256)


def supersample_factor(size: int) -> int:
    """More samples at small sizes, where aliasing is most visible."""
    if size <= 32:
        return 8
    if size <= 128:
        return 4
    if size <= 512:
        return 2
    return 1


def render(size: int) -> Image.Image:
    """Rasterize the SVG at `size`, supersampled and filtered down."""
    factor = supersample_factor(size)
    raw = subprocess.run(
        [
            "rsvg-convert",
            "--width",
            str(size * factor),
            "--height",
            str(size * factor),
            str(SOURCE),
        ],
        check=True,
        capture_output=True,
    ).stdout
    image = Image.open(BytesIO(raw)).convert("RGBA")
    if factor != 1:
        image = image.resize((size, size), Image.Resampling.LANCZOS)
    return image


def main() -> None:
    frames = {size: render(size) for size in sorted(set(ICO_SIZES) | set(ICNS_SIZES) | {MASTER_SIZE})}

    frames[MASTER_SIZE].save(ASSETS / "app-icon.png", format="PNG")

    for size in UI_SIZES:
        render(size).save(ASSETS / f"app-icon-{size}.png", format="PNG")

    ico_base = frames[256]
    ico_base.save(
        ASSETS / "app-icon.ico",
        format="ICO",
        sizes=[(size, size) for size in ICO_SIZES],
        append_images=[frames[size] for size in ICO_SIZES if size != 256],
    )

    icns_base = frames[MASTER_SIZE]
    icns_base.save(
        ASSETS / "app-icon.icns",
        format="ICNS",
        append_images=[frames[size] for size in ICNS_SIZES if size != MASTER_SIZE],
    )


if __name__ == "__main__":
    main()
