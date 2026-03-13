using System;
using System.Collections.Generic;

namespace ACS.Framework.DependencyInjection
{
    /// <summary>
    /// Spring.NET의 IApplicationContext.GetObject() / GetObjectsOfType()를 대체하는 서비스 로케이터.
    /// 직접적인 생성자 주입이 불가능한 레거시 코드에서 과도기적으로 사용.
    /// 새 코드에서는 생성자 주입을 권장.
    /// </summary>
    public interface IServiceLocator
    {
        T Resolve<T>() where T : class;
        T ResolveNamed<T>(string name) where T : class;
        object Resolve(Type serviceType);
        IEnumerable<T> ResolveAll<T>() where T : class;
    }
}
