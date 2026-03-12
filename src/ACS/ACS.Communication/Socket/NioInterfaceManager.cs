using ACS.Framework.Base;
using Spring.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message;
using ACS.Communication.Socket;
using ACS.Communication.Socket.Checker;
using ACS.Communication.Socket.Model;
using ACS.Workflow;
using NHibernate.Criterion;
using ACS.Framework.Resource.Model;
using ACS.Framework.Resource;
using ACS.Utility;
//using Client;

namespace ACS.Communication.Socket
{
    public class NioInterfaceManager : AbstractManager, IApplicationContextAware
    {
        private IDictionary nioInterfaces = new Hashtable();
        protected IMessageManagerEx messageManager;
        //protected CacheManager cacheManager;
        protected DuplicateChecker duplicateChecker;
        private IApplicationContext applicationContext;
        public IResourceManagerEx ResourceManager { get; set; } 
        public virtual IDictionary NioInterfaces
        {
            get { return this.nioInterfaces; }
            set { this.nioInterfaces = value; }
        }

        public virtual IMessageManagerEx MessageManager
        {
            get { return this.messageManager; }
        }

        //public virtual CacheManager CacheManager
        //{
        //    get
        //    {
        //        return this.cacheManager;
        //    }
        //    set
        //    {
        //        this.cacheManager = value;
        //    }
        //}

        public virtual DuplicateChecker DuplicateChecker
        {
            get { return this.duplicateChecker; }
            set { this.duplicateChecker = value; }
        }

        public virtual IApplicationContext ApplicationContext
        {
            get { return this.applicationContext; }
            set { this.applicationContext = value; }
        }
        public virtual void DisplayAll()
        {
            foreach (DictionaryEntry iterator in nioInterfaces)
            {
                SocketClient socketClient = (SocketClient)iterator.Value;
                logger.Info(socketClient.Nio);
            }
        }

        public virtual string GetNioInterfaceNames()
        {
            return this.nioInterfaces.Keys.ToString();
        }

        public int GetNioInterfaceCount()
        {
            return this.nioInterfaces.Count;
        }

        //public virtual SocketClient GetNioInterface(string name)
        public virtual object GetNioInterface(string name)
        {
            this.ResourceManager = (IResourceManagerEx)applicationContext.GetObject("ResourceManager");

            VehicleEx vehicle = this.ResourceManager.GetVehicle(name);
            SocketClient socketClient = (SocketClient)this.nioInterfaces[vehicle.NioId];
            return socketClient;
        }

        public virtual void Load(string applicationName)
        {
            IList nioes = this.PersistentDao.FindByAttribute(typeof(Nio), "ApplicationName", applicationName);
            //logger.fine("nio drivers will be loaded into the application{" + applicationName + "}, " + nioes);
            foreach (object iterator in nioes)
            {
                Nio nio = (Nio)iterator;
                LoadNioInterface(nio);

                ////190312 Validate IP
                //Nio nio = (Nio)iterator;
                
                //bool ipValidate = SystemUtility.ValidateIPv4(nio.RemoteIp);
                //if (ipValidate)
                //{
                //    LoadNioInterface(nio);
                //}
                //else
                //{
                //    //Error!!
                //    logger.Error("IPAddress Error: " + nio.RemoteIp);
                //}
            }
        }

        public virtual void Unload()
        {
            foreach (DictionaryEntry iterator in nioInterfaces)
            {
                UnloadNioInterface((string)iterator.Key);
            }
        }

        public virtual void LoadNioInterface(Nio nio)
        {
            SocketClient socketClient = (SocketClient)System.Activator.CreateInstance(Type.GetType(nio.InterfaceClassName));
            socketClient.Nio = nio;
            //191001 Socket Initialize 1Row 주석처리
            //socketClient.Initialize();

            try
            {
                IWorkflowManager workflowManager = (IWorkflowManager)this.applicationContext.GetObject(nio.WorkflowManagerName);

                socketClient.WorkflowManager = workflowManager;
                socketClient.DuplicateChecker = this.DuplicateChecker;
            }
            catch (Exception e)
            {
                logger.Warn("workflow is not used, configure it if you use workflow", e);
            }
            finally
            {

            }
            LoadNioInterface(nio.getName(), socketClient);
        }

        public void LoadNioInterface(String name, SocketClient socketClient)
        {
            if (this.nioInterfaces.Contains(name))
            {
                logger.Error("nio{" + name + "} already exists, older will be erased and newer is going to be added");
                SocketClient older = (SocketClient)this.nioInterfaces[name];
                if (older.IsOpen())
                {
                    older.Stop();
                }
                this.nioInterfaces.Remove(name);
            }

            if ((string.IsNullOrEmpty(socketClient.Nio.State)) || (!socketClient.Nio.State.Equals("LOADED")))
            {
                //logger.fine("nio{" + socketClient.getNio().getName() + "} state will be changed to {" + "LOADED" + "}");
                socketClient.Nio.State = "LOADED";
                UpdateNioState(socketClient.Nio);
            }
           
            socketClient.Initialize();
            this.nioInterfaces.Add(name, socketClient);
            //logger.fine("nio{" + name + "} was loaded, " + socketClient);

            DisplayAll();
        }

        public virtual void LoadNioInterface(String name)
        {
            Nio nio = (Nio)this.PersistentDao.FindByName(typeof(Nio), name);
        }

        public virtual void LoadNioInterface(String applicationName, String name)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(Nio));
            criteria.Add(Restrictions.Eq("ApplicationName", applicationName));
            criteria.Add(Restrictions.Eq("Name", name));

            IList nioes = this.PersistentDao.FindByCriteria(criteria);
            if (nioes.Count == 0)
            {
                logger.Error("nio{" + name + "} does not exist");
                return;
            }
            LoadNioInterface((Nio)nioes[0]);
        }

        public virtual void UnloadNioInterface(string name)
        {
            SocketClient socketClient = (SocketClient)this.nioInterfaces[name];

            this.nioInterfaces.Remove(name);

            if (socketClient != null)
            {
                if (socketClient.IsOpen())
                {
                    socketClient.Stop();
                }
                //logger.fine("nio{" + name + "} was unloaded, " + socketClient);
                if ((string.IsNullOrEmpty(socketClient.Nio.State)) || (!socketClient.Nio.State.Equals("UNLOADED")))
                {
                    //logger.fine("state will be changed to {UNLOADED}");
                    socketClient.Nio.State = "UNLOADED";
                    UpdateNioState(socketClient.Nio);
                }
            }
            else
            {
                //logger.fine("already unloaded, name{" + name + "}");
            }
            this.nioInterfaces.Remove(name);

            DisplayAll();
        }

        public void ReloadAll(string applicationName)
        {
            Unload();
            Load(applicationName);
        }

        public void ReloadNioInterface(string applicationName, string name)
        {
            UnloadNioInterface(name);
            LoadNioInterface(applicationName, name);
        }

        public void RestartAll()
        {
            StopAll();
            StartAll();
        }

        public void RestartNioInterface(string name)
        {
            StopNioInterface(name);

            StartNioInterface(name);
        }

        public virtual void StartAll()
        {
            foreach (DictionaryEntry iterator in nioInterfaces)
            {
                SocketClient socketClient = (SocketClient)iterator.Value;
                StartNioInterface(socketClient);
            }
        }

        public virtual int StartNioInterface(string name)
        {
            logger.Info("starting open nio{" + name + "}");
            return StartNioInterface((SocketClient)this.nioInterfaces[name]);
        }

        public int StartNioInterface(SocketClient socketClient)
        {
            int result = 0;
            if (socketClient != null)
            {
                if (!socketClient.IsOpen())
                {
                    if (!socketClient.Nio.State.Equals("CONNECTING"))
                    {
                        UpdateNioState(socketClient, "CONNECTING");
                        if (socketClient.Start())
                        {
                            UpdateNioState(socketClient, "CONNECTED");
                            result = 2;
                        }
                    }
                    else
                    {
                        //logger.fine("already connecting, " + socketClient.nio);
                        result = 1;
                    }
                }
                else
                {
                    //logger.fine("already opend, " + socketClient.nio);
                    result = 1;
                }
            }
            else
            {
                logger.Error("dose not exist nio");
            }
            return result;
        }

        public virtual void StopAll()
        {
            foreach (DictionaryEntry iterator in nioInterfaces)
            {
                StopNioInterface((SocketClient)iterator.Value);
            }
        }

        public virtual bool StopNioInterface(string name)
        {
            SocketClient socketClient = (SocketClient)this.nioInterfaces[name];
            return StopNioInterface(socketClient);
        }

        public bool StopNioInterface(SocketClient socketClient)
        {
            UpdateNioState(socketClient, "CLOSED");
            bool result = socketClient.Stop();
            UpdateNioState(socketClient, "CLOSED");
            return result;
        }

        public bool IsConnected(string name)
        {
            SocketClient socketClient = (SocketClient)this.nioInterfaces[name];
            return IsConnected(socketClient);
        }

        public bool IsConnected(SocketClient socketClient)
        {
            return socketClient.SessionOpened;
        }

        public void UpdateNioInterface(String name)
        {
            SocketClient socketClient = (SocketClient)this.nioInterfaces[name];
            Nio nio = GetNio(name, socketClient.Nio.ApplicationName);
            socketClient.Nio = nio;
        }

        public void AddNio(Nio nio)
        {
            this.PersistentDao.Save(nio);
        }

        public void UpdateNio(Nio nio)
        {
            this.PersistentDao.Update(nio);
        }

        public int UpdateNioState(Nio nio)
        {
            UpdateNioEditTime(nio);
            return this.PersistentDao.Update(typeof(Nio), "State", nio.State, nio.Id);
        }

        private int UpdateNioEditTime(Nio nio)
        {
            return this.PersistentDao.Update(typeof(Nio), "EditTime", DateTime.Now, nio.Id);
        }

        public int UpdateNioState(SocketClient socketClient, string state)
        {
            Nio nio = socketClient.Nio;
            if (string.IsNullOrEmpty(state))
            {
                //logger.fine("nio{" + nio.getName() + "}.state will be changed to {" + state + "}, " + nio);
                nio.State = state;
                return UpdateNioState(nio);
            }
            return 0;
        }


        public int UpdateNioState(string name, string applicationName, string state)
        {
            UpdateNioEditTime(name, applicationName);

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("Name", name);
            conditionAttributes.Add("ApplicationName", applicationName);

            return this.PersistentDao.UpdateByAttributes(typeof(Nio), "State", state, conditionAttributes);
        }

        private int UpdateNioEditTime(string name, string applicationName)
        {
            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("Name", name);
            conditionAttributes.Add("ApplicationName", applicationName);

            return this.PersistentDao.UpdateByAttributes(typeof(Nio), "EditTime",DateTime.Now, conditionAttributes);
        }

        public void RemoveNio(Nio nio)
        {
            this.PersistentDao.Delete(nio);
        }

        public IList GetNioes()
        {
            return this.PersistentDao.FindAll(typeof(Nio));
        }

        public int GetNioCount()
        {
            return this.PersistentDao.Count(DetachedCriteria.For(typeof(Nio)));
        }

        /**
        * @deprecated
        */
        public Nio GetNio(string name)
        {
            return (Nio)this.PersistentDao.FindByName(typeof(Nio), name, false);
        }

        public Nio GetNio(string name, string applicationName)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(Nio));
            criteria.Add(Restrictions.Eq("Name", name));
            criteria.Add(Restrictions.Eq("ApplicationName", applicationName));

            IList niolist = this.PersistentDao.FindByCriteria(criteria);
            if (niolist.Count > 0)
            {
                return (Nio)niolist[0];
            }
            logger.Error("nio{" + name + " - " + applicationName + "} does not exist in repository");
            return null;
        }

        public Nio GetNioByState(String name, String state)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(Nio));
            criteria.Add(Restrictions.Eq("Name", name));
            criteria.Add(Restrictions.Eq("State", state));

            IList niolist = this.PersistentDao.FindByCriteria(criteria);
            if (niolist.Count > 0)
            {
                return (Nio)niolist[0];
            }
            logger.Error("nio{" + name + " - " + state + "} does not exist in repository");
            return null;
        }

        public Nio GetNioByMachineName(string name, string machineName, string state)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Name", name);
            attributes.Add("MachineName", machineName);
            attributes.Add("State", state);

            IList niolist = this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
            if (niolist.Count > 0)
            {
                return (Nio)niolist[0];
            }
            logger.Error("nio{" + name + " - " + machineName + "} does not exist in repository");
            return null;
        }

        public IList GetNiosByMachineName(string agvName, string machineName, string state)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Name", agvName);
            attributes.Add("MachineName", machineName);
            attributes.Add("State", state);

            IList niolist = this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
            if (niolist.Count > 0)
            {
                return niolist;
            }
            logger.Error("nio{" + agvName + " - " + machineName + "} does not exist in repository");
            return null;
        }

        //181213 WIFI RF ZIGBEE available
        public IList GetNiosByAGVName(string agvName, string machineName)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Name", agvName);
            attributes.Add("MachineName", machineName);
            //attributes.Add("State", state);

            IList niolist = this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
            if (niolist.Count > 0)
            {
                return niolist;
            }
            logger.Error("nio{" + agvName + " - " + machineName + "} does not exist in repository");
            return null;
        }

        public IList GetNiosByMachineName(string machineName, string state)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("MachineName", machineName);
            attributes.Add("State", state);

            return this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
        }

        //181213 WIFI RF ZIGBEE available
        public IList GetNiosByMachineName(string machineName)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("MachineName", machineName);
            //attributes.Add("State", state);

            return this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
        }

        public IList GetNioesByApplicationName(string applicationName)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(Nio));
            criteria.Add(Restrictions.Eq("ApplicationName", applicationName));

            return this.PersistentDao.FindByCriteria(criteria);
        }


        public IList GetNioes(string name)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(Nio));
            criteria.Add(Restrictions.Eq("Name", name));

            return this.PersistentDao.FindByCriteria(criteria);
        }

        public IList GetNioByApplicationName(string applicationName)
        {
            return this.PersistentDao.FindByAttributeOrderBy(typeof(Nio), "ApplicationName", applicationName, "Name");
        }

        public IList GetNioesByMachineName(string machineName)
        {
            return this.PersistentDao.FindByAttributeOrderBy(typeof(Nio), "MachineName", machineName, "MachineName");
        }

        //181027 H Code Send Logic Change
        public SocketClient GetNioInterfacebyVehicleId(string vehicleId, string applicationName)
        {
            //Vehicle's socketClient
            SocketClient socketClient = (SocketClient)this.GetNioInterface(vehicleId);

            if (socketClient == null)
            {
                return null;
            }

            IList listNio = GetNioesByApplicationName(applicationName);

            if (listNio != null && listNio.Count > 0)
            {
                //Application Nio List
                for (IEnumerator iterator = listNio.GetEnumerator(); iterator.MoveNext(); )
                {
                    Nio nio = (Nio)iterator.Current;
                   
                    //Vehicle's Nio, Application Nio Same (ex:TCP15)
                    if (socketClient.Nio.Name == nio.Name)
                    {
                        return socketClient;
                    }
                }
            }
            else
            {
                logger.Warn("Can Not Find Nio Interface By APP : " + applicationName);
            }

            return null;
        }
    }
}


