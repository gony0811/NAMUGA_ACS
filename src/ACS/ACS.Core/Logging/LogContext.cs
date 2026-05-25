using System;
using System.Threading;

namespace ACS.Core.Logging
{
    /// <summary>
    /// 한 메시지 처리 흐름에 흐르는 로그 컨텍스트 값. 빈 값은 null로 둔다.
    /// </summary>
    public sealed class LogContextData
    {
        public string TransactionId { get; set; }
        public string MessageName { get; set; }
        public string CommunicationMessageName { get; set; }
        public string TransportCommandId { get; set; }
        public string CarrierName { get; set; }
        public string MachineName { get; set; }
        public string UnitName { get; set; }
    }

    /// <summary>
    /// AsyncLocal 기반 주변(ambient) 로그 컨텍스트.
    /// 메시지 처리 진입점에서 <see cref="Push"/>로 깔아두면, 같은 실행 흐름(동기/await 전파 포함)에서
    /// 발생하는 모든 로그가 빈 필드를 이 값으로 자동 보강할 수 있다.
    ///
    /// 주의: AsyncLocal은 값을 읽는 "그 스레드/실행 흐름"에서만 유효하다. 따라서 보강은 로그를
    /// 생성하는 비즈니스 스레드(LogManagerImpl.CreateLogMessage)에서 수행해야 하며,
    /// 백그라운드 큐 소비자 스레드에서 읽으면 안 된다.
    /// </summary>
    public static class LogContext
    {
        private static readonly AsyncLocal<LogContextData> _current = new AsyncLocal<LogContextData>();

        /// <summary>현재 실행 흐름의 로그 컨텍스트. 없으면 null.</summary>
        public static LogContextData Current => _current.Value;

        /// <summary>
        /// 컨텍스트를 설정하고, Dispose 시 이전 값으로 복원하는 스코프를 반환한다(중첩 안전).
        /// </summary>
        public static IDisposable Push(LogContextData data)
        {
            var previous = _current.Value;
            _current.Value = data;
            return new Scope(previous);
        }

        private sealed class Scope : IDisposable
        {
            private readonly LogContextData _previous;
            private bool _disposed;

            public Scope(LogContextData previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _current.Value = _previous;
            }
        }
    }
}
