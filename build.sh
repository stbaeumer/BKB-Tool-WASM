#!/bin/bash
set -e

echo "Step 1: Installing wasm-tools workload..."
dotnet workload install wasm-tools

echo "Step 2: Removing old dist folder..."
rm -rf dist

echo "Step 3: Publishing Release build..."
dotnet publish BKBToolClient.csproj -c Release -o dist

echo "Build completed successfully!"
echo "Checking dist/wwwroot/_framework folder..."
ls -la dist/wwwroot/_framework/ | head -20
