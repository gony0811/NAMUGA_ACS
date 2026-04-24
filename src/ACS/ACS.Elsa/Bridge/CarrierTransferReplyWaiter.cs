using System.Collections.Concurrent;
using System.Threading.Tasks;
using ACS.Core.Logging;

namespace ACS.Elsa.Bridge
{
    /// <summary>
    /// RAIL-CARRIERTRANSFER 전송 후 RAIL-CARRIERTRANSFERREPLY 응답을 대기하기 위한 정적 동기화 유틸.
    ///
    /// Trans 워크플로우 스레드(SCHEDULE-QUEUEJOB)에서 RegisterWait + WaitForReply로 블로킹 대기하고,
    /// RabbitMQ 리스너 스레드(RAIL-CARRIERTRANSFERREPLY 워크플로우)에서 SignalReply로 시그널.
    /// ConcurrentDictionary + TaskCompletionSource로 스레드 간 안전하게 동기화.
    /// </summary>
    public static class CarrierTransferReplyWaiter
    {
        private static readonly Logger logger = Logger.GetLogger("CARRIER_TRANSFER_REPLY_WAITER");
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _waiters = new();

        /// <summary>
        /// commandId에 대한 응답 대기를 등록.
        /// 기존 등록이 있으면 취소 후 새로 등록.
        /// </summary>
        public static void RegisterWait(string commandId)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (_waiters.TryRemove(commandId, out var oldTcs))
            {
                oldTcs.TrySetCanceled();
            }

            _waiters[commandId] = tcs;
            logger.Debug($"RegisterWait: commandId={commandId}");
        }

        /// <summary>
        /// commandId에 대한 응답 도착을 시그널.
        /// RAIL-CARRIERTRANSFERREPLY 워크플로우에서 호출.
        /// </summary>
        public static void SignalReply(string commandId, string resultCode)
        {
            if (_waiters.TryRemove(commandId, out var tcs))
            {
                tcs.TrySetResult(resultCode);
                logger.Info($"SignalReply: commandId={commandId}, resultCode={resultCode}");
            }
            else
            {
                logger.Warn($"SignalReply: 대기 중인 waiter 없음 - commandId={commandId}");
            }
        }

        /// <summary>
        /// commandId에 대한 응답을 지정된 시간만큼 블로킹 대기.
        /// </summary>
        /// <returns>(success, resultCode) - 타임아웃 시 success=false</returns>
        public static (bool success, string resultCode) WaitForReply(string commandId, int timeoutMs)
        {
            if (!_waiters.TryGetValue(commandId, out var tcs))
            {
                logger.Warn($"WaitForReply: 등록된 waiter 없음 - commandId={commandId}");
                return (false, null);
            }

            try
            {
                bool completed = tcs.Task.Wait(timeoutMs);
                _waiters.TryRemove(commandId, out _);

                if (completed)
                {
                    logger.Info($"WaitForReply: 응답 수신 - commandId={commandId}, resultCode={tcs.Task.Result}");
                    return (true, tcs.Task.Result);
                }
                else
                {
                    logger.Warn($"WaitForReply: 타임아웃 ({timeoutMs}ms) - commandId={commandId}");
                    return (false, null);
                }
            }
            catch
            {
                _waiters.TryRemove(commandId, out _);
                logger.Error($"WaitForReply: 예외 발생 - commandId={commandId}");
                return (false, null);
            }
        }
    }
}
