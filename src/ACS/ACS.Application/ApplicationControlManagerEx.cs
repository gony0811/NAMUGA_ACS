using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using ACS.Communication;
using ACS.Communication.Msb;
using ACS.Communication.Socket;
using ACS.Framework.Base;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Reload;
using ACS.Framework.Message.Model.Control;
using ACS.Workflow;
using Spring.Context;
using Spring.Context.Support;


namespace ACS.Application
{

    //public class ApplicationControlManagerExImplement : AbstractManager, IApplicationControlManager
    //{
    //    public long SleepIntervalWhenStop { get; set; }
    //    public long RunningBizProcessCheckIntervalWhenStop { get; set; }

    //    public int runningBizProcessCheckRetryCountWhenStop { get; set; }
    //    public IApplicationContext ApplicationContext { get; set; }
    //    public IReloadableApplicationContextAware ReloadableApplicationContextAware { get; set; }
    //    public string ReloadableDirectory { get; set; }
    //    public string[] ReloadableAssemblyDefinitions { get; set; }

    //    public IWorkflowManager WorkflowManager { get; set; }
    //    public IApplicationManager ApplicationManager { get; set; }

    //    private NioInterfaceManager nioInterfaceManager;

    //    public virtual NioInterfaceManager NioInterfaceManager
    //    {
    //        get { return this.nioInterfaceManager; }
    //        set { this.nioInterfaceManager = value; }
    //    }

    //    public bool Control(ControlMessage paramControlMessage)
    //    {
    //        bool result = false;
    //        string messageName = paramControlMessage.MessageName;

    //        if (messageName.Equals("CONTROL-HEARTBEAT"))
    //        {
    //            result = HeartBeat(paramControlMessage);
    //        }
    //        else if (messageName.Equals("CONTROL-RELOADSERVICE"))
    //        {
    //            result = ReloadService(paramControlMessage);
    //        }
    //        else if (messageName.Equals("CONTROL-RELOADWORKFLOW"))
    //        {
    //            result = ReloadWorkflow(paramControlMessage);
    //        }
    //        else if (messageName.Equals("CONTROL-STOP"))
    //        {
    //            result = Stop(paramControlMessage);
    //        }
    //        else
    //        {
    //            //logger.DebugFormat("Please check messageName : {0}", paramControlMessage.MessageName);
    //        }

    //        return result;
    //    }

    //    public bool HeartBeat(ControlMessage paramControlMessage)
    //    {
    //        return true;
    //    }

    //    public bool InvokeHeartBeat()
    //    {
    //        return true;
    //    }

    //    public bool ReloadWorkflow(ControlMessage paramControlMessage)
    //    {
    //        return InvokeReloadWorkflow();
    //    }

    //    public bool InvokeReloadWorkflow()
    //    {
    //        if (this.WorkflowManager != null)
    //        {
    //            this.WorkflowManager.Reload();
    //        }

    //        return true;
    //    }

    //    /**
    //     * @deprecated
    //    **/
    //    public bool ReloadService(ControlMessage controlMessage)
    //    {
    //        return InvokeReloadService();
    //    }


    //    /**
    //    * @deprecated
    //    */
    //    public bool InvokeReloadService()
    //    {
    //        if (this.ReloadableApplicationContextAware != null)
    //        {
    //            AbstractApplicationContext currentApplicationContext = (AbstractApplicationContext)this.ApplicationContext;

    //            IApplicationContext parentApplicationContext = currentApplicationContext.ParentContext;

    //            //CustomClassLoader serviceClassLoader = new CustomClassLoader(this.ReloadableDirectory);

    //            AbstractApplicationContext reloadableApplicationContext = new XmlApplicationContext(parentApplicationContext, ReloadableAssemblyDefinitions);
    //            //reloadableApplicationContext.
    //        }

    //        return false;
    //    }

    //    /**
    //    * @deprecated
    //    */
    //    public bool Stop(ControlMessage controlMessage)
    //    {
    //        return InvokeStop(controlMessage.ApplicationType, controlMessage.ApplicationName);
    //    }

    //    /**
    //    * @deprecated
    //    */
    //    public bool InvokeStop(string applicationType, string applicationName)
    //    {
    //        long startTime = DateTime.UtcNow.Millisecond;

    //        //DisposeMsbControllables();


    //        return false;
    //    }

    //    /**
    //    * @deprecated
    //    */

    //    public bool Gc(ControlMessage paramControlMessage)
    //    {
    //        return InvokeGc();
    //    }

    //    public bool InvokeGc()
    //    {
    //        System.GC.Collect();

    //        return true;
    //    }

    //    /**
    //    * @deprecated
    //    */
    //    public bool RefreshCache(ControlMessage paramControlMessage)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    /**
    //    * @deprecated
    //    */
    //    public bool InvokeRefreshCache()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    /**
    //     * @deprecated
    //     */
    //    protected void DisposeMsbControllables()
    //    {
    //        IApplicationContext currentApplicationContext = this.ApplicationContext;

    //        IDictionary currentControllables = currentApplicationContext.GetObjectsOfType(typeof(IControllable));
    //        foreach (IControllable controllable in currentControllables)
    //        {
    //            if (controllable is IMsbControllable)
    //            {
    //                controllable.Stop();
    //            }
    //        }
    //    }


    //}


}
