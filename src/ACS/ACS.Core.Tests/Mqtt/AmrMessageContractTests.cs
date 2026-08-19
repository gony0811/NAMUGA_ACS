using System.Text.Json;
using ACS.Communication.Mqtt.Model;
using Xunit;

namespace ACS.Core.Tests.Mqtt
{
    /// <summary>
    /// ACS↔AMR MQTT 페이로드 계약 스냅샷 (docs/ACS-AMR_mqtt_exchange.md v0.3, docs/mqtt_interface.md).
    ///  - moveCmd 출력은 v0.2 와 동일해야 한다 (jobId/type 미직렬화).
    ///  - actionCmd 는 type/jobId, cancelCmd 는 jobId 를 실어야 한다.
    ///  - reply 는 jobId/step/stepName/carrierSlot 선택 필드를 읽을 수 있어야 하고, 없어도 파싱된다.
    /// </summary>
    public class AmrMessageContractTests
    {
        [Fact]
        public void MoveCmd_Serialization_HasNoJobIdOrType()
        {
            var cmd = new AmrCommandMessage
            {
                CmdId = "EX001", Command = "moveCmd", NodeId = "N1011", Port = "LEFT",
                JobType = "UNLOAD", PortType = "BUFFER", Model = "CF203W", AmrSlot = 1
            };
            string json = JsonSerializer.Serialize(cmd);
            Assert.DoesNotContain("\"jobId\"", json);
            Assert.DoesNotContain("\"type\"", json);
            Assert.Contains("\"command\":\"moveCmd\"", json);
            Assert.Contains("\"amrSlot\":1", json);
        }

        [Fact]
        public void ActionCmd_Serialization_HasTypeAndJobId()
        {
            var cmd = new AmrCommandMessage
            {
                CmdId = "EX001", Command = "actionCmd", NodeId = "N2002", Port = "RIGHT",
                JobType = "EXCHANGE", Type = "UNLOAD", JobId = "EX001", Model = "CF203W", AmrSlot = 3
            };
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(cmd));
            var root = doc.RootElement;
            Assert.Equal("actionCmd", root.GetProperty("command").GetString());
            Assert.Equal("UNLOAD", root.GetProperty("type").GetString());
            Assert.Equal("EXCHANGE", root.GetProperty("jobType").GetString());
            Assert.Equal("EX001", root.GetProperty("jobId").GetString());
            Assert.Equal(3, root.GetProperty("amrSlot").GetInt32());
        }

        [Fact]
        public void CancelCmd_Serialization_HasJobId()
        {
            var cmd = new AmrCommandMessage { CmdId = "EX001", Command = "cancelCmd", JobId = "EX001" };
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(cmd));
            var root = doc.RootElement;
            Assert.Equal("cancelCmd", root.GetProperty("command").GetString());
            Assert.Equal("EX001", root.GetProperty("jobId").GetString());
            Assert.False(root.TryGetProperty("type", out _));
        }

        [Fact]
        public void Reply_Deserialization_ReadsOptionalFields()
        {
            string json = "{\"cmdId\":\"EX001\",\"status\":\"STEP_COMPLETE\",\"resultCode\":0,\"message\":\"ok\"," +
                          "\"jobId\":\"EX001\",\"step\":30,\"stepName\":\"UNLOAD_OLD\",\"carrierSlot\":3," +
                          "\"timestamp\":\"2026-08-19T10:00:00Z\"}";
            var reply = JsonSerializer.Deserialize<AmrReplyMessage>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(reply);
            Assert.Equal("STEP_COMPLETE", reply.Status);
            Assert.Equal("EX001", reply.JobId);
            Assert.Equal(30, reply.Step);
            Assert.Equal("UNLOAD_OLD", reply.StepName);
            Assert.Equal(3, reply.CarrierSlot);
        }

        [Fact]
        public void Reply_Deserialization_LegacyPayload_OptionalFieldsNull()
        {
            // v0.2 형식(moveCmd_Reply): 선택 필드 없음
            string json = "{\"cmdId\":\"EX001\",\"status\":\"COMPLETED\",\"resultCode\":0,\"message\":\"Success\"," +
                          "\"timestamp\":\"2026-08-19T10:00:00Z\"}";
            var reply = JsonSerializer.Deserialize<AmrReplyMessage>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(reply);
            Assert.Null(reply.JobId);
            Assert.Null(reply.Step);
            Assert.Null(reply.StepName);
            Assert.Null(reply.CarrierSlot);
            Assert.Null(reply.JobType);
        }
    }
}
