#!/bin/bash

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
ICON_PATH="$SCRIPT_DIR/Assets/app-icon.icns"

echo "Cleaning up previous build..."
dotnet clean
echo ""
echo "Compiling MapWizardDesktop with dotnet..."
dotnet publish -c Release --self-contained -r osx-x64 -o "$PUBLISH_DIR"

echo ""
echo "Building Velopack Release v$BUILD_VERSION"
vpk [osx] pack -u MapWizard.Desktop --packTitle "MapWizard" -v $BUILD_VERSION -o "$RELEASE_DIR" -p "$PUBLISH_DIR" -e "MapWizard.Desktop" -i "$ICON_PATH" 