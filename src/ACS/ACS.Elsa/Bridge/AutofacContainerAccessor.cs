using Autofac;

namespace ACS.Elsa.Bridge
{
    /// <summary>
    /// Autofac ILifetimeScope를 Elsa의 IServiceProvider 영역으로 전달하는 브릿지.
    ///
    /// 문제:
    ///   Elsa 3는 IServiceCollection → BuildServiceProvider()로 자체 IServiceProvider를 구축.
    ///   Autofac 컨테이너와 완전히 분리되어 있어 Elsa Activity에서 Autofac 서비스를 resolve할 수 없음.
    ///
    /// 해결:
    ///   1. ElsaModule에서 이 클래스의 인스턴스를 Elsa IServiceCollection에 등록
    ///   2. Autofac 컨테이너 빌드 후 Container 프로퍼티 설정
    ///   3. Activity에서 context.GetService&lt;AutofacContainerAccessor&gt;()로 Autofac 서비스 접근
    /// </summary>
    public class AutofacContainerAccessor
    {
        /// <summary>
        /// Autofac 루트 컨테이너. Executor에서 Build() 후 설정됨.
        /// </summary>
        public ILifetimeScope Container { get; set; }

        /// <summary>
        /// Autofac 컨테이너에서 서비스를 resolve.
        /// </summary>
        public T Resolve<T>() where T : class
        {
            return Container?.Resolve<T>();
        }

        /// <summary>
        /// Autofac 컨테이너에서 서비스를 resolve (실패 시 null).
        /// </summary>
        public T ResolveOptional<T>() where T : class
        {
            return Container?.ResolveOptional<T>();
        }

        /// <summary>
        /// Autofac 컨테이너에서 이름으로 서비스를 resolve.
        /// 예: ResolveNamed&lt;IMessageAgent&gt;("HostAgentSender")
        /// </summary>
        public T ResolveNamed<T>(string name) where T : class
        {
            return Container?.ResolveNamed<T>(name);
        }

        /// <summary>
        /// Autofac 컨테이너에서 Type으로 서비스를 resolve.
        /// </summary>
        public object Resolve(System.Type serviceType)
        {
            return Container?.Resolve(serviceType);
        }
    }
}
