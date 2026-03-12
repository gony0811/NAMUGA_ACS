using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Spring.Context;
using Spring.Context.Support;
using Spring.Context.Events;
using System.Configuration;
using System.Xml;
using System.IO;
using System.Diagnostics;
using ACS.Framework.Reload;
using ACS.Framework.Application;
using ACS.Framework.Logging;
using ACS.Utility;
using log4net;
using log4net.Config;

namespace ACS.Application
{
    public class Executor
    {
        private Logger logger = Logger.GetLogger("ErrorLogger");
        private IList definitions = new ArrayList();
        private IList serviceDefinitions = new ArrayList();     
        public string Id { get; set; }
        public string StartUpPath { get; set; }
        public string ConfigPath { get; set; }
        public string Type { get; set; }
        public string HardwareType { get; set; }
        public string Msb { get; set; }
        public string Memory { get; set; }
        public string Jmx { get; set; }
        public bool UseManagedBean { get; set; }
        public string BaseClass { get; set; }
        public string ServicePath { get; set; }
        public bool UseService { get; set; }
        public string LogPath { get; set; }
        public string LogTemplate { get; set; }
        public string LogLevel { get; set; }
        public bool UpdateLogPropertiesFile { get; set; }


        public IList Definitions { get { return definitions; } set { definitions = value; } }
        public IList ServiceDefinitions { get { return serviceDefinitions; } set { serviceDefinitions = value; } }
        public IBeforeContextInitialized BeforeContextInitialized { get; set; }

        private IApplicationContext applicationContext = null;

        public IApplicationContext Start()
        {
            try
            {
                long startTime = System.DateTime.UtcNow.Millisecond;
                this.StartUpPath = ConfigurationManager.AppSettings[Settings.SYSTEM_STARTUP_PATH];

                this.Id = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];
                if (this.Id == null)
                {
                    //log
                    throw new ApplicationException("process id is null");
                }


                if(string.IsNullOrEmpty(StartUpPath))
                {
                    string exe = Process.GetCurrentProcess().MainModule.FileName;
                    StartUpPath = Path.GetDirectoryName(exe);
                    //using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + Id + "'"))
                    //{
                    //    wmiService.Get();
                    //    string currentServiceExePath = wmiService["PathName"].ToString();
                    //    logger.Info(wmiService["PathName"].ToString());
                    //}
                }

                
                this.HardwareType = ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_KEY_HARDWARE_TYPE];

                if (string.IsNullOrEmpty(this.ConfigPath))
                {
                    this.ConfigPath = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_CONFIG];
                    this.ConfigPath = this.ConfigPath.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                }

                XmlElement node = GetApplicationElement();
                Load(node);

                CreateLogProperty();

                this.BeforeContextInitialized = (this.BeforeContextInitialized == null ? (this.BeforeContextInitialized = new BeforeContextInitializedImplement()) : this.BeforeContextInitialized);
                this.BeforeContextInitialized.Execute(this);

                IReloadableClassLoader serviceClassLoader = null;

                if(this.UseService)
                {
                    IApplicationContext parentApplicationContext = new XmlApplicationContext(GetBeanDefinitionsAsStringArray());
                    parentApplicationContext.Name = this.Id;
                    string path = SystemUtility.GetFullPathName(ServicePath);
                    serviceClassLoader = new CustomClassLoader(path);
                    applicationContext = new XmlApplicationContext(false, parentApplicationContext, GetServiceBeanDefinitionsAsStringArray());

                    foreach(object classObject in serviceClassLoader.GetClassLoader())
                    {
                        applicationContext.ConfigureObject(classObject, classObject.GetType().FullName);
                    }
                }
                else
                {
                    applicationContext = new XmlApplicationContext(GetAllBeanDefinitionsAsStringArray());
                    ((XmlApplicationContext)applicationContext).Name = this.Id;

                }

                AfterContextInitializedEventArg afterContextInitializedEventArg = new AfterContextInitializedEventArg(this, applicationContext);

                applicationContext.PublishEvent(this, afterContextInitializedEventArg);

                logger.Info(string.Format("{0}({1}) server is started.", this.Type, this.Id));
                return applicationContext;
            }
            catch (Exception e)
            {
                throw new ApplicationException("Executor Start() Error", e);
            }
        }

        public void Stop()
        {
            IApplicationControlManager applicationControlManager = (IApplicationControlManager)applicationContext.GetObject("ApplicationControlManager");
            if(applicationControlManager.InvokeStop(this.Type, this.Id))
            {
                logger.Info(string.Format("{0}({1}) server is stopped.", this.Type, this.Id));
            }
            else
            {
                logger.Error(string.Format("{0}({1}) server stop is failed.", this.Type, this.Id));
            }
        }


        public String[] GetServiceBeanDefinitionsAsStringArray()
        {
            String[] serviceBeanDefinitionPaths = new String[this.ServiceDefinitions.Count];
            for (int index = 0; index < this.ServiceDefinitions.Count; index++)
            {
                String path = SystemUtility.GetFullPathName((String)this.ServiceDefinitions[index]);
                serviceBeanDefinitionPaths[index] = path;
            }
            return serviceBeanDefinitionPaths;
        }

        private String[] GetAllBeanDefinitionsAsStringArray()
        {
            String[] allBeanDefinitionPaths = new String[this.Definitions.Count + this.ServiceDefinitions.Count];
            for (int index = 0; index < this.Definitions.Count; index++)
            {
                String path = (String)this.Definitions[index];
                allBeanDefinitionPaths[index] = path;
            }
            for (int index = 0; index < this.ServiceDefinitions.Count; index++)
            {
                String path = (String)this.ServiceDefinitions[index];
                allBeanDefinitionPaths[index] = path;
            }
            return allBeanDefinitionPaths;
        }

        private String[] GetBeanDefinitionsAsStringArray()
        {
            String[] beanDefinitionPaths = new String[this.Definitions.Count];
            for (int index = 0; index < this.Definitions.Count; index++)
            {
                String path = StartUpPath + @"/" + (String)this.Definitions[index];
                beanDefinitionPaths[index] = path;
            }
            return beanDefinitionPaths;
        }


        private void Load(XmlElement applicationElement)
        {
            string fullPath = string.Empty;
            XmlElement idElement = applicationElement["id"];
            this.Msb = idElement.GetAttribute("msb");
            this.BaseClass = idElement.GetAttribute("base");
            this.Memory = idElement.GetAttribute("memory");
            this.Jmx = idElement.GetAttribute("jmx");
            this.UseManagedBean = idElement.GetAttribute("useManagedBean").Equals("true");
            this.ServicePath = idElement.GetAttribute("servicepath");
                     
            if (!string.IsNullOrEmpty(ServicePath))
            {
                fullPath = StartUpPath + @"/" + ServicePath;
                if (!Directory.Exists(fullPath))
                {
                    //System.out.println("[" + TimeUtils.getTimeToMilliPrettyFormat() + "] servicePath{" + this.servicePath + "} will be created");
                    Directory.CreateDirectory(fullPath);
                }
                this.UseService = true;
                
            }

            this.Type = idElement.GetAttribute("type");

            XmlElement logElement = applicationElement["log"];
            this.LogLevel = logElement.GetAttribute("level");
            this.LogTemplate = logElement.GetAttribute("template");
            if(string.IsNullOrEmpty(this.LogTemplate))
            {
                this.LogTemplate = StartUpPath + "/config/@{site}/startup/log-template.xml";
                this.LogTemplate = this.LogTemplate.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
            }
            else
            {
                this.LogTemplate = StartUpPath + @"/" + this.LogTemplate;
                this.LogTemplate = this.LogTemplate.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
            }
            this.LogPath = StartUpPath + @"/" + logElement.InnerText;
            this.LogPath = LogPath.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
            this.UpdateLogPropertiesFile = (logElement.GetAttribute("update").Equals("true"));

            XmlElement definitionsElement = applicationElement["definitions"];

            foreach(XmlNode node in definitionsElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment) continue;
                XmlElement definitionElement = (XmlElement)node;
                definitionElement.InnerText = definitionElement.InnerText.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                this.Definitions.Add(definitionElement.InnerText);
            }

            XmlElement serviceDefinitionsElement = applicationElement["services"];
            if (serviceDefinitionsElement != null)
            {
                foreach(XmlNode node in serviceDefinitionsElement)
                {
                    if (node.NodeType == XmlNodeType.Comment) continue;
                    XmlElement serviceDefinitionElement = (XmlElement)node;
                    serviceDefinitionElement.InnerText = serviceDefinitionElement.InnerText.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);
                    this.ServiceDefinitions.Add(serviceDefinitionElement.InnerText);
                }
            }
            else
            {
                //System.out.println("[" + TimeUtils.getTimeToMilliPrettyFormat() + "] services element does not exist in startup configuration file, you can not use the reload funtionality");
            }
        }

        private void CreateLogProperty()
        {
            if (this.UpdateLogPropertiesFile)
            {
                List<string> newLines = new List<string>();
                FineLevel fineLevel = new FineLevel(FineLevel.FINE_INT, "FINE", 0);

                String logLevel = this.LogLevel;
                if (this.LogLevel.Equals("FINE"))
                {
                    logLevel = "FINE#" + fineLevel.FINE.Name;
                }
                else if(this.LogLevel.Equals("WELL"))
                {

                }

                try
                {
                    IList lines = System.IO.File.ReadAllLines(LogTemplate);

                    foreach (var line in lines)
                    {
                        string newLine = line as string;

                        int equalIndex = newLine.IndexOf("$");

                        if (equalIndex > 0)
                        {
                            if (newLine.Contains("${log.level}"))
                            {
                                newLine = newLine.Replace("${log.level}", logLevel);
                            }

                            if (newLine.Contains("${process.type}"))
                            {
                                newLine = newLine.Replace("${process.type}", this.Type);
                            }

                            if (newLine.Contains("${process.name}"))
                            {
                                newLine = newLine.Replace("${process.name}", this.Id);
                            }

                            newLines.Add(newLine);
                        }
                        else
                        {
                            newLines.Add(newLine);
                        }
                    }
                    System.IO.File.WriteAllLines(this.LogPath, newLines);
                }
                catch (Exception e)
                {

                    Console.WriteLine(e.Message);
                }
            }

            XmlConfigurator.ConfigureAndWatch(new FileInfo(LogPath));
        }

        private XmlElement GetApplicationElement()
        {
            XmlDocument document = new XmlDocument();
            string path = StartUpPath + @"/" + ConfigPath;

            document.Load(path);

            if (document == null)
            {
                //System.out.println("[" + TimeUtils.getTimeToMilliPrettyFormat() + "] failed to start, please check the configPath{" + this.configPath + "}");
                //System.exit(0);
            }
            XmlNodeList ids = document.SelectNodes("//id");

            foreach(XmlNode id in ids)
            {
                XmlNode idNode = id.LastChild;
                if (idNode.InnerText.Equals(this.Id))
                {
                    return (XmlElement)id.ParentNode;
                }
            }
            //System.out.println("[" + TimeUtils.getTimeToMilliPrettyFormat() + "] " + this.id + " does not exist in startup configuration file, please double-check it");
            //System.exit(0);
            return null;
        }

    }
}
