using System;
using Autofac;
using ACS.Framework.Base.Interface;

namespace ACS.Application.Modules
{
    /// <summary>
    /// EF Core DbContext 및 PersistentDao 등록 모듈.
    /// Spring.NET의 NHibernate SessionFactory + HibernateDaoImpl 등록을 대체.
    /// AcsDbContext와 EfCorePersistentDao는 ACS.Database 프로젝트에서 구현.
    /// </summary>
    public class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // EF Core DbContext 등록 (NHibernate SessionFactory 대체)
            builder.RegisterType<ACS.Database.AcsDbContext>()
                .AsSelf()
                .InstancePerLifetimeScope();

            // EF Core 기반 PersistentDao 등록 (HibernateDaoImpl 대체)
            builder.RegisterType<ACS.Database.EfCorePersistentDao>()
                .As<IPersistentDao>()
                .InstancePerLifetimeScope();

            // DB 스키마 생성 (NHibernate hbm2ddl.auto 대체) -> Executor.Start() 로 이동
            // builder.RegisterBuildCallback(scope =>
            // {
            //    var dbContext = scope.Resolve<ACS.Database.AcsDbContext>();
            //    dbContext.Database.EnsureCreated();
            // });
        }
    }
}
