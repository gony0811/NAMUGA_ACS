#!/bin/bash

echo "=== COMPLETE HBM.XML AUDIT REPORT ==="
echo ""

find SDC.ACS.Database/Database -name "*.hbm.xml" -type f | sort | nl | while read -r number filepath; do
    # Use sed to extract values more reliably
    assembly=$(sed -n 's/.*assembly[[:space:]]*=[[:space:]]*"\([^"]*\)".*/\1/p' "$filepath" | head -1)
    namespace=$(sed -n 's/.*namespace[[:space:]]*=[[:space:]]*"\([^"]*\)".*/\1/p' "$filepath" | head -1)
    classname=$(sed -n 's/.*<class[^>]*name="\([^"]*\)".*/\1/p' "$filepath" | head -1)
    
    echo "$number. File: $filepath"
    [ -n "$assembly" ] && echo "   Assembly: $assembly" || echo "   Assembly: (empty)"
    [ -n "$namespace" ] && echo "   Namespace: $namespace" || echo "   Namespace: (empty)"
    [ -n "$classname" ] && echo "   Class: $classname" || echo "   Class: (empty)"
    
    # Check if class file exists
    if [ -n "$classname" ]; then
        # Look for the class file
        found=$(find . -name "${classname}.cs" 2>/dev/null | grep -E "(SDC\.ACS\.Framework|SDC\.ACS\.Communication|SDC\.ACS\.Extension)" | head -1)
        if [ -n "$found" ]; then
            echo "   Status: ✅ FOUND"
            echo "   Location: $found"
        else
            echo "   Status: ❌ NOT FOUND"
        fi
    else
        echo "   Status: ⚠️ No class name"
    fi
    echo ""
done
