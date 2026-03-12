using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ACS.Framework.Reload
{
    public interface IReloadableClassLoader
    {
        void Reload();

        void Reload(string[] paramString, CustomClassLoader.IListener paramListener);

        void Reload(string directoryPath, CustomClassLoader.IListener paramListener);

        void Unload();

        List<object> GetClassLoader();

        string GetDirectoryName();
    }
}
