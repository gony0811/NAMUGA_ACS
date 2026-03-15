using Autofac;

namespace ACS.App.Modules.Site
{
    /// <summary>
    /// SSM1D1F 사이트 모듈.
    /// V2와 유사한 설정. HeartBeat(control) 및 일부 매니저 구현이 다를 수 있음.
    /// </summary>
    public class Ssm1d1fSiteModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // SSM1D1F 사이트별 오버라이드
            // V2와 유사한 구성이므로 필요 시 V2와 동일한 오버라이드 적용.
        }
    }
}
