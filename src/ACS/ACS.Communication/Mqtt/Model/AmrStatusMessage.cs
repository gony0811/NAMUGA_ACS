using System;
using System.Text.Json.Serialization;

namespace ACS.Communication.Mqtt.Model
{
    /// <summary>
    /// AMR이 발행하는 상태 메시지 (amr/{id}/status 토픽)
    /// mqtt_interface.md 스펙에 따른 JSON 구조
    /// </summary>
    public class AmrStatusMessage
    {
        /// <summary>MqttInterfaceManager에서 토픽 파싱 후 세팅</summary>
        [JsonIgnore]
        public string VehicleId { get; set; }

        /// <summary>로봇 동작 상태 (Off, Running, Paused)</summary>
        public string RobotState { get; set; }

        /// <summary>에러 코드 (0 = 정상)</summary>
        public int ErrorCode { get; set; }

        /// <summary>로봇 현재 위치</summary>
        public AmrPose Pose { get; set; }

        /// <summary>맵 상태 (0~100%)</summary>
        public float MapStatusPercent { get; set; }

        /// <summary>주행 상태 (WaitingForArrival, Moving)</summary>
        public string NavigationStatus { get; set; }

        /// <summary>배터리 상태</summary>
        public AmrBattery Battery { get; set; }
    }

    /// <summary>
    /// 로봇 위치 정보
    /// </summary>
    public class AmrPose
    {
        /// <summary>X 좌표 (meters)</summary>
        public float X { get; set; }

        /// <summary>Y 좌표 (meters)</summary>
        public float Y { get; set; }

        /// <summary>각도 (radian)</summary>
        public float Angle { get; set; }
    }

    /// <summary>
    /// 배터리 상태 정보
    /// </summary>
    public class AmrBattery
    {
        /// <summary>배터리 잔량 (%)</summary>
        public float VoltagePercent { get; set; }

        /// <summary>배터리 전류 S (A)</summary>
        public float CurrentS { get; set; }

        /// <summary>배터리 전류 A (A, 부호 있음)</summary>
        public float CurrentA { get; set; }

        /// <summary>배터리 온도 (°C)</summary>
        public float TemperatureCelsius { get; set; }

        /// <summary>충전 상태 (Charging, FullyCharged)</summary>
        public string ChargingState { get; set; }
    }
}
