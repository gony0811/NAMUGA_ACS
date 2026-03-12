#!/bin/bash
# ACS Server Build & Run Script (macOS / .NET 8.0)
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/SDC.ACS.StartUp/bin/Debug/net8.0"

echo "=== Building ACS Server ==="
dotnet build "$SCRIPT_DIR/ACS.sln" -q

echo "=== Preparing runtime files ==="
# Copy config files
rm -rf "$OUTPUT_DIR/config"
cp -r "$SCRIPT_DIR/Bin/Config" "$OUTPUT_DIR/config"

# Fix log4net configs for macOS (no Kernel32.dll for ColoredConsoleAppender)
find "$OUTPUT_DIR/config" -name "*.xml" | while read f; do
  python3 -c "
import re
with open('$f','r') as fh: c = fh.read()
c = re.sub(r'log4net\.Appender\.ColoredConsoleAppender', 'log4net.Appender.ConsoleAppender', c)
c = re.sub(r'\s*<mapping>.*?</mapping>', '', c, flags=re.DOTALL)
with open('$f','w') as fh: fh.write(c)
" 2>/dev/null
done

# Copy Process directory (BizFileRepository dynamic assembly loading)
mkdir -p "$OUTPUT_DIR/Process"
cp "$SCRIPT_DIR/Bin/Process/SDC.ACS.Biz.Config.xml" "$OUTPUT_DIR/Process/"
cp "$OUTPUT_DIR/SDC.ACS.Biz.dll" "$OUTPUT_DIR/Process/" 2>/dev/null || true

echo "=== Starting ACS Server ==="
cd "$OUTPUT_DIR"
exec ./ES01_P
