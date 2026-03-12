using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.Serialization;

namespace ACS.Communication.Msb
{
    public interface IMessageAgent
    {
        void Send(XmlDocument paramDocument);

        void Send(string paramstring);

        void Send(IDictionary paramMap);

        void Send(byte[] paramArrayOfByte);

        void Send(ISerializable paramSerializable);

        void Send(Object paramObject);

        void Send(XmlDocument paramDocument, string paramstring);

        void Send(string paramstring1, string paramstring2);

        void Send(IDictionary paramMap, string paramstring);

        void Send(byte[] paramArrayOfByte, string paramstring);

        void Send(ISerializable paramSerializable, string paramstring);

        void Send(Object paramObject, string paramstring);

        void Send(string paramstring1, bool paramBoolean, string paramstring2);

        void Send(string paramstring1, string paramstring2, bool paramBoolean, string paramstring3);

        void Send(XmlDocument paramDocument, bool paramBoolean, string paramstring);

        void Send(XmlDocument paramDocument, string paramstring1, bool paramBoolean, string paramstring2);

        void Send(Object paramObject, bool paramBoolean, string paramstring);

        void Send(Object paramObject, string paramstring1, bool paramBoolean, string paramstring2);


    }
}
