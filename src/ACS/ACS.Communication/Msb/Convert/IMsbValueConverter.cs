using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb
{
    public interface IMsbValueConverter
    {
        string ChangeValue(string paramstring1, string paramstring2, string paramstring3, Object paramObject, string paramstring4);

        string ComposeValue(string paramstring1, Object paramObject, string paramstring2);
    }
}
