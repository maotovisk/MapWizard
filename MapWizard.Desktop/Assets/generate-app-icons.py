#!/usr/bin/env python3
"""Regenerate packaged icons from the canonical mapwizard.svg artwork.

Requires rsvg-convert and Pillow. Run from any directory with:
    python3 MapWizard.Desktop/Assets/generate-app-icons.py
"""

from io import BytesIO
from pathlib import Path
import subprocess

from PIL import Image


ASSETS = Path(__file__).resolve().parent
SOURCE = ASSETS / "mapwizard.svg"


def main() -> None:
    rendered = subprocess.run(
        ["rsvg-convert", "--width", "1024", "--height", "1024", str(SOURCE)],
        check=True,
        capture_output=True,
    ).stdout
    with Image.open(BytesIO(rendered)) as image:
        icon = image.convert("RGBA")
        icon.save(ASSETS / "app-icon.png", format="PNG")
        icon.save(
            ASSETS / "app-icon.ico",
            format="ICO",
            sizes=[(size, size) for size in (16, 24, 32, 48, 64, 128, 256)],
        )
        icon.save(ASSETS / "app-icon.icns", format="ICNS")


if __name__ == "__main__":
    main()
