using System;
using System.Text.Json;
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

        /// <summary>로봇 동작 상태</summary>
        public AmrState State { get; set; }

        /// <summary>로봇 현재 위치</summary>
        public AmrPose Pose { get; set; }

        /// <summary>에러 정보</summary>
        public AmrError Error { get; set; }

        /// <summary>배터리 상태</summary>
        public AmrBattery Battery { get; set; }

        /// <summary>비정상 상황 보고</summary>
        public AmrAbnormal Abnormal { get; set; }
    }

    /// <summary>
    /// 로봇 동작 상태 정보
    /// </summary>
    public class AmrState
    {
        /// <summary>운행 상태: "Run" / "Stop"</summary>
        public string RunState { get; set; }

        /// <summary>적재 상태: "Full" / "Empty"</summary>
        public string FullState { get; set; }

        /// <summary>작업 상태: Idle/Moving/Docking/Jog</summary>
        public string WorkState { get; set; }

        /// <summary>현재 설정된 목적지 노드 ID</summary>
        public string VehicleDestNode { get; set; }
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
    /// 에러 정보
    /// </summary>
    public class AmrError
    {
        /// <summary>에러 코드 (0 = 정상)</summary>
        public int Code { get; set; }

        /// <summary>에러 메시지</summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 배터리 상태 정보
    /// </summary>
    public class AmrBattery
    {
        /// <summary>배터리 잔량 (%)</summary>
        public float LevelPercent { get; set; }

        /// <summary>배터리 전압 (V)</summary>
        public float Voltage { get; set; }

        /// <summary>배터리 전류 (A, 부호 있음)</summary>
        public float Current { get; set; }

        /// <summary>배터리 온도 (°C)</summary>
        public float TemperatureCelsius { get; set; }

        /// <summary>충전 상태: "Charging" / "Discharging"</summary>
        public string ChargingState { get; set; }
    }

    /// <summary>
    /// 비정상 상황 보고
    /// </summary>
    public class AmrAbnormal
    {
        /// <summary>비정상 유형 (예: CHARGING_FAIL)</summary>
        public string Type { get; set; }

        /// <summary>발생 노드 ID</summary>
        public string Node { get; set; }

        /// <summary>발생 시각</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// JSON에서 문자열 또는 숫자를 모두 string으로 읽는 컨버터.
    /// AMR이 workState를 숫자(1) 또는 문자열("Idle")로 보낼 수 있으므로 양쪽 모두 처리.
    /// </summary>
    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number when reader.TryGetInt64(out var l) => l.ToString(),
                JsonTokenType.Number => reader.GetDouble().ToString(),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.Null => null,
                _ => reader.GetString()
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
