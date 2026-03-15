using Autofac;

namespace ACS.App.Modules.Site
{
    /// <summary>
    /// V2 사이트 모듈.
    /// V2는 HeartBeat(control) 및 일부 매니저 구현이 다름.
    /// 프로세스 모듈보다 나중에 등록되므로 Autofac의 "마지막 등록 우선" 규칙에 의해 오버라이드됨.
    /// </summary>
    public class V2SiteModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // V2 사이트별 오버라이드
            // HeartBeat 관련 설정은 ControlModule에서 IControlServerManager로 등록된
            // ControlServerManagerImplement의 프로퍼티로 설정됨.
            // 필요 시 V2 전용 구현체를 여기서 오버라이드 등록.
        }
    }
}
