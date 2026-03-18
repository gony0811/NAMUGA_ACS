using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using ACS.Core.Logging;

namespace ACS.Elsa.Workflows
{
    /// <summary>
    /// JSON 파일 기반 워크플로우 로더.
    ///
    /// Workflows/Json/ 디렉토리에서 *.json 파일을 읽어
    /// Elsa 워크플로우 정의로 변환하여 등록한다.
    ///
    /// JSON 형식:
    /// {
    ///   "definitionId": "WORKFLOW_NAME",
    ///   "name": "워크플로우 표시명",
    ///   "description": "워크플로우 설명",
    ///   "steps": [
    ///     {
    ///       "type": "Activity.FullTypeName 또는 단축명",
    ///       "name": "스텝 표시명",
    ///       "inputs": { "PropertyName": "value" }
    ///     }
    ///   ]
    /// }
    ///
    /// 이 로더는 JSON 정의를 파싱하여 C# WorkflowBase 래퍼로 변환한다.
    /// Elsa Studio에서 디자인한 JSON을 별도 DB 없이 파일 시스템에서 로드하는 용도.
    /// </summary>
    public static class JsonWorkflowLoader
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_JSON_LOADER");

        /// <summary>
        /// 지정된 디렉토리에서 JSON 워크플로우 정의 파일을 로드.
        /// </summary>
        /// <returns>로드된 워크플로우 정의 목록 (definitionId → JsonWorkflowDefinition)</returns>
        public static Dictionary<string, JsonWorkflowDefinition> LoadFromDirectory(string directory = null)
        {
            var result = new Dictionary<string, JsonWorkflowDefinition>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(AppContext.BaseDirectory, "Workflows", "Json");
            }

            if (!Directory.Exists(directory))
            {
                logger.Info($"JSON 워크플로우 디렉토리 없음: {directory} — JSON 워크플로우 미사용.");
                return result;
            }

            var jsonFiles = Directory.GetFiles(directory, "*.json");
            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var definition = JsonSerializer.Deserialize<JsonWorkflowDefinition>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (definition != null && !string.IsNullOrEmpty(definition.DefinitionId))
                    {
                        result[definition.DefinitionId] = definition;
                        logger.Info($"JSON 워크플로우 로드: {definition.DefinitionId} (from {Path.GetFileName(filePath)})");
                    }
                    else
                    {
                        logger.Warn($"JSON 워크플로우 파일 무시 (definitionId 없음): {Path.GetFileName(filePath)}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"JSON 워크플로우 로드 실패: {Path.GetFileName(filePath)} — {ex.Message}", ex);
                }
            }

            logger.Info($"JSON 워크플로우 {result.Count}개 로드 완료.");
            return result;
        }
    }

    /// <summary>
    /// JSON 워크플로우 정의 모델.
    /// </summary>
    public class JsonWorkflowDefinition
    {
        public string DefinitionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<JsonWorkflowStep> Steps { get; set; } = new();
    }

    /// <summary>
    /// JSON 워크플로우 스텝 정의.
    /// </summary>
    public class JsonWorkflowStep
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Inputs { get; set; } = new();
    }

    /// <summary>
    /// JSON 정의를 기반으로 런타임에 생성되는 WorkflowBase 래퍼.
    /// ElsaWorkflowManagerBridge에서 일반 C# 워크플로우와 동일하게 사용 가능.
    /// </summary>
    public class JsonBackedWorkflow : WorkflowBase
    {
        private readonly JsonWorkflowDefinition _definition;

        public JsonBackedWorkflow(JsonWorkflowDefinition definition)
        {
            _definition = definition;
        }

        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = _definition.DefinitionId;
            builder.Name = _definition.Name ?? _definition.DefinitionId;
            builder.Description = _definition.Description ?? "";

            // JSON의 steps를 Sequence 내 활동들로 변환
            // 현재는 BizJobActivity/ServiceMethodActivity를 활용한 단순 파이프라인 지원
            var activities = new List<IActivity>();

            foreach (var step in _definition.Steps)
            {
                // ServiceMethodActivity로 변환
                if (step.Inputs.TryGetValue("serviceType", out var serviceTypeObj) &&
                    step.Inputs.TryGetValue("methodName", out var methodNameObj))
                {
                    activities.Add(new Activities.ServiceMethodActivity
                    {
                        ServiceTypeName = new(serviceTypeObj?.ToString() ?? ""),
                        MethodName = new(methodNameObj?.ToString() ?? "")
                    });
                }
                else
                {
                    // 기본: 로그 출력
                    activities.Add(new WriteLine($"[JSON Step] {step.Name ?? step.Type}"));
                }
            }

            if (activities.Count == 0)
            {
                activities.Add(new WriteLine($"JSON workflow '{_definition.DefinitionId}' executed (no steps defined)."));
            }

            builder.Root = new Sequence
            {
                Activities = activities
            };
        }
    }
}
