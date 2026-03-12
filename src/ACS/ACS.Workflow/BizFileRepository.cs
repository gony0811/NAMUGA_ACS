using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ACS.Workflow
{
    public class BizFileRepository
    {
        private string folderPath;
        private string configFileName = "ACS.Biz.Config.xml";

        private ConcurrentDictionary<string, Tuple<Type, object, bool>> instanceList = new ConcurrentDictionary<string, Tuple<Type, object, bool>>();

        public string FolderPath { get {return folderPath; } set { folderPath = value; }}
        public string ConfigFileName { get {return configFileName; } set { configFileName = value; } }
        public ConcurrentDictionary<string, Tuple<Type, object, bool>> InstanceList { get { return instanceList; } set { instanceList = value; } }

        public BizFileRepository()
        {
            folderPath = AppDomain.CurrentDomain.BaseDirectory + "Process";
        }

        public BizFileRepository(string folderPath, string configFileName)
        {
            this.folderPath = folderPath;
            this.configFileName = configFileName;
        }

        public void Init()
        {
            ConfigFileLoad();
        }

        public void Reload()
        {
            ConfigFileLoad();
        }

        private void ConfigFileLoad()
        {
            string filePath = System.IO.Path.Combine(folderPath, configFileName);

            XmlDocument document = new XmlDocument();

            document.Load(filePath);

            XmlNodeList xmlNodeList = document.ChildNodes;

            foreach(XmlNode node in xmlNodeList)
            {
                if (node.Name != "Assembly") continue;

                string asmId = node.ChildNodes[0].LastChild.Value;
                XmlAttribute asmAttribute = node.ChildNodes[0].Attributes[1];

                if (!(asmAttribute.LastChild.Value.ToUpper() == "TRUE")) continue;

                string source = node.ChildNodes[1].LastChild.Value;

                if(source.Contains("@{AppStartUp}"))
                {
                    source = source.Replace("@{AppStartUp}", AppDomain.CurrentDomain.BaseDirectory);
                }
                // macOS/Linux path compatibility
                source = source.Replace('\\', System.IO.Path.DirectorySeparatorChar);

                XmlNodeList classList = node.ChildNodes[2].ChildNodes;

                //AppDomain dom = AppDomain.CreateDomain(asmId);
                //AssemblyName assemblyName = new AssemblyName();
                //assemblyName.CodeBase = source;
                Assembly assembly = Assembly.Load(File.ReadAllBytes(source));
                //Assembly assembly = Assembly.LoadFrom(source);
                
                string command = "", baseType = "", parallel = "false";

                foreach (XmlNode classNode in classList)
                {
                    if (classNode.Name != "definition") continue;

                    string instanceName = classNode.LastChild.Value;
                    XmlAttributeCollection attributeList = classNode.Attributes;

                    foreach (XmlAttribute attribute in attributeList)
                    {
                        if (attribute.Name.Equals("command"))
                        {
                            command = attribute.LastChild.Value;
                            continue;
                        }
                        else if (attribute.Name.Equals("base"))
                        {
                            baseType = attribute.LastChild.Value;
                            continue;
                        }
                        else if (attribute.Name.Equals("parallel"))
                        {
                            parallel = attribute.LastChild.Value;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (command == "RAIL-VEHICLELOCATIONCHANGED")
                    {
                        Type type = assembly.GetType(instanceName);
                        object obj = Activator.CreateInstance(type);
                        bool isParallel = parallel.ToUpper().Equals("TRUE") ? true : false;
                        Tuple<Type, object, bool> instance = new Tuple<Type, object, bool>(type, obj, isParallel);

                        instanceList.AddOrUpdate(command, instance, (key, oldValue) => instance);
                    }
                    else
                    {
                        Type type = assembly.GetType(instanceName);
                        object obj = Activator.CreateInstance(type);
                        bool isParallel = parallel.ToUpper().Equals("TRUE") ? true : false;
                        Tuple<Type, object, bool> instance = new Tuple<Type, object, bool>(type, obj, isParallel);

                        instanceList.AddOrUpdate(command, instance, (key, oldValue) => instance);
                    }
  
                }              
            }         
        }
    }

}
