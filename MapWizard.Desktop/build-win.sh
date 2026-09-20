#!/bin/bash

# NativeAOT cannot cross-compile between operating systems, so this script must
# run on Windows (for example from Git Bash). On Windows you can also use
# build-win.bat, which does the same thing.

# Find the absolute path of the script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if version parameter is provided
if [ "$#" -ne 1 ]; then
    echo "Version number is required."
    echo "Usage: ./build.sh [version]"
    exit 1
fi

BUILD_VERSION="$1"
RELEASE_DIR="$SCRIPT_DIR/releases"
PUBLISH_DIR="$SCRIPT_DIR/publish"
ICON_PATH="$SCRIPT_DIR/Assets/app-icon.ico"

echo "Cleaning up previous build..."
dotnet clean
echo ""
echo "Compiling MapWizard with dotnet..."
dotnet publish -c Release --self-contained -r win-x64 -o "$PUBLISH_DIR"

# NativeAOT emits debug symbols next to the binary; they are not needed in the
# installer payload.
find "$PUBLISH_DIR" -maxdepth 1 \( -name '*.dbg' -o -name '*.pdb' \) -delete

echo ""
echo "Building Velopack Release v$BUILD_VERSION"
vpk [win] pack --runtime win-x64 -u MapWizard.Desktop --packTitle "MapWizard"  -v $BUILD_VERSION -o "$RELEASE_DIR" -p "$PUBLISH_DIR" -e "MapWizard.Desktop.exe" -i "$ICON_PATH"
