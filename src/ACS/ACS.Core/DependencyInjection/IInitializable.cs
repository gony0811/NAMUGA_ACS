namespace ACS.Core.DependencyInjection
{
    /// <summary>
    /// Spring.NET의 IInitializingObject.AfterPropertiesSet() 및 init-method를 대체하는 인터페이스.
    /// Autofac의 OnActivated 콜백으로 Init()을 호출하여 사용.
    /// </summary>
    public interface IInitializable
    {
        void Init();
    }
}
