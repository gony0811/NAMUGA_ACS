using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using SysPath = System.IO.Path;
using SysFile = System.IO.File;

namespace ACS.Core.Workflow;

public class BizFileRepository
{
    private string folderPath;
    private string configFileName = "ACS.Biz.Config.xml";

    private ConcurrentDictionary<string, Tuple<Type, object, bool>> instanceList = new ConcurrentDictionary<string, Tuple<Type, object, bool>>();

    public string FolderPath { get { return folderPath; } set { folderPath = value; } }
    public string ConfigFileName { get { return configFileName; } set { configFileName = value; } }
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
        string filePath = SysPath.Combine(folderPath, configFileName);

        XmlDocument document = new XmlDocument();
        document.Load(filePath);

        XmlNodeList xmlNodeList = document.ChildNodes;

        foreach (XmlNode node in xmlNodeList)
        {
            if (node.Name != "Assembly") continue;

            string asmId = node.ChildNodes[0].LastChild.Value;
            XmlAttribute asmAttribute = node.ChildNodes[0].Attributes[1];

            if (!(asmAttribute.LastChild.Value.ToUpper() == "TRUE")) continue;

            string source = node.ChildNodes[1].LastChild.Value;

            if (source.Contains("@{AppStartUp}"))
            {
                source = source.Replace("@{AppStartUp}", AppDomain.CurrentDomain.BaseDirectory);
            }
            source = source.Replace('\\', SysPath.DirectorySeparatorChar);

            XmlNodeList classList = node.ChildNodes[2].ChildNodes;

            Assembly assembly = Assembly.Load(SysFile.ReadAllBytes(source));

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
                    }
                    else if (attribute.Name.Equals("base"))
                    {
                        baseType = attribute.LastChild.Value;
                    }
                    else if (attribute.Name.Equals("parallel"))
                    {
                        parallel = attribute.LastChild.Value;
                    }
                }

                Type type = assembly.GetType(instanceName);
                object obj = Activator.CreateInstance(type);
                bool isParallel = parallel.ToUpper().Equals("TRUE");
                Tuple<Type, object, bool> instance = new Tuple<Type, object, bool>(type, obj, isParallel);

                instanceList.AddOrUpdate(command, instance, (key, oldValue) => instance);
            }
        }
    }
}
