#!/bin/bash
# filepath: /workspaces/BKB-Tool-WASM/restart.sh

echo "🛑 Stopping existing dotnet processes..."
pkill -9 dotnet 2>/dev/null || true

echo "⏳ Waiting for processes to terminate..."
sleep 1

# Check if port 5000 is still in use and force kill if necessary
if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
    echo "⚠️  Port 5000 still in use, force killing process..."
    kill -9 $(lsof -t -i:5000) 2>/dev/null || true
    sleep 1
fi

# Final check
if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
    echo "❌ Error: Port 5000 is still in use!"
    echo "Please manually kill the process with: kill -9 \$(lsof -t -i:5000)"
    exit 1
fi

echo "✅ Port 5000 is free"
echo "🚀 Starting application..."
cd /workspaces/BKB-Tool-WASM
dotnet run --project BKBToolClient.csproj