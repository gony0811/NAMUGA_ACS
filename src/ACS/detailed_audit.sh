#!/bin/bash

echo "================================"
echo "HBM.XML FILES AUDIT - COMPLETE REPORT"
echo "================================"
echo ""

# Process each HBM file
find SDC.ACS.Database/Database -name "*.hbm.xml" -type f | sort | while read hbm_file; do
    echo "File: $hbm_file"
    
    # Extract assembly
    assembly=$(grep -o 'assembly = "[^"]*"' "$hbm_file" | head -1 | cut -d'"' -f2)
    
    # Extract namespace
    namespace=$(grep -o 'namespace = "[^"]*"' "$hbm_file" | head -1 | cut -d'"' -f2)
    
    # Extract class name
    class_name=$(grep -o '<class[^>]*name="[^"]*"' "$hbm_file" | grep -o 'name="[^"]*"' | head -1 | cut -d'"' -f2)
    
    echo "  Assembly: $assembly"
    echo "  Namespace: $namespace"
    echo "  Class: $class_name"
    
    # Build the expected path
    if [ -z "$assembly" ]; then
        assembly="SDC.ACS.Framework"
    fi
    
    # Convert assembly to path
    project_path=$(echo "$assembly" | tr '.' '/')
    
    if [ -z "$namespace" ]; then
        # No namespace - look for class at project root
        search_pattern="*/${project_path}/**/${class_name}.cs"
        class_exists=$(find . -path "*${project_path}*" -name "${class_name}.cs" 2>/dev/null | head -1)
    else
        # Convert namespace to path structure (after assembly)
        ns_without_assembly=${namespace#${assembly}.}
        ns_path=$(echo "$ns_without_assembly" | tr '.' '/')
        search_pattern="*/${project_path}/${ns_path}/${class_name}.cs"
        class_exists=$(find . -path "*${project_path}*" -path "*${ns_path}*" -name "${class_name}.cs" 2>/dev/null | head -1)
    fi
    
    if [ -z "$class_exists" ]; then
        echo "  Status: ❌ CLASS NOT FOUND"
    else
        echo "  Status: ✅ FOUND at: $class_exists"
    fi
    echo ""
done
