using System.Xml;
using Autofac;

namespace ACS.Core.Workflow;

public class WorkflowManagerImpl : IWorkflowManager, IDisposable
{
    private List<string> skipProcessList = new List<string>();
    public List<string> SkipProcessList { get { return skipProcessList; } set { skipProcessList = value; } }
    public BizFileRepository BizFileRepository { get; set; }
    public BizProcessManager BizProcessManager { get; set; }
    public Thread WorkerThread;
    private bool internalIsParallel = false;
    public ILifetimeScope LifetimeScope { get; set; }

    public WorkflowManagerImpl()
    {
    }

    public void Init()
    {
    }

    public void Start()
    {
    }

    public void Stop()
    {
        BizProcessManager.Terminate = true;
    }

    public bool Execute(string workflowName, object paramObject)
    {
        if (paramObject == null)
            throw new ArgumentNullException("object argument", "object should not be null");

        object[] args = { paramObject };
        return Execute(workflowName, args, internalIsParallel);
    }

    public bool Execute(string workflowName, object paramObject, bool isParallel)
    {
        if (paramObject == null)
            throw new ArgumentNullException("object argument", "object should not be null");

        object[] args = { paramObject };
        return Execute(workflowName, args, isParallel);
    }

    public bool Execute(string workflowName, XmlDocument document)
    {
        return Execute(workflowName, document, internalIsParallel);
    }

    public bool Execute(string workflowName, XmlDocument document, bool isParallel)
    {
        if (document == null)
            throw new ArgumentNullException("document argument", "object should not be null");

        object[] args = { document };
        return Execute(workflowName, args, isParallel);
    }

    public bool Execute(string workflowName, object[] args)
    {
        return Execute(workflowName, args, internalIsParallel);
    }

    public bool Execute(string workflowName, object[] args, bool isParallel)
    {
        if (skipProcess(workflowName))
            return true;

        if (BizFileRepository == null)
            return false;

        try
        {
            BizProcessContext bizProcessContext = new BizProcessContext(workflowName, BizFileRepository.InstanceList[workflowName], args, isParallel);
            bizProcessContext.LifetimeScope = this.BizProcessManager.LifetimeScope;
            return bizProcessContext.Execute();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Execute(string transactionId, string workflowName, XmlDocument document)
    {
        return Execute(transactionId, workflowName, document, internalIsParallel);
    }

    public bool Execute(string transactionId, string workflowName, XmlDocument document, bool isParallel)
    {
        if (document == null)
            throw new ArgumentNullException("document argument", "object should not be null");

        object[] args = { document };
        return Execute(transactionId, workflowName, args, isParallel);
    }

    public bool Execute(string transactionId, string workflowName, object paramObject)
    {
        if (paramObject == null)
            throw new ArgumentNullException("object argument", "object should not be null");

        object[] args = { paramObject };
        return Execute(transactionId, workflowName, args, internalIsParallel);
    }

    public bool Execute(string transactionId, string workflowName, object paramObject, bool isParallel)
    {
        if (paramObject == null)
            throw new ArgumentNullException("object argument", "object should not be null");

        object[] args = { paramObject };
        return Execute(transactionId, workflowName, args, isParallel);
    }

    public bool Execute(string transactionId, string workflowName, object[] paramArrayOfObject, bool isParallel)
    {
        return Execute(transactionId, workflowName, paramArrayOfObject, isParallel, false);
    }

    public bool Execute(string transactionId, string workflowName, object[] args, bool isParallel, bool isFirstWorkflow)
    {
        if (skipProcess(workflowName))
            return true;

        try
        {
            BizProcessContext bizprocess = new BizProcessContext(workflowName, BizFileRepository.InstanceList[workflowName], args, isParallel);
            bizprocess.LifetimeScope = BizProcessManager.LifetimeScope;
            return bizprocess.Execute();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool skipProcess(string workflowName)
    {
        return this.SkipProcessList.Contains(workflowName);
    }

    public void Reload()
    {
        this.BizFileRepository.Reload();
    }

    public bool SkipWorkflow(string workflowName)
    {
        return this.skipProcessList.Contains(workflowName);
    }

    public void Dispose()
    {
    }
}
