using Autofac;

namespace ACS.App.Modules.Site
{
    /// <summary>
    /// V1 사이트 모듈.
    /// V1은 기본(default) 구현을 사용하므로 별도 오버라이드 없음.
    /// </summary>
    public class V1SiteModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // V1 사이트는 CoreModule 및 프로세스 모듈의 기본 등록을 그대로 사용.
            // 사이트별 오버라이드가 필요 없음.
        }
    }
}
