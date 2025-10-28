#!/bin/bash
# Build script for LLM Dialog Mod

echo "Building LLM Dialog Mod..."
cd src
dotnet build --configuration Release

if [ $? -eq 0 ]; then
    echo "Build successful!"
    echo "Mod files are ready in src/bin/Release/net6.0/"
else
    echo "Build failed!"
    exit 1
fi
