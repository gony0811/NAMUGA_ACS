using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Spring.Context;
using System.Threading;

namespace ACS.Workflow
{
    public class WorkflowManagerImpl : IWorkflowManager, IDisposable
    {

        private List<string> skipProcessList = new List<string>();
        //private BizProcessContext bizProcessContext;
        //private BizFileRepository BizFileRepository;
        //private BizProcessManager BizProcessManager;
        public List<string> SkipProcessList { get { return skipProcessList; } set { skipProcessList = value; } }
        //public BizProcessContext BizProcessContext { get  { return bizProcessContext;}  set { bizProcessContext = value;} }
        public BizFileRepository BizFileRepository { get; set; }// { return this.BizFileRepository; } set { this.BizFileRepository = value; } }
        public BizProcessManager BizProcessManager { get; set; } //{ return this.BizProcessManager; } set { this.BizProcessManager = value; } }
        public Thread WorkerThread;
        private bool internalIsParallel = false;
        public IApplicationContext ApplicationContext { get; set; }
        public WorkflowManagerImpl()
        {

        }

        public void Init()
        {
        }

        public void Start()
        {
            //BizProcessManager.Run(200);
        }

        public void Stop()
        {
            BizProcessManager.Terminate = true;
        }
        public bool Execute(string workflowName, object paramObject)
        {
            if (paramObject == null)
            {
                throw new ArgumentNullException("object argument", "object should not be null");
            }

            Object[] args = { paramObject };

            return Execute(workflowName, args, internalIsParallel);
        }

        public bool Execute(string workflowName, object paramObject, bool isParallel)
        {
            if (paramObject == null)
            {
                throw new ArgumentNullException("object argument", "object should not be null");
            }

            Object[] args = { paramObject };
            return Execute(workflowName, args, isParallel);
        }

        public bool Execute(string workflowName, XmlDocument document)
        {
            return Execute(workflowName, document, internalIsParallel);
        }

        public bool Execute(string workflowName, XmlDocument document, bool isParallel)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document argument", "object should not be null");
            }

            Object[] args = { document };
            return Execute(workflowName, args, isParallel);
        }

        public bool Execute(string workflowName, object[] args)
        {
            return Execute(workflowName, args, internalIsParallel);
        }

        public bool Execute(string workflowName, object[] args, bool isParallel)
        {
            if (skipProcess(workflowName))
            {
                return true;
            }
            if (BizFileRepository == null)
            {
                return false;
            }
            try
            {
                //bizProcessContext.CommandJobList = bizRepository.InstanceList;
                //
                //if (!bizProcessContext.CommandJobList.ContainsKey(workflowName))
                //{
                //    // LOG : Not exist workflow name
                //    return false;
                //}
                //return bizProcessContext.Execute(workflowName, args, !isParallel);

                BizProcessContext bizProcessContext = new BizProcessContext(workflowName, BizFileRepository.InstanceList[workflowName], args, isParallel);
                bizProcessContext.ApplicationContext = this.BizProcessManager.ApplicationContext;
                return bizProcessContext.Execute();
                //if (BizProcessManager.RequestExecuteJob(bizProcessContext) > 0) return true;
                //{
                //    // LOG : Not exist workflow name
                //    return false;
                //}


            }
            catch (Exception e)
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
            {
                throw new ArgumentNullException("document argument", "object should not be null");
            }

            Object[] args = { document };

            return Execute(transactionId, workflowName, args, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, object paramObject)
        {
            if (paramObject == null)
            {
                throw new ArgumentNullException("object argument", "object should not be null");
            }

            Object[] args = { paramObject };

            return Execute(transactionId, workflowName, args, internalIsParallel);
        }

        public bool Execute(string transactionId, string workflowName, object paramObject, bool isParallel)
        {
            if (paramObject == null)
            {
                throw new ArgumentNullException("object argument", "object should not be null");
            }
            Object[] args = { paramObject };

            return Execute(transactionId, workflowName, args, isParallel);
        }

        public bool Execute(string transactionId, string workflowName, object[] paramArrayOfObject, bool isParallel)
        {
            return Execute(transactionId, workflowName, paramArrayOfObject, isParallel, false);
        }

        public bool Execute(string transactionId, string workflowName, Object[] args, bool isParallel, bool isFirstWorkflow)
        {
            if (skipProcess(workflowName))
            {
                return true;
            }

            try
            {
                //bizProcessContext.CommandJobList = bizRepository.InstanceList;
                //if(!bizProcessContext.CommandJobList.ContainsKey(workflowName))

                BizProcessContext bizprocess = new BizProcessContext(workflowName, BizFileRepository.InstanceList[workflowName], args, isParallel);
                bizprocess.ApplicationContext = BizProcessManager.ApplicationContext;
                //if (BizProcessManager.RequestExecuteJob(bizprocess) > 0) return true;  
                //{
                //    // LOG : Not exist workflow name
                //    return false;
                //}
                return bizprocess.Execute();
                // return BizProcessContext.Execute(workflowName, args, !isParallel);

            }
            catch (Exception e)
            {

                return false;
            }
        }

        public bool skipProcess(string workflowName)
        {
            if (this.SkipProcessList.Contains(workflowName))
            {
                return true;
            }
            return false;
        }

        public void Reload()
        {
            this.BizFileRepository.Reload();
        }

        public bool SkipWorkflow(string workflowName)
        {
            if (this.skipProcessList.Contains(workflowName))
            {
                // skip process
                return true;
            }

            return false;
        }

        public void Dispose()
        {

        }
    }
}
