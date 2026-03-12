#!/bin/bash

echo "=== COMPLETE HBM.XML CLASS EXISTENCE AUDIT REPORT ==="
echo ""
echo "Total Files: 43"
echo ""

# Manual mapping based on viewed files
declare -A mappings=(
    ["SDC.ACS.Database/Database/Communication/Nio/Nio.hbm.xml"]="Nio|SDC.ACS.Communication|SDC.ACS.Communication.Socket.Model"
    ["SDC.ACS.Database/Database/Communication/Secs/Secs.hbm.xml"]="Secs|SDC.ACS.Framework|"
    ["SDC.ACS.Database/Database/Framework/Material/CarrierEx.hbm.xml"]="CarrierEx|SDC.ACS.Framework|SDC.ACS.Framework.Material.Model"
    ["SDC.ACS.Database/Database/Framework/Logging/LogMessage.hbm.xml"]="LogMessage|SDC.ACS.Framework|SDC.ACS.Framework.Logging.Model"
    ["SDC.ACS.Database/Database/Framework/Logging/LargeLogMessage.hbm.xml"]="LargeLogMessage|SDC.ACS.Framework|"
    ["SDC.ACS.Database/Database/Framework/History/VehicleCrossWaitHistoryEx.hbm.xml"]="VehicleCrossWaitHistoryEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/MismatchAndFlyHistoryEx.hbm.xml"]="MismatchAndFlyHistoryEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/VehicleHistoryEx.hbm.xml"]="VehicleHistoryEx|SDC.ACS.Framework|SDC.ACS.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/AlarmTimeHistoryEx.hbm.xml"]="AlarmTimeHistoryEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/TransportCommandHistoryEx.hbm.xml"]="TransportCommandHistoryEx|SDC.ACS.Framework|SDC.ACS.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/NioHistory.hbm.xml"]="NioHistory|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/AlarmReportHistoryEx.hbm.xml"]="AlarmReportHistoryEx|SDC.ACS.Framework|SDC.ACS.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/VehicleSearchPathHistory.hbm.xml"]="VehicleSearchPathHistory|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/HeartBeatFailHistoryEx.hbm.xml"]="HeartBeatFailHistoryEx|SDC.ACS.Framework|SDC.ACS.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/VehicleBatteryHistoryEx.hbm.xml"]="VehicleBatteryHistoryEx|SDC.ACS.Framework|SDC.ACS.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/History/TruncateParameterEx.hbm.xml"]="TruncateParameterEx|SDC.ACS.Framework|SDC.ACS.Framework.History.Model"
    ["SDC.ACS.Database/Database/Framework/Path/StationViewEx.hbm.xml"]="StationViewEx|SDC.ACS.Framework|SDC.ACS.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/NodeEx.hbm.xml"]="NodeEx|SDC.ACS.Framework|SDC.ACS.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/WaitPointViewEx.hbm.xml"]="WaitPointViewEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/LinkEx.hbm.xml"]="LinkEx|SDC.ACS.Framework|SDC.ACS.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/LinkViewEx.hbm.xml"]="LinkViewEx|SDC.ACS.Framework|SDC.ACS.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/CurrentInterSectionInfoEx.hbm.xml"]="CurrentInterSectionInfoEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/LocationViewEx.hbm.xml"]="LocationViewEx|SDC.ACS.Framework|SDC.ACS.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/StationEx.hbm.xml"]="StationEx|SDC.ACS.Framework|SDC.ACS.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Path/InterSectionControlEx.hbm.xml"]="InterSectionControlEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Path.Model"
    ["SDC.ACS.Database/Database/Framework/Transfer/TransportCommandRequestEx.hbm.xml"]="TransportCommandRequestEx|SDC.ACS.Framework|SDC.ACS.Framework.Transfer.Model"
    ["SDC.ACS.Database/Database/Framework/Transfer/UiCommand.hbm.xml"]="UiCommand|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Transfer.Model"
    ["SDC.ACS.Database/Database/Framework/Transfer/TransportCommandEx.hbm.xml"]="TransportCommandEx|SDC.ACS.Framework|SDC.ACS.Framework.Transfer.Model"
    ["SDC.ACS.Database/Database/Framework/Transfer/UiTransport.hbm.xml"]="UiTransport|SDC.ACS.Framework|SDC.ACS.Framework.Transfer.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/ZoneEx.hbm.xml"]="ZoneEx|SDC.ACS.Framework|SDC.ACS.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/VehicleIdleEx.hbm.xml"]="VehicleIdleEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/OrderPairNodeEx.hbm.xml"]="OrderPairNodeEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/VehicleEx.hbm.xml"]="VehicleExs|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/SpecialConfig.hbm.xml"]="SpecialConfig|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/VehicleCrossWaitEx.hbm.xml"]="VehicleCrossWaitEx|SDC.ACS.Framework|SDC.ACS.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/LinkZoneEx.hbm.xml"]="LinkZoneEx|SDC.ACS.Framework|SDC.ACS.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/Inform.hbm.xml"]="Inform|SDC.ACS.Framework|SDC.ACS.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/VehicleOrderEx.hbm.xml"]="VehicleOrderEx|SDC.ACS.Extension.Framework|SDC.ACS.Extension.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/BayEx.hbm.xml"]="BayEx|SDC.ACS.Framework|SDC.ACS.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Resource/LocationEx.hbm.xml"]="LocationEx|SDC.ACS.Framework|SDC.ACS.Framework.Resource.Model"
    ["SDC.ACS.Database/Database/Framework/Application/Application.hbm.xml"]="Application|SDC.ACS.Framework|SDC.ACS.Framework.Application.Model"
    ["SDC.ACS.Database/Database/Framework/Alarm/AlarmSpecEx.hbm.xml"]="AlarmSpecEx|SDC.ACS.Framework|SDC.ACS.Framework.Alarm.Model"
    ["SDC.ACS.Database/Database/Framework/Application/UiApplicationManager.hbm.xml"]="UiApplicationManager|SDC.ACS.Framework|SDC.ACS.Framework.Application.Model"
    ["SDC.ACS.Database/Database/Framework/Alarm/AlarmEx.hbm.xml"]="AlarmEx|SDC.ACS.Framework|SDC.ACS.Framework.Alarm.Model"
)

num=0
for file in "${!mappings[@]}"; do
    num=$((num+1))
    IFS='|' read -r class assembly namespace <<< "${mappings[$file]}"
    
    echo "$num. File: $file"
    echo "   Assembly: $assembly"
    echo "   Namespace: $namespace"
    echo "   Class: $class"
    
    # Check if file exists
    found=$(find . -name "${class}.cs" 2>/dev/null | grep -E "(Framework|Communication)" | head -1)
    if [ -n "$found" ]; then
        echo "   ✅ Status: CLASS EXISTS"
        echo "   Location: $found"
    else
        echo "   ❌ Status: CLASS NOT FOUND"
    fi
    echo ""
done | sort
