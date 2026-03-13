using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using ACS.Framework.Application.Model;
using ACS.Framework.Logging;
using ACS.Communication;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Model;

namespace ACS.Application
{
    public class AfterContextInitializedEx : AfterContextInitialized
    {
        public new Logger logger = Logger.GetLogger(typeof(AfterContextInitializedEx));

        public override void AfterContextInitComplete(Executor executor, ILifetimeScope lifetimeScope)
        {
            Framework.Application.Model.Application application = null;
            logger.logManager = lifetimeScope.Resolve<ILogManager>();

            if (executor.Type.Equals(TYPE_TS))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                SetReloadableToApplicationControlManager(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                logger.Fatal("succeeded in starting TS server");

                InvokeStartWorkflow(lifetimeScope, application, MESSAGENAME_COMMON_START_TS);
            }
            else if(executor.Type.Equals(TYPE_EI))
            {
                StartLogPropertyWatchDog(lifetimeScope, executor, false);

                SetLifetimeScopeWorkflowManager(lifetimeScope);

                SetLifetimeScopeToApplicationControlManager(lifetimeScope);

                SetReloadableToApplicationControlManager(lifetimeScope, executor);

                LoadSocket(lifetimeScope, executor);

                SynchronizeCache(lifetimeScope);

                StartSocket(lifetimeScope, executor);

                StartMsb(lifetimeScope, executor);

                application = CreateOrUpdateApplication(lifetimeScope, executor);

                //logger.well("succeeded in starting server", true);
                logger.Fatal("succeeded in starting ES server");

                InvokeStartWorkflow(lifetimeScope, application, "COMMON-START-EI");
            }
            else
            {
                string message = "please check process type, it should be {trans or ei}.";
                throw new NotSupportedException(message);
            }
        }

    
        protected override void StartSocket(ILifetimeScope lifetimeScope, Executor executor)
        {
            IEnumerable<IControllable> controllables = lifetimeScope.Resolve<IEnumerable<IControllable>>();
        }

        private void LoadSocket(ILifetimeScope lifetimeScope, Executor executor)
        {
            NioInterfaceManager nioInterfaceMangager = lifetimeScope.Resolve<NioInterfaceManager>();
            nioInterfaceMangager.Load(executor.Id);
        }

    }
}
