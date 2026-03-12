#!/bin/bash

# Generate detailed audit report using the earlier view output
echo "=== COMPLETE HBM.XML AUDIT - ALL 43 MAPPING FILES ==="
echo ""

# Use grep with multiline to capture class names properly
declare -a files=(
"SDC.ACS.Database/Database/Communication/Nio/Nio.hbm.xml"
"SDC.ACS.Database/Database/Communication/Secs/Secs.hbm.xml"
"SDC.ACS.Database/Database/Framework/Material/CarrierEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Logging/LogMessage.hbm.xml"
"SDC.ACS.Database/Database/Framework/Logging/LargeLogMessage.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/VehicleCrossWaitHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/MismatchAndFlyHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/VehicleHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/AlarmTimeHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/TransportCommandHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/NioHistory.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/AlarmReportHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/VehicleSearchPathHistory.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/HeartBeatFailHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/VehicleBatteryHistoryEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/History/TruncateParameterEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/StationViewEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/NodeEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/WaitPointViewEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/LinkEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/LinkViewEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/CurrentInterSectionInfoEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/LocationViewEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/StationEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Path/InterSectionControlEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Transfer/TransportCommandRequestEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Transfer/UiCommand.hbm.xml"
"SDC.ACS.Database/Database/Framework/Transfer/TransportCommandEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Transfer/UiTransport.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/ZoneEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/VehicleIdleEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/OrderPairNodeEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/VehicleEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/SpecialConfig.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/VehicleCrossWaitEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/LinkZoneEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/Inform.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/VehicleOrderEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/BayEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Resource/LocationEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Application/Application.hbm.xml"
"SDC.ACS.Database/Database/Framework/Alarm/AlarmSpecEx.hbm.xml"
"SDC.ACS.Database/Database/Framework/Application/UiApplicationManager.hbm.xml"
"SDC.ACS.Database/Database/Framework/Alarm/AlarmEx.hbm.xml"
)

num=0
for file in "${files[@]}"; do
    num=$((num+1))
    if [ ! -f "$file" ]; then
        echo "$num. ❌ FILE NOT FOUND: $file"
        continue
    fi
    
    # Extract class name from filename (remove .hbm.xml)
    class_name=$(basename "$file" .hbm.xml)
    
    # Get assembly and namespace from grep
    assembly=$(grep 'assembly = "' "$file" | head -1 | sed 's/.*assembly = "\([^"]*\)".*/\1/')
    namespace=$(grep 'namespace = "' "$file" | head -1 | sed 's/.*namespace = "\([^"]*\)".*/\1/')
    
    # Extract actual class name from XML
    actual_class=$(grep '<class' "$file" | head -1 | sed 's/.*name="\([^"]*\)".*/\1/')
    
    echo "$num. File: $file"
    echo "   Assembly: $assembly"
    echo "   Namespace: $namespace"
    echo "   Class name in XML: $actual_class"
    
    # Check if class exists
    if [ -n "$actual_class" ]; then
        found=$(find . -name "${actual_class}.cs" 2>/dev/null | grep -E "(Framework|Communication)" | head -1)
        if [ -n "$found" ]; then
            echo "   ✅ Status: CLASS EXISTS"
        else
            echo "   ❌ Status: CLASS NOT FOUND"
        fi
    else
        echo "   ⚠️ Status: Could not extract class name from XML"
    fi
    echo ""
done
