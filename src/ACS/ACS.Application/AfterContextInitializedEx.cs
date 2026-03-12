using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spring.Context;
using ACS.Framework.Application.Model;
using ACS.Framework.Logging;
using ACS.Communication;
using ACS.Communication.Socket;
using log4net;
using ACS.Communication.Socket.Model;

namespace ACS.Application
{
    public class AfterContextInitializedEx : AfterContextInitialized
    {
        public new Logger logger = Logger.GetLogger(typeof(AfterContextInitializedEx));

        public override void AfterContextInitComplete(Executor executor, IApplicationContext applicationContext)
        {
            Framework.Application.Model.Application application = null;
            logger.logManager = (ILogManager)applicationContext.GetObject("LogManager");

            if (executor.Type.Equals(TYPE_TS))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                SetReloadableToApplicationControlManager(applicationContext, executor);

                SynchronizeCache(applicationContext);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                logger.Fatal("succeeded in starting TS server");

                InvokeStartWorkflow(applicationContext, application, MESSAGENAME_COMMON_START_TS);
            }
            else if(executor.Type.Equals(TYPE_EI))
            {
                StartLogPropertyWatchDog(applicationContext, executor, false);

                SetApplicationContextWorkflowManager(applicationContext);

                SetApplicationContextToApplicationControlManager(applicationContext);

                SetReloadableToApplicationControlManager(applicationContext, executor);

                LoadSocket(applicationContext, executor);

                SynchronizeCache(applicationContext);

                StartSocket(applicationContext, executor);

                StartMsb(applicationContext, executor);

                application = CreateOrUpdateApplication(applicationContext, executor);

                //logger.well("succeeded in starting server", true);
                logger.Fatal("succeeded in starting ES server");

                InvokeStartWorkflow(applicationContext, application, "COMMON-START-EI");
            }
            else
            {
                string message = "please check process type, it should be {trans or ei}.";
                throw new NotSupportedException(message);
            }
        }

    
        protected override void StartSocket(IApplicationContext applicationContext, Executor executor)
        {
            IDictionary controllables = applicationContext.GetObjectsOfType(typeof(IControllable));

            //foreach (object controllableObj in controllables)
            //{
            //    IControllable controllable = (IControllable)controllableObj;
            //    controllable.Start();
            //}

            //IApplicationContext parentApplicationContext = applicationContext.ParentContext;

            //IDictionary parentControllables = parentApplicationContext.GetObjectsOfType(typeof(IControllable));

            //foreach (object controllableObj in parentControllables)
            //{
            //    //continue;

            //    IControllable controllable = (IControllable)controllableObj;
            //    controllable.Start();
            //}

            //dynamic VehicleInterfaceService = applicationContext.GetObject("VehicleInterfaceService");
            //ArrayList nioes = (ArrayList)VehicleInterfaceService.GetNioes();

            //foreach (Nio nio in nioes)
            //{
            //   var task = Task.Run(() => VehicleInterfaceService.StartNio(nio));
            //}

        }

        private void LoadSocket(IApplicationContext applicationContext, Executor executor)
        {
            NioInterfaceManager nioInterfaceMangager = (NioInterfaceManager)applicationContext.GetObject("NioInterfaceManager");
            nioInterfaceMangager.Load(executor.Id);
        }

    }
}
