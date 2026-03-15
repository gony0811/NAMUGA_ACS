using System.Collections.Concurrent;
using Autofac;

namespace ACS.Core.Workflow;

public class BizProcessManager : IDisposable
{
    private BlockingCollection<BizProcessContext> reservedProcess = new BlockingCollection<BizProcessContext>();
    private BizProcessContext currentRunningProcess;
    public BizProcessContext CurrentRunningProcess { get { return currentRunningProcess; } }
    public bool Terminate { get; set; }
    public ILifetimeScope LifetimeScope { get; set; }

    public BizProcessManager()
    {
        Terminate = false;
    }

    public void Init()
    {
    }

    public void Run(int milisecPeriod)
    {
        try
        {
            while (!Terminate)
            {
                currentRunningProcess = reservedProcess.Take();
                currentRunningProcess.LifetimeScope = LifetimeScope;

                if (currentRunningProcess.AsyncMode)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                    {
                        currentRunningProcess.Execute();
                    }));
                }
                else
                {
                    currentRunningProcess.Execute();
                }

                Thread.Sleep(milisecPeriod);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
        }
    }

    public void Dispose()
    {
        Terminate = true;
    }
}
