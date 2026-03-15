using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ACS.Core.DependencyInjection
{
    /// <summary>
    /// 스레드 세이프한 이벤트 발행/구독 구현체.
    /// Spring.NET의 ApplicationContext.PublishEvent / IApplicationEventListener를 대체.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers
            = new ConcurrentDictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public void Publish<TEvent>(TEvent @event)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                List<Delegate> snapshot;
                lock (_lock)
                {
                    snapshot = new List<Delegate>(handlers);
                }
                foreach (var handler in snapshot)
                {
                    ((Action<TEvent>)handler)(@event);
                }
            }
        }

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
            lock (_lock)
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                lock (_lock)
                {
                    handlers.Remove(handler);
                }
            }
        }
    }
}
