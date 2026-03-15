using System.Reflection;
using Autofac;

namespace ACS.Core.Workflow;

public class BizProcessContext
{
    public string Command { get; private set; }
    public Tuple<Type, object, bool> BizProcessJob { get; private set; }
    public bool AsyncMode { get; private set; }
    public List<string> TypeNames { get; private set; }
    public object[] Arguments { get; private set; }
    public int JobResult { get; private set; }
    private Dictionary<string, Tuple<Type, object>> commandJobList;
    public ILifetimeScope LifetimeScope { get; set; }

    public BizProcessContext(string command, Tuple<Type, object, bool> bizContext, object[] args, bool isAsyncMode = false)
    {
        Command = command;
        BizProcessJob = bizContext;
        Arguments = args;
        AsyncMode = isAsyncMode;
    }

    public void Init()
    {
    }

    public Dictionary<string, Tuple<Type, object>> CommandJobList
    {
        get { return commandJobList; }
        set { commandJobList = value; }
    }

    public bool Execute()
    {
        Type _type = BizProcessJob.Item1;
        object _obj = BizProcessJob.Item2;
        bool _asyncMode = BizProcessJob.Item3;
        eJobResult jobResult = eJobResult.ASYNCJOB;

        if (BizProcessJob == null) return false;

        if (_asyncMode || AsyncMode)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((o) =>
            {
                try
                {
                    PropertyInfo pi = _type.GetProperty("LifetimeScope");
                    MethodInfo mi = _type.GetMethod("Execute");
                    object[] param = { Arguments };
                    pi.SetValue(_obj, LifetimeScope);
                    jobResult = (eJobResult)mi.Invoke(_obj, param);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    return;
                }
            }));

            return true;
        }
        else
        {
            try
            {
                PropertyInfo pi = _type.GetProperty("LifetimeScope");
                MethodInfo mi = _type.GetMethod("Execute");
                object[] param = { Arguments };
                pi.SetValue(_obj, LifetimeScope);
                jobResult = (eJobResult)mi.Invoke(_obj, param);
                if (jobResult == eJobResult.SUCCESS) return true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return false;
            }
        }

        return false;
    }
}
