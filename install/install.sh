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
#   --no-desktop       Linux only: skip the desktop entry and icon.
#   -h, --help         Show this help.
#
set -euo pipefail

REPO="maotovisk/MapWizard"
API="https://api.github.com/repos/${REPO}"
PAGE="https://github.com/${REPO}/releases"
RAW="https://raw.githubusercontent.com/${REPO}"

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
  --no-desktop       Linux only: skip the desktop entry and icon.
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

# Releases don't ship a standalone icon asset, so pull one straight from the
# source tree at the exact release tag (falling back to the default branch if the
# tag is unknown). Prefers the 256px variant; the 1024px master works everywhere
# and is scaled by the launcher. Returns non-zero if nothing could be fetched.
fetch_icon() {
    local out="$1" ref name url
    for ref in "$TAG" HEAD; do
        if [[ -n "$ref" && "$ref" != "latest" ]]; then
            for name in app-icon-256.png app-icon.png; do
                url="${RAW}/${ref}/MapWizard.Desktop/Assets/${name}"
                if curl -fsSL -o "$out" "$url" 2>/dev/null && [[ -s "$out" ]]; then
                    return 0
                fi
            done
        fi
    done
    rm -f "$out"
    return 1
}

# Picks the hicolor theme directory matching a PNG's real width, so the icon
# isn't filed under a size it doesn't have. Falls back to 256x256 if the width
# can't be read (the header is 4 big-endian bytes at offset 16).
png_icon_size() {
    local width
    width="$(od -An -tu4 -N4 -j16 --endian=big "$1" 2>/dev/null | tr -d '[:space:]')"
    case "$width" in
        "" | *[!0-9]*) printf '256x256' ;;
        *)
            if ((width >= 512)); then printf '512x512'
            elif ((width >= 256)); then printf '256x256'
            elif ((width >= 128)); then printf '128x128'
            elif ((width >= 64)); then printf '64x64'
            elif ((width >= 48)); then printf '48x48'
            else printf '256x256'
            fi
            ;;
    esac
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
        hicolor_dir="$data_dir/icons/hicolor"
        mkdir -p "$applications_dir"

        icon_line=""
        icon_tmp="$tmpdir/mapwizard-icon.png"
        if fetch_icon "$icon_tmp"; then
            icons_dir="$hicolor_dir/$(png_icon_size "$icon_tmp")/apps"
            mkdir -p "$icons_dir"
            mv -f "$icon_tmp" "$icons_dir/mapwizard.png"
            icon_line="Icon=mapwizard"
        else
            # Last resort: extract the icon bundled inside the AppImage.
            icons_dir="$hicolor_dir/256x256/apps"
            if (cd "$tmpdir" && "$dest" --appimage-extract '.DirIcon' >/dev/null 2>&1) &&
                [[ -f "$tmpdir/squashfs-root/.DirIcon" ]]; then
                mkdir -p "$icons_dir"
                cp "$tmpdir/squashfs-root/.DirIcon" "$icons_dir/mapwizard.png" 2>/dev/null &&
                    icon_line="Icon=mapwizard"
            fi
        fi

        {
            printf '[Desktop Entry]\n'
            printf 'Type=Application\n'
            printf 'Name=MapWizard\n'
            printf 'GenericName=osu! beatmap tool\n'
            printf 'Comment=Copy hit sounds, manage metadata, edit combo colours and clean up osu! beatmaps\n'
            printf 'Exec="%s" %%U\n' "$dest"
            printf 'TryExec="%s"\n' "$dest"
            printf 'Terminal=false\n'
            printf 'Categories=Utility;\n'
            printf 'Keywords=osu;beatmap;hitsound;metadata;combo colour;cleaner;\n'
            printf 'StartupNotify=true\n'
            printf 'StartupWMClass=MapWizard.Desktop\n'
            if [[ -n "$icon_line" ]]; then
                printf '%s\n' "$icon_line"
            fi
        } >"$applications_dir/mapwizard.desktop"

        command -v update-desktop-database >/dev/null 2>&1 &&
            update-desktop-database "$applications_dir" >/dev/null 2>&1 || true
        command -v gtk-update-icon-cache >/dev/null 2>&1 &&
            gtk-update-icon-cache -q -t -f "$hicolor_dir" >/dev/null 2>&1 || true

        case ":${PATH}:" in
            *":${dest_dir}:"*) ;;
            *) warn "${dest_dir} is not on your PATH; run MapWizard with ${dest}" ;;
        esac
        log "Desktop entry and icon installed; launch MapWizard from your applications menu."
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
