using System;
using System.Collections.Generic;
using Autofac;

namespace ACS.Framework.DependencyInjection
{
    /// <summary>
    /// Autofac ILifetimeScope를 래핑하는 IServiceLocator 구현체.
    /// Spring.NET의 IApplicationContext.GetObject() 패턴을 레거시 코드에서 계속 사용 가능하게 함.
    /// </summary>
    public class AutofacServiceLocator : IServiceLocator
    {
        private readonly ILifetimeScope _scope;

        public AutofacServiceLocator(ILifetimeScope scope)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public T Resolve<T>() where T : class
        {
            return _scope.Resolve<T>();
        }

        public T ResolveNamed<T>(string name) where T : class
        {
            return _scope.ResolveNamed<T>(name);
        }

        public object Resolve(Type serviceType)
        {
            return _scope.Resolve(serviceType);
        }

        public IEnumerable<T> ResolveAll<T>() where T : class
        {
            return _scope.Resolve<IEnumerable<T>>();
        }
    }
}
