#!/bin/bash

# Navigate to the project directory first, or specify the project file
# Option 1: Navigate to project directory
cd nptui/ || { echo "Error: nptui directory not found."; exit 1; }

echo "Building for Linux x64..."
dotnet publish -c Release -r linux-x64 --self-contained -o ../binaries/linux-x64

# Add the previously discussed fix for self-extracting native libraries
# This should be in your .csproj already, but ensure it is:
# <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>

echo "Building for Linux ARM64..."
dotnet publish -c Release -r linux-arm64 --self-contained -o ../binaries/linux-arm64

# Go back to the root directory if desired
cd ../
