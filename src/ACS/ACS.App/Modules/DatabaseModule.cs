using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using ACS.Core.Base.Interface;

namespace ACS.App.Modules
{
    /// <summary>
    /// EF Core DbContext 및 PersistentDao 등록 모듈.
    /// </summary>
    public class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // EF Core DbContext 등록 — IConfiguration 주입
            builder.Register(c =>
                {
                    var config = c.Resolve<IConfiguration>();
                    return new ACS.Database.AcsDbContext(
                        new Microsoft.EntityFrameworkCore.DbContextOptions<ACS.Database.AcsDbContext>(),
                        config);
                })
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
