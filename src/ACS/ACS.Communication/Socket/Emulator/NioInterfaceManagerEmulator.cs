using ACS.Communication.Socket.Checker;
using ACS.Communication.Socket.Model;
using ACS.Core.Base;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Utility;
using ACS.Core.Workflow;
using Autofac;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ACS.Communication.Socket.Emulator
{
    /// <summary>
    /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
    /// TCP Server용 
    /// </summary>
    //public class NioInterfaceManagerEx : AbstractManager, IApplicationContextAware
    public class NioInterfaceManagerEmulator : NioInterfaceManager
    {
        public override void DisplayAll()
        {
            foreach (DictionaryEntry iterator in NioInterfaces)
            {
                SocketServer socketServer = (SocketServer)iterator.Value;
                logger.Info(socketServer.Nio);
            }
        }

        public override string GetNioInterfaceNames()
        {
            return this.NioInterfaces.Keys.ToString();
        }

        //public int GetNioInterfaceCount()
        //{
        //    return this.nioInterfaces.Count;
        //}

        //public override SocketServer GetNioInterface(string name)
        public override object GetNioInterface(string name)
        {
            this.ResourceManager = LifetimeScope.Resolve<IResourceManagerEx>();

            VehicleEx vehicle = this.ResourceManager.GetVehicle(name);
            SocketServer socketServer = (SocketServer)this.NioInterfaces[vehicle.NioId];
            return socketServer;
        }

        public override void Load(string applicationName)
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

        public override void Unload()
        {
            foreach (DictionaryEntry iterator in NioInterfaces)
            {
                UnloadNioInterface((string)iterator.Key);
            }
        }

        public override void LoadNioInterface(Nio nio)
        {
            SocketServer socketServer = (SocketServer)System.Activator.CreateInstance(Type.GetType(nio.InterfaceClassName));
            socketServer.Nio = nio;

            socketServer.Initialize();

            try
            {
                IWorkflowManager workflowManager = (IWorkflowManager)this.LifetimeScope.ResolveNamed<IWorkflowManager>(nio.WorkflowManagerName);

                socketServer.WorkflowManager = workflowManager;
                socketServer.DuplicateChecker = this.DuplicateChecker;
            }
            catch (Exception e)
            {
                logger.Warn("workflow is not used, configure it if you use workflow", e);
            }
            finally
            {

            }
            LoadNioInterface(nio.getName(), socketServer);
        }

        public void LoadNioInterface(String name, SocketServer socketServer)
        {
            if (this.NioInterfaces.Contains(name))
            {
                logger.Error("nio{" + name + "} already exists, older will be erased and newer is going to be added");
                SocketServer older = (SocketServer)this.NioInterfaces[name];
                if (older.IsOpen())
                {
                    older.Stop();
                }
                this.NioInterfaces.Remove(name);
            }

            if ((string.IsNullOrEmpty(socketServer.Nio.State)) || (!socketServer.Nio.State.Equals("LOADED")))
            {
                //logger.fine("nio{" + socketClient.getNio().getName() + "} state will be changed to {" + "LOADED" + "}");
                socketServer.Nio.State = "LOADED";
                UpdateNioState(socketServer.Nio);
            }
            socketServer.Initialize();
            this.NioInterfaces.Add(name, socketServer);
            //logger.fine("nio{" + name + "} was loaded, " + socketClient);

            DisplayAll();
        }

        public override void LoadNioInterface(String name)
        {
            Nio nio = (Nio)this.PersistentDao.FindByName(typeof(Nio), name);
        }

        public override void LoadNioInterface(String applicationName, String name)
        {
            var attributes = new Dictionary<string, object> { { "ApplicationName", applicationName }, { "Name", name } };
            IList nioes = this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
            if (nioes.Count == 0)
            {
                logger.Error("nio{" + name + "} does not exist");
                return;
            }
            LoadNioInterface((Nio)nioes[0]);
        }

        public override void UnloadNioInterface(string name)
        {
            SocketServer socketServer = (SocketServer)this.NioInterfaces[name];

            this.NioInterfaces.Remove(name);

            if (socketServer != null)
            {
                if (socketServer.IsOpen())
                {
                    socketServer.Stop();
                }
                //logger.fine("nio{" + name + "} was unloaded, " + socketClient);
                if ((string.IsNullOrEmpty(socketServer.Nio.State)) || (!socketServer.Nio.State.Equals("UNLOADED")))
                {
                    //logger.fine("state will be changed to {UNLOADED}");
                    socketServer.Nio.State = "UNLOADED";
                    UpdateNioState(socketServer.Nio);
                }
            }
            else
            {
                //logger.fine("already unloaded, name{" + name + "}");
            }
            this.NioInterfaces.Remove(name);

            DisplayAll();
        }

        //public void ReloadAll(string applicationName)
        //{
        //    Unload();
        //    Load(applicationName);
        //}

        //public void ReloadNioInterface(string applicationName, string name)
        //{
        //    UnloadNioInterface(name);
        //    LoadNioInterface(applicationName, name);
        //}

        //public void RestartAll()
        //{
        //    StopAll();
        //    StartAll();
        //}

        //public void RestartNioInterface(string name)
        //{
        //    StopNioInterface(name);

        //    StartNioInterface(name);
        //}

        public override void StartAll()
        {
            foreach (DictionaryEntry iterator in NioInterfaces)
            {
                SocketServer socketServer = (SocketServer)iterator.Value;
                StartNioInterface(socketServer);
            }
        }

        public override int StartNioInterface(string name)
        {
            logger.Info("starting open nio{" + name + "}");
            return StartNioInterface((SocketServer)this.NioInterfaces[name]);
        }

        public int StartNioInterface(SocketServer socketServer)
        {
            int result = 0;
            if (socketServer != null)
            {
                if (!socketServer.IsOpen())
                {
                    if (!socketServer.Nio.State.Equals("CONNECTING"))
                    {
                        UpdateNioState(socketServer, "CONNECTING");
                        if (socketServer.Start())
                        {
                            UpdateNioState(socketServer, "CONNECTED");
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

        public override void StopAll()
        {
            foreach (DictionaryEntry iterator in NioInterfaces)
            {
                StopNioInterface((SocketServer)iterator.Value);
            }
        }

        public override bool StopNioInterface(string name)
        {
            SocketServer socketServer = (SocketServer)this.NioInterfaces[name];
            return StopNioInterface(socketServer);
        }

        public bool StopNioInterface(SocketServer socketServer)
        {
            UpdateNioState(socketServer, "CLOSED");
            bool result = socketServer.Stop();
            UpdateNioState(socketServer, "CLOSED");
            return result;
        }

        //public bool IsConnected(string name)
        //{
        //    SocketClient socketClient = (SocketClient)this.nioInterfaces[name];
        //    return IsConnected(socketClient);
        //}

        public bool IsConnected(SocketServer socketClient)
        {
            return socketClient.SessionOpened;
        }

        //public void UpdateNioInterface(String name)
        //{
        //    SocketClient socketClient = (SocketClient)this.nioInterfaces[name];
        //    Nio nio = GetNio(name, socketClient.Nio.ApplicationName);
        //    socketClient.Nio = nio;
        //}

        //public void AddNio(Nio nio)
        //{
        //    this.PersistentDao.Save(nio);
        //}

        //public void UpdateNio(Nio nio)
        //{
        //    this.PersistentDao.Update(nio);
        //}

        public int UpdateNioState(SocketServer socketServer, string state)
        {
            Nio nio = socketServer.Nio;
            if (string.IsNullOrEmpty(state))
            {
                //logger.fine("nio{" + nio.getName() + "}.state will be changed to {" + state + "}, " + nio);
                nio.State = state;
                return UpdateNioState(nio);
            }
            return 0;
        }

        //public void RemoveNio(Nio nio)
        //{
        //    this.PersistentDao.Delete(nio);
        //}

        //public IList GetNioes()
        //{
        //    return this.PersistentDao.FindAll(typeof(Nio));
        //}

        //public int GetNioCount()
        //{
        //    return this.PersistentDao.Count(DetachedCriteria.For(typeof(Nio)));
        //}

        /**
        * @deprecated
        */
        //public Nio GetNio(string name)
        //{
        //    return (Nio)this.PersistentDao.FindByName(typeof(Nio), name, false);
        //}

        //public Nio GetNioByState(String name, String state)
        //{
        //    DetachedCriteria criteria = DetachedCriteria.For(typeof(Nio));
        //    criteria.Add(Restrictions.Eq("Name", name));
        //    criteria.Add(Restrictions.Eq("State", state));

        //    IList niolist = this.PersistentDao.FindByCriteria(criteria);
        //    if (niolist.Count > 0)
        //    {
        //        return (Nio)niolist[0];
        //    }
        //    logger.Error("nio{" + name + " - " + state + "} does not exist in repository");
        //    return null;
        //}

        //public Nio GetNioByMachineName(string name, string machineName, string state)
        //{
        //    Dictionary<string, object> attributes = new Dictionary<string, object>();
        //    attributes.Add("Name", name);
        //    attributes.Add("MachineName", machineName);
        //    attributes.Add("State", state);

        //    IList niolist = this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
        //    if (niolist.Count > 0)
        //    {
        //        return (Nio)niolist[0];
        //    }
        //    logger.Error("nio{" + name + " - " + machineName + "} does not exist in repository");
        //    return null;
        //}

        //public IList GetNiosByMachineName(string agvName, string machineName, string state)
        //{
        //    Dictionary<string, object> attributes = new Dictionary<string, object>();
        //    attributes.Add("Name", agvName);
        //    attributes.Add("MachineName", machineName);
        //    attributes.Add("State", state);

        //    IList niolist = this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
        //    if (niolist.Count > 0)
        //    {
        //        return niolist;
        //    }
        //    logger.Error("nio{" + agvName + " - " + machineName + "} does not exist in repository");
        //    return null;
        //}


        //public IList GetNiosByMachineName(string machineName, string state)
        //{
        //    Dictionary<string, object> attributes = new Dictionary<string, object>();
        //    attributes.Add("MachineName", machineName);
        //    attributes.Add("State", state);

        //    return this.PersistentDao.FindByAttributes(typeof(Nio), attributes);
        //}

        //public IList GetNioByApplicationName(string applicationName)
        //{
        //    return this.PersistentDao.FindByAttributeOrderBy(typeof(Nio), "ApplicationName", applicationName, "Name");
        //}

        //public IList GetNioesByMachineName(string machineName)
        //{
        //    return this.PersistentDao.FindByAttributeOrderBy(typeof(Nio), "MachineName", machineName, "MachineName");
        //}

    }
}
