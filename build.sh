#!/bin/bash
set -e # Exit immediately if a command exits with a non-zero status

# Default build flags
BUILD_X64=true
BUILD_ARM=true

# Parse command-line argument if provided
if [ $# -gt 0 ]; then
    case "$1" in
        x64|--x64)
            BUILD_ARM=false
            ;;
        arm|arm64|--arm|--arm64)
            BUILD_X64=false
            ;;
        *)
            echo "Error: Unknown argument '$1'. Usage: $0 [x64|arm]"
            exit 1
            ;;
    esac
fi

echo "Building..."

cd nptui/ || { echo "Error: nptui directory not found."; exit 1; }

# Build x64
if [ "$BUILD_X64" = true ]; then
    echo "Building for Linux x64..."
    dotnet publish -c Release -r linux-x64 --self-contained -o ../binaries/linux-x64
fi

# Build ARM64
if [ "$BUILD_ARM" = true ]; then
    echo "Building for Linux ARM64..."
    dotnet publish -c Release -r linux-arm64 --self-contained -o ../binaries/linux-arm64
fi

# Go back to the root directory
cd ../

# Clean up & rename x64 binary
if [ "$BUILD_X64" = true ]; then
    rm -f binaries/linux-x64/nptui_x64
    cp binaries/linux-x64/nptui binaries/linux-x64/nptui_x64
fi

# Clean up & rename ARM binary
if [ "$BUILD_ARM" = true ]; then
    rm -f binaries/linux-arm64/nptui_arm
    cp binaries/linux-arm64/nptui binaries/linux-arm64/nptui_arm
fi

echo "Build complete!"