using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base.Interface;
using ACS.Framework.Material;
using ACS.Framework.Material.Model;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.Application.Model;
using Autofac;
using System.Collections;

namespace ACS.Biz
{
    public class DemoProcess : BaseBizJob, IDisposable
    {
        public IPersistentDao persistentDao { private get; set; }
        public IMaterialManagerEx materialManager { private get; set; }
        public IResourceManagerEx resourceManager { get; set; }

        public DemoProcess()
        {
            //persistentDao = (IPersistentDao)LifetimeScope["PersistentDao"];
            //materialManager = (IMaterialManagerEx)LifetimeScope["MaterialManager"];
        }

        public override int ExecuteJob(object[] args)
        {
            try
            {
                resourceManager = LifetimeScope.Resolve<IResourceManagerEx>();
                ACS.Framework.Application.Model.Application application = (ACS.Framework.Application.Model.Application)args[1];
                //resourceManager = (IResourceManagerEx)applicationContext.GetObject("ResourceManager");
                IList listBays = this.resourceManager.GetBays();

                CarrierEx carrier = new CarrierEx();
                carrier.Id = "111";
                carrier.Type = "ABC";
                carrier.CreateTime = DateTime.Now;
                materialManager.CreateCarrier(carrier);
                persistentDao.Update("");
            }
            catch(Exception e)
            {
                Dispose();
                throw e;
            }
            
            
            return 0;
        }

        public void Dispose()
        {
            LifetimeScope = null;
        }
    }
}
