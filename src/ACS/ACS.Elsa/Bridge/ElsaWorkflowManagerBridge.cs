using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using ACS.Core.Logging;
using ACS.Core.Workflow;

namespace ACS.Elsa.Bridge
{
    /// <summary>
    /// Hybrid IWorkflowManager: routes commands to Elsa or legacy WorkflowManagerImpl
    /// based on elsa-migration.json configuration.
    ///
    /// Commands listed in "ElsaCommands" are run via IWorkflowRunner (동기 실행).
    /// All other commands fall through to the original WorkflowManagerImpl.
    /// This enables gradual migration without breaking existing functionality.
    /// </summary>
    public class ElsaWorkflowManagerBridge : IWorkflowManager
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ElsaWorkflowManagerBridge));

        private readonly WorkflowManagerImpl _legacyManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private HashSet<string> _elsaCommands;
        private readonly string _configPath;

        /// <summary>
        /// DefinitionId → IWorkflow 인스턴스 매핑.
        /// WorkflowBase의 Build()에서 설정한 DefinitionId로 워크플로우를 찾음.
        /// </summary>
        private readonly Dictionary<string, IWorkflow> _workflowRegistry = new(StringComparer.OrdinalIgnoreCase);

        // 1초 간격 텔레메트리/스케줄 워크플로우의 시작/완료 로그는 Debug 로 강등.
        // 운영: Serilog MinimumLevel=Information(기본 침묵), 디버깅: Debug(전체 흐름 가시화).
        private static readonly HashSet<string> _telemetryWorkflowNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "SCHEDULE-CHECKVEHICLES", "RAIL-VEHICLEUPDATE", "RAIL-VEHICLEALARM",
            "CONTROL-HEARTBEAT", "CONTROL-STARTHEARTBEAT", "RAIL-VEHICLEHEARTBEAT"
        };
        private static bool IsTelemetryWorkflow(string name) =>
            !string.IsNullOrEmpty(name) && _telemetryWorkflowNames.Contains(name);

        public ElsaWorkflowManagerBridge(
            WorkflowManagerImpl legacyManager,
            IServiceScopeFactory scopeFactory)
        {
            _legacyManager = legacyManager;
            _scopeFactory = scopeFactory;

            // Config file next to the running executable
            _configPath = Path.Combine(AppContext.BaseDirectory, "elsa-migration.json");
            LoadConfig();
            ScanWorkflows();
        }

        private void LoadConfig()
        {
            _elsaCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(_configPath))
            {
                logger.Info($"elsa-migration.json not found at {_configPath} — all commands routed to legacy.");
                return;
            }

            try
            {
                var json = File.ReadAllText(_configPath);
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ElsaCommands", out var cmds) && cmds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in cmds.EnumerateArray())
                    {
                        var cmd = item.GetString();
                        if (!string.IsNullOrEmpty(cmd))
                            _elsaCommands.Add(cmd);
                    }
                }

                logger.Info($"Elsa migration config loaded: {_elsaCommands.Count} command(s) routed to Elsa.");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load elsa-migration.json: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ACS.Elsa 어셈블리에서 WorkflowBase 구현체를 스캔하여 등록.
        /// 또한 JSON 워크플로우 파일도 로드하여 레지스트리에 추가.
        /// </summary>
        private void ScanWorkflows()
        {
            // 1. ACS.Elsa 어셈블리에서 C# 워크플로우 스캔
            ScanWorkflowsFromAssembly(typeof(ElsaWorkflowManagerBridge).Assembly, "ACS.Elsa");

            // 2. JSON 워크플로우 로드
            try
            {
                var jsonWorkflows = Workflows.JsonWorkflowLoader.LoadFromDirectory();
                foreach (var kvp in jsonWorkflows)
                {
                    var jsonWorkflow = new Workflows.JsonBackedWorkflow(kvp.Value);
                    _workflowRegistry[kvp.Key] = jsonWorkflow;
                    logger.Info($"JSON workflow registered: {kvp.Key}");
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to load JSON workflows: {ex.Message}");
            }

            logger.Info($"Total {_workflowRegistry.Count} workflow(s) registered.");
        }

        private void ScanWorkflowsFromAssembly(Assembly asm, string assemblyLabel)
        {
            try
            {
                var workflowBaseType = typeof(WorkflowBase);

                foreach (var type in asm.GetExportedTypes())
                {
                    if (type.IsAbstract || !workflowBaseType.IsAssignableFrom(type)) continue;

                    try
                    {
                        var instance = (WorkflowBase)Activator.CreateInstance(type);
                        _workflowRegistry[type.Name] = instance;
                        logger.Info($"Workflow registered: {type.Name} (from {assemblyLabel})");
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to instantiate workflow {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to scan workflows from {assemblyLabel}: {ex.Message}");
            }
        }

        private bool IsElsaCommand(string workflowName)
        {
            return _elsaCommands.Contains(workflowName);
        }

        /// <summary>
        /// IWorkflowRunner.RunAsync로 워크플로우를 동기(inline) 실행.
        /// DispatchAsync와 달리 현재 스레드에서 실행되므로 디버깅/브레이크포인트 가능.
        /// </summary>
        private bool RunElsaWorkflow(string workflowName, object[] args)
        {
            try
            {
                bool isTelemetry = IsTelemetryWorkflow(workflowName);
                if (isTelemetry)
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Elsa running '{workflowName}' with {args?.Length ?? 0} argument(s)...");
                }
                else
                {
                    logger.Info($"Elsa running '{workflowName}' with {args?.Length ?? 0} argument(s)...");
                }

                // DefinitionId 또는 클래스명으로 워크플로우 찾기
                IWorkflow workflow = null;

                // 1. elsa-migration.json의 ElsaCommands에 매핑된 워크플로우 클래스명 찾기
                //    예: "MOVECMD" → HostMoveCmdWorkflow
                //    규칙: "Host{CommandName}Workflow" 패턴으로 검색
                var candidates = new[]
                {
                    $"Host{workflowName}Workflow",          // HostMoveCmdWorkflow
                    $"{workflowName}Workflow",              // MOVECMDWorkflow
                    workflowName,                           // 직접 클래스명
                    ConvertToPascalClassName(workflowName)  // SCHEDULE-QUEUEJOB → ScheduleQueuejobWorkflow
                };

                foreach (var candidate in candidates)
                {
                    if (_workflowRegistry.TryGetValue(candidate, out workflow))
                        break;
                }

                if (workflow == null)
                {
                    logger.Warn($"Elsa workflow not found for '{workflowName}'. " +
                                $"Searched: {string.Join(", ", candidates)}. " +
                                $"Available: {string.Join(", ", _workflowRegistry.Keys)}");
                    return false;
                }

                var input = new Dictionary<string, object>
                {
                    ["CommandName"] = workflowName,
                    ["Arguments"] = args
                };

                var options = new RunWorkflowOptions
                {
                    Input = input
                };

                // Scoped 의존성(IServiceProvider, DB 컨텍스트 등)이 살아 있는 새 scope에서 실행.
                // 필드에 IWorkflowRunner를 캐싱하면 첫 scope dispose 후 ObjectDisposedException 발생.
                using var scope = _scopeFactory.CreateScope();
                var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
                var result = workflowRunner.RunAsync(workflow, options)
                    .GetAwaiter().GetResult();

                if (isTelemetry)
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug($"Elsa workflow '{workflowName}' completed. Status={result.WorkflowState.Status}");
                }
                else
                {
                    logger.Info($"Elsa workflow '{workflowName}' completed. Status={result.WorkflowState.Status}");
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Elsa workflow '{workflowName}' failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 메시지명을 PascalCase 클래스명으로 변환.
        /// 예: "SCHEDULE-QUEUEJOB" → "ScheduleQueuejobWorkflow"
        /// _workflowRegistry가 OrdinalIgnoreCase이므로 정확한 casing 불필요.
        /// </summary>
        private static string ConvertToPascalClassName(string messageName)
        {
            var parts = messageName.Split('-', '_');
            var pascal = string.Concat(parts.Select(p =>
                p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1).ToLower() : ""));
            return pascal + "Workflow";
        }

        // --- IWorkflowManager implementation ---

        public bool Execute(string workflowName, object paramObject)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new[] { paramObject });
            return _legacyManager.Execute(workflowName, paramObject);
        }

        public bool Execute(string workflowName, object paramObject, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new[] { paramObject });
            return _legacyManager.Execute(workflowName, paramObject, isParallel);
        }

        public bool Execute(string workflowName, XmlDocument document)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new object[] { document });
            return _legacyManager.Execute(workflowName, document);
        }

        public bool Execute(string workflowName, XmlDocument document, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new object[] { document });
            return _legacyManager.Execute(workflowName, document, isParallel);
        }

        public bool Execute(string workflowName, object[] args)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, args);
            return _legacyManager.Execute(workflowName, args);
        }

        public bool Execute(string workflowName, object[] args, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, args);
            return _legacyManager.Execute(workflowName, args, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, XmlDocument document)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new object[] { document });
            return _legacyManager.Execute(transactionId, workflowName, document);
        }

        public bool Execute(string transactionId, string workflowName, XmlDocument document, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new object[] { document });
            return _legacyManager.Execute(transactionId, workflowName, document, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, object paramObject)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new[] { paramObject });
            return _legacyManager.Execute(transactionId, workflowName, paramObject);
        }

        public bool Execute(string transactionId, string workflowName, object paramObject, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, new[] { paramObject });
            return _legacyManager.Execute(transactionId, workflowName, paramObject, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, object[] args, bool isParallel)
        {
            if (IsElsaCommand(workflowName))
                return RunElsaWorkflow(workflowName, args);
            return _legacyManager.Execute(transactionId, workflowName, args, isParallel);
        }

        public void Reload()
        {
            _legacyManager.Reload();
            LoadConfig(); // also reload Elsa migration config
        }

        public bool SkipWorkflow(string workflowName)
        {
            return _legacyManager.SkipWorkflow(workflowName);
        }

        public void Start()
        {
            _legacyManager.Start();
        }

        public void Stop()
        {
            _legacyManager.Stop();
        }
    }
}
