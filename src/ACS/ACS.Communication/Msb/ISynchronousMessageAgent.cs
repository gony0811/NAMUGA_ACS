using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.Serialization;

namespace ACS.Communication.Msb
{
    public interface ISynchronousMessageAgent
    {
        XmlDocument Request(XmlDocument paramDocument);

        XmlDocument Request(XmlDocument paramDocument, string paramstring);

        XmlDocument Request(XmlDocument paramDocument, long paramLong);

        XmlDocument Request(XmlDocument paramDocument, string paramstring, long paramLong);

        string Request(string paramstring);

        string Request(string paramstring1, string paramstring2);

        string Request(string paramstring, long paramLong);

        string Request(string paramstring1, string paramstring2, long paramLong);

        Object Request(Object paramObject);

        Object Request(Object paramObject, string paramstring);

        Object Request(Object paramObject, long paramLong);

        Object Request(Object paramObject, string paramstring, long paramLong);

        ISerializable Request(ISerializable paramSerializable);

        ISerializable Request(ISerializable paramSerializable, string paramstring);

        ISerializable Request(ISerializable paramSerializable, long paramLong);

        ISerializable Request(ISerializable paramSerializable, string paramstring, long paramLong);

        void Reply(XmlDocument paramDocument1, XmlDocument paramDocument2);

        void Reply(XmlDocument paramDocument, string paramstring);

        void Reply(ISerializable paramSerializable, string paramstring1, string paramstring2);

        XmlDocument Request(XmlDocument paramDocument, bool paramBoolean, string paramstring);

        XmlDocument Request(XmlDocument paramDocument, string paramstring1, bool paramBoolean, string paramstring2);

        XmlDocument Request(XmlDocument paramDocument, long paramLong, bool paramBoolean, string paramstring);

        XmlDocument Request(XmlDocument paramDocument, string paramstring1, long paramLong, bool paramBoolean, string paramstring2);

        string Request(string paramstring1, bool paramBoolean, string paramstring2);

        string Request(string paramstring1, string paramstring2, bool paramBoolean, string paramstring3);

        string Request(string paramstring1, long paramLong, bool paramBoolean, string paramstring2);

        string Request(string paramstring1, string paramstring2, long paramLong, bool paramBoolean, string paramstring3);

        Object Request(Object paramObject, bool paramBoolean, string paramstring);

        Object Request(Object paramObject, string paramstring1, bool paramBoolean, string paramstring2);

        Object Request(Object paramObject, long paramLong, bool paramBoolean, string paramstring);

        Object Request(Object paramObject, string paramstring1, long paramLong, bool paramBoolean, string paramstring2);

        void Reply(XmlDocument paramDocument1, XmlDocument paramDocument2, bool paramBoolean, string paramstring);

        void Reply(XmlDocument paramDocument, string paramstring1, bool paramBoolean, string paramstring2);
    }
}
