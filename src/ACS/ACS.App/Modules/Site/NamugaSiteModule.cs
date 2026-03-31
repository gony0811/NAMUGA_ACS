using System;
using Autofac;
using ACS.Core.Base;
using ACS.Core.Resource;
using ACS.Core.Path;
using ACS.Core.Cache;

namespace ACS.App.Modules.Site
{
    /// <summary>
    /// NAMUGA 사이트 모듈.
    /// ResourceManagerExsImplement, CacheManagerExImplement 등 NAMUGA 전용 구현체 사용.
    /// 프로세스 모듈보다 나중에 등록되므로 Autofac의 "마지막 등록 우선" 규칙에 의해 오버라이드됨.
    /// </summary>
    public class NamugaSiteModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // NAMUGA 전용: ResourceManagerExsImplement으로 오버라이드
            // ACS.Manager type resolved by name to avoid circular project reference
            var resourceMgrType = Type.GetType("ACS.Manager.ResourceManagerExsImplement, ACS.Manager");
            if (resourceMgrType != null)
                builder.RegisterType(resourceMgrType)
                    .As<IResourceManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired()
                    .OnActivated(e => ((AbstractManager)e.Instance).Init());

            // NAMUGA 전용: CacheManagerExImplement으로 오버라이드
            var cacheMgrType = Type.GetType("ACS.Manager.CacheManagerExImplement, ACS.Manager");
            if (cacheMgrType != null)
                builder.RegisterType(cacheMgrType)
                    .As<ICacheManagerEx>()
                    .SingleInstance()
                    .PropertiesAutowired();
        }
    }
}
