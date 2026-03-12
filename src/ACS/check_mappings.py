import xml.etree.ElementTree as ET
import os
import glob

# Parse all .hbm.xml files
mapping_files = glob.glob("SDC.ACS.Database/Database/**/*.hbm.xml", recursive=True)
mapping_files.sort()

results = []

for hbm_file in mapping_files:
    try:
        tree = ET.parse(hbm_file)
        root = tree.getroot()
        
        # Extract namespace from the root element
        ns = {'h': 'urn:nhibernate-mapping-2.2'}
        
        # Get attributes from hibernate-mapping element
        assembly = root.get('assembly', '')
        namespace = root.get('namespace', '')
        
        # Find all class elements
        for class_elem in root.findall('.//class'):
            class_name = class_elem.get('name', '')
            
            results.append({
                'file': hbm_file,
                'assembly': assembly,
                'namespace': namespace,
                'class': class_name,
            })
    except Exception as e:
        print(f"Error parsing {hbm_file}: {e}")

# Output results
for i, r in enumerate(results, 1):
    print(f"{i}. File: {r['file']}")
    print(f"   Assembly: {r['assembly']}")
    print(f"   Namespace: {r['namespace']}")
    print(f"   Class: {r['class']}")
    print()

print(f"\nTotal: {len(results)} mapping entries")
