#!/usr/bin/env bash
#
# MapWizard installer for Linux and macOS.
#
# Windows uses the Setup.exe from the releases page instead; this script only
# handles the platforms where Velopack ships an AppImage (.AppImage) or a pkg
# (.pkg).
#
# Usage:
#   curl -fsSL https://mapwizard.maot.dev/install | bash
#
# Options:
#   --pre              Install the newest pre-release instead of the latest stable.
#   --version <tag>    Install a specific release tag (e.g. v2.7.2 or 3.0.0-rc2).
#   --dir <path>       Linux only: where to place the AppImage (default: ~/.local/bin).
#   --portable         macOS only: unzip to ~/Applications instead of using the pkg.
#   --no-desktop       Linux only: skip the desktop launcher.
#   -h, --help         Show this help.
#
set -euo pipefail

REPO="maotovisk/MapWizard"
API="https://api.github.com/repos/${REPO}"
PAGE="https://github.com/${REPO}/releases"

CHANNEL="stable"
VERSION=""
INSTALL_DIR=""
PORTABLE=0
NO_DESKTOP=0

log() { printf '%s\n' "$*"; }
warn() { printf 'warning: %s\n' "$*" >&2; }
die() {
    printf 'error: %s\n' "$*" >&2
    exit 1
}

usage() {
    cat <<'EOF'
MapWizard installer for Linux and macOS.

Usage:
  curl -fsSL https://mapwizard.maot.dev/install | bash

Options:
  --pre              Install the newest pre-release instead of the latest stable.
  --version <tag>    Install a specific release tag (e.g. v2.7.2 or 3.0.0-rc2).
  --dir <path>       Linux only: where to place the AppImage (default: ~/.local/bin).
  --portable         macOS only: unzip to ~/Applications instead of using the pkg.
  --no-desktop       Linux only: skip the desktop launcher.
  -h, --help         Show this help.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --pre) CHANNEL="prerelease" ;;
        --version)
            [[ $# -ge 2 ]] || die "--version needs a tag"
            VERSION="$2"
            shift
            ;;
        --dir)
            [[ $# -ge 2 ]] || die "--dir needs a path"
            INSTALL_DIR="$2"
            shift
            ;;
        --portable) PORTABLE=1 ;;
        --no-desktop) NO_DESKTOP=1 ;;
        -h | --help)
            usage
            exit 0
            ;;
        *) die "unknown option: $1 (try --help)" ;;
    esac
    shift
done

command -v curl >/dev/null 2>&1 || die "curl is required"
command -v uname >/dev/null 2>&1 || die "uname is required"

OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
    Linux) ;;
    Darwin) ;;
    *) die "unsupported platform '${OS}'. On Windows, download and run the Setup.exe from ${PAGE}/latest" ;;
esac

case "$ARCH" in
    x86_64 | amd64) ;;
    arm64)
        [[ "$OS" == "Darwin" ]] || die "no arm64 Linux build is published yet (only linux-x64)"
        warn "the macOS build is x64; Apple Silicon needs Rosetta 2 (macOS will offer to install it)"
        ;;
    aarch64)
        die "no arm64 Linux build is published yet (only linux-x64)"
        ;;
    *) die "unsupported architecture '${ARCH}'" ;;
esac

# Pull the release metadata. Stable uses releases/latest; --pre takes the newest
# release of any kind; a pinned version looks the tag up directly.
release_json=""
fetch_release() {
    if [[ -n "$VERSION" ]]; then
        local tag="$VERSION"
        if [[ "$tag" != v* ]]; then
            release_json="$(curl -fsSL "${API}/releases/tags/v${tag}" 2>/dev/null)" ||
                release_json="$(curl -fsSL "${API}/releases/tags/${tag}" 2>/dev/null)" || return 1
        else
            release_json="$(curl -fsSL "${API}/releases/tags/${tag}" 2>/dev/null)" || return 1
        fi
    elif [[ "$CHANNEL" == "prerelease" ]]; then
        release_json="$(curl -fsSL "${API}/releases?per_page=1" 2>/dev/null)" || return 1
    else
        release_json="$(curl -fsSL "${API}/releases/latest" 2>/dev/null)" || return 1
    fi
    return 0
}

release_tag() {
    printf '%s' "$release_json" |
        grep -o '"tag_name"[[:space:]]*:[[:space:]]*"[^"]*"' |
        head -n 1 |
        sed -E 's/.*"([^"]*)"$/\1/'
}

# Finds a browser_download_url whose name ends with the given suffix.
asset_url() {
    printf '%s' "$release_json" |
        grep -o '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]*"' |
        sed -E 's/.*"(https:[^"]*)"/\1/' |
        grep -F -- "$1" |
        head -n 1
}

fallback_url() {
    local name="$1" tag
    tag="$(release_tag || true)"
    if [[ -n "$tag" ]]; then
        printf '%s/download/%s/%s' "$PAGE" "$tag" "$name"
    else
        printf '%s/latest/download/%s' "$PAGE" "$name"
    fi
}

fetch_release || true

if [[ -z "$release_json" && "$CHANNEL" == "prerelease" ]]; then
    die "could not reach the GitHub API to find the newest pre-release"
fi

TAG="$(release_tag || true)"
[[ -n "$TAG" ]] || TAG="${VERSION:-latest}"

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

download() {
    local url="$1" out="$2"
    log "Downloading $(basename "$out") ..."
    curl -fL --progress-bar -o "$out" "$url" || die "download failed: $url"
}

linux_asset="MapWizard.Desktop.AppImage"
macos_pkg="MapWizard.Desktop-osx-Setup.pkg"
macos_zip="MapWizard.Desktop-osx-Portable.zip"

if [[ "$OS" == "Linux" ]]; then
    url="$(asset_url ".AppImage" || true)"
    [[ -n "$url" ]] || url="$(fallback_url "$linux_asset")"

    dest_dir="${INSTALL_DIR:-$HOME/.local/bin}"
    dest="${dest_dir}/MapWizard.AppImage"
    mkdir -p "$dest_dir"
    download "$url" "$tmpdir/$linux_asset"
    mv -f "$tmpdir/$linux_asset" "$dest"
    chmod +x "$dest"

    if [[ "$NO_DESKTOP" -eq 0 ]]; then
        data_dir="${XDG_DATA_HOME:-$HOME/.local/share}"
        applications_dir="$data_dir/applications"
        icons_dir="$data_dir/icons/hicolor/256x256/apps"
        mkdir -p "$applications_dir"

        icon_line=""
        if (cd "$tmpdir" && "$dest" --appimage-extract '.DirIcon' >/dev/null 2>&1) &&
            [[ -f "$tmpdir/squashfs-root/.DirIcon" ]]; then
            mkdir -p "$icons_dir"
            if cp "$tmpdir/squashfs-root/.DirIcon" "$icons_dir/mapwizard.png" 2>/dev/null; then
                icon_line="Icon=mapwizard"
            fi
        fi

        {
            printf '[Desktop Entry]\n'
            printf 'Type=Application\n'
            printf 'Name=MapWizard\n'
            printf 'Comment=osu! beatmap utility suite\n'
            printf 'Exec=%s %%U\n' "$dest"
            printf 'Terminal=false\n'
            printf 'Categories=Utility;\n'
            if [[ -n "$icon_line" ]]; then
                printf '%s\n' "$icon_line"
            fi
        } >"$applications_dir/mapwizard.desktop"

        command -v update-desktop-database >/dev/null 2>&1 &&
            update-desktop-database "$applications_dir" >/dev/null 2>&1 || true

        case ":${PATH}:" in
            *":${dest_dir}:"*) ;;
            *) warn "${dest_dir} is not on your PATH; run MapWizard with ${dest}" ;;
        esac
    fi

    log ""
    log "MapWizard ${TAG} installed to ${dest}"
    log "Launch it with: ${dest}"
else
    # macOS
    if [[ "$PORTABLE" -eq 1 ]]; then
        url="$(asset_url "-osx-Portable.zip" || true)"
        [[ -n "$url" ]] || url="$(fallback_url "$macos_zip")"
        dest_dir="$HOME/Applications"
        mkdir -p "$dest_dir"
        download "$url" "$tmpdir/$macos_zip"
        log "Extracting to ${dest_dir} ..."
        ditto -x -k "$tmpdir/$macos_zip" "$dest_dir" >/dev/null 2>&1 ||
            die "could not extract ${macos_zip}"
        app_path="$(find "$dest_dir" -maxdepth 1 -name 'MapWizard*.app' -print -quit)"
        [[ -n "$app_path" ]] && xattr -dr com.apple.quarantine "$app_path" 2>/dev/null || true
        log ""
        log "MapWizard ${TAG} installed to ${app_path:-$dest_dir}"
    else
        url="$(asset_url ".pkg" || true)"
        [[ -n "$url" ]] || url="$(fallback_url "$macos_pkg")"
        download "$url" "$tmpdir/$macos_pkg"

        if [[ ! -t 0 && ! -e /dev/tty ]]; then
            die "the installer needs sudo but no terminal is available; run without piping: curl -fsSL ... -o install.sh && bash install.sh"
        fi

        log "Installing the package (sudo required) ..."
        sudo installer -pkg "$tmpdir/$macos_pkg" -target / ||
            die "package installation failed"

        app_path="$(find /Applications "$HOME/Applications" -maxdepth 1 -name 'MapWizard*.app' -print -quit 2>/dev/null)"
        [[ -n "$app_path" ]] && sudo xattr -dr com.apple.quarantine "$app_path" 2>/dev/null || true

        log ""
        log "MapWizard ${TAG} installed${app_path:+ to $app_path}"
    fi
fi

log "Update later from inside the app (Settings > Updates)."
