#!/bin/bash

# Set publish folder relative to the script location
publishFolder="$(dirname "$0")/Build"

# Create the publish folder if it does not exist
mkdir -p "$publishFolder"

# Publish the project
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:DebugType=none -o "$publishFolder"

# Copy configuration file
cp "VirtuosoMIDICompanion.ini" "$publishFolder"

# Remove Windows specific libraries if they exist
windowsFiles=("*.dll" "*.exe" "*.pdb" "*.config")

for pattern in "${windowsFiles[@]}"; do
    find "$publishFolder" -name "$pattern" -exec rm -f {} \;
done

echo "Published project to $publishFolder"