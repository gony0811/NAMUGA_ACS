using System;
using System.Collections.Generic;

namespace ACS.Framework.DependencyInjection
{
    /// <summary>
    /// Spring.NET의 IApplicationEventListener / PublishEvent를 대체하는 이벤트 발행/구독 인터페이스.
    /// </summary>
    public interface IEventAggregator
    {
        void Publish<TEvent>(TEvent @event);
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
    }
}
