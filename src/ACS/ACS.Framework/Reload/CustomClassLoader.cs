using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace ACS.Framework.Reload
{
    public class CustomClassLoader : IReloadableClassLoader
    {
        private List<object> classLoader = new List<object>();
        private string directoryName;

        public string ClassPath { get; set; }

        public CustomClassLoader(string dir)
            : this(dir, null)
        {
            
        }

        public CustomClassLoader(String dir, IListener listener)
        {
            this.directoryName = dir;
            string[] files = Directory.GetFiles(dir);
            Reload(files, listener);
        }

        public CustomClassLoader(DirectoryInfo directoryInfo)
        {
            this.directoryName = directoryInfo.FullName;
        }

        


        public List<object> GetClassLoader()
        {
            return classLoader;
        }

        public string GetDirectoryName()
        {
            return this.directoryName;
        }

        public void Reload()
        {
            Reload(ClassPath, null);
        }

        public void Reload(string[] files, CustomClassLoader.IListener listener)
        {
            foreach(string file in files)
            {
                char[] sep = { '.' };
                string[] fileType = file.Split(sep);

                if (string.Equals(fileType[fileType.Length - 1].ToUpper(), "DLL"))
                {
                    string fullpath = file;
                    Assembly assembly = Assembly.LoadFile(fullpath);
                    Type[] types = (Type[])assembly.ExportedTypes;

                    foreach(Type type in types)
                    {
                        if(type != null)
                        {
                            if (type.IsAbstract)
                            {
                                continue;
                            }
                            else
                            {
                                classLoader.Add(Activator.CreateInstance(type));
                                continue;
                            }
                                
                        }
                    }
                }
            }
        }

        public void Reload(string dir, CustomClassLoader.IListener listener)
        {
            this.directoryName = dir;
            ClassPath = dir;
            string[] files = Directory.GetFiles(dir);

            
            Reload(files, listener);
        }

        public void Unload()
        {
            classLoader = null;
        }


        public interface IListener
        {
            void PluginLoaded(string paramString);

            void Exception(Exception paramException);
        }
    }
}
