using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Resource.Model;
using ACS.Communication.Socket.Model;
using ACS.Utility;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using log4net;
using ACS.Framework.Base;

namespace ACS.Service
{
    public class DataHandlingServiceEx : AbstractServiceEx, ICloneable
    {
        public ILog logger = log4net.LogManager.GetLogger(typeof(DataHandlingServiceEx));
        
        public DataHandlingServiceEx() : base() { }
        public override void Init()
        {
            base.Init();
        }
        /* (non-Javadoc)
        * @see com.mcs.server.service.impl.DataHandlingService#getElementChildren(org.jdom.Document, java.lang.String)
        */
        public IList GetChildrenElement(XmlDocument document, String xpath)
        {
            //XmlElement element;
            ////XmlElement element = XmlUtils.getXmlElement(document, xpath);
            //XmlNodeList xmlNodeList = document.SelectNodes(xpath);
            //foreach(XmlNode xmlNode in xmlNodeList)
            //{
            //   element = (XmlElement)xmlNode;
            //}
            //if (element == null)
            //{
            //    //logger.Warn("{" + xpath + "} does not exist. please check xpath.");
            //    return new ArrayList();
            //}
            //
            //return element.getChildren();
            //return element;
            IList temp = new List<object>();
            return temp;
        }


        public IList GetChildrenElement(XmlElement element)
        {

            //if (element == null)
            //{
            //    //logger.Warn("element is null. please check element.");
            //    return new ArrayList();
            //}
            //
            //return element.getChildren();
            //
            IList temp = new List<object>();
            return temp;
        }

        public int GetChildrenElementSize(XmlDocument document, String xpath)
        {
           ////System.out.println(XmlUtils.toStringPrettyFormat(document));		
           //XmlElement element = XmlUtils.getXmlElement(document, xpath);
           //if (element == null)
           //{
           //    //logger.Warn("{" + xpath + "} does not exist. please check xpath.");
           //    return 0;
           //}
           //
           //return element.getChildren().size();
            return 0;
        }

        public XmlDocument GetDocumentByIndex(IList list, int index)
        {
            return (XmlDocument)list[index];
        }

        public XmlElement GetElementByIndex(IList list, int index)
        {
            return (XmlElement)list[index];
        }

        public XmlElement GetChildElement(XmlElement element, int index)
        {
            //????
            //return (XmlElement)element.getChildren()[index];
            
            return element;
        }

        public XmlElement GetChildElement(XmlElement element, String childElementName)
        {
            //????
            //return (XmlElement)element.getChild(childElementName);

            return element;
        }

        public String GetElementText(XmlDocument document, String xpath)
        {
            return XmlUtility.GetDataFromXml(document, xpath);
        }

        public String GetElementText(XmlElement element)
        {
            return element.InnerText;
        }

        public String GetAtrributeValue(XmlElement element, String attributeName)
        {

            return element.GetAttribute(attributeName);
        }

        public String GetXPathData(XmlDocument doc, String xpath)
        {
            return XmlUtility.GetDataFromXml(doc, xpath);
        }

        public XmlElement CloneElement(XmlElement element)
        {
            XmlNode xmlnode = element.Clone();

            return (XmlElement)xmlnode;
        }

        public XmlDocument CloneDocument(XmlDocument document)
        {
            XmlNode xmlnode = (XmlNode)document;

            XmlDocument returnDocu = new XmlDocument();
            returnDocu = (XmlDocument)xmlnode.Clone();

            return returnDocu;
        }

        public XmlElement AddElement(XmlElement parentElement, String name, String value)
        {
            XmlNode xmlnode = (XmlNode)parentElement;
            XmlDocument xmldoc = (XmlDocument)xmlnode;

            XmlElement Command = xmldoc.CreateElement(name);
            Command.InnerText = value;

            parentElement.AppendChild(Command);

            return parentElement;
        }

        public XmlElement AddElement(XmlDocument document, String xpath, String name, String value)
        {

            //XmlElement parentElement = XmlUtils.getXmlElement(document, xpath);
            //XmlElement element = new XmlElement();
            //return (XmlElement)parentElement.AppendChild(newElement);


            XmlNode ParentNode = document.SelectSingleNode(xpath);
            XmlElement newEle = document.CreateElement(name);
            newEle.InnerText = value;
            ParentNode.AppendChild(newEle);

            XmlElement temp = (XmlElement)document.SelectSingleNode(name);
            return temp;
        }

        public XmlElement AddAttribute(XmlElement element, String name, String value)
        {
            element.SetAttribute(name, value);
            return element;
        }

        public IList CreateArrayList()
        {
            return new ArrayList();
        }

        public Object Get(IList list, double index) {
		
		//Object obj = list.get((int) index);
        Object obj = list[(int)index];
		return obj;
	}

        public Nio GetNio(IList list, double index)
        {
            //return (Nio)list.get((int)index);
            return (Nio)list[(int)index];
        }

        public double Size(IList list)
        {
            if (list == null)
            {
                //logger.Warn("list is null, default size{0} is applied");
                return 0;
            }
            //logger.Info("size{" + list.size() + "}");
            return list.Count;
        }

        public int Size(Hashtable map)
        {
            if (map == null)
            {
                //logger.Warn("map is null, default size{0} is applied");
                return 0;
            }
            return map.Count;
        }

        public bool IsSizeZero(IList list)
        {
            if (list == null || list.Count == 0)
            {
                return true;
            }

            return false;
        }

        public double Increase(double value)
        {
            return ++value;
        }

        public double Decrease(double value)
        {
            return --value;
        }

        public bool Greater(double value, double compared)
        {
            return this.Greater(value, compared, true);
        }

        public bool Greater(double value, double compared, bool containEqual)
        {
            if (containEqual)
            {
                return (value >= compared);
            }
            else
            {
                return (value > compared);
            }
        }

   
        public bool Less(int value, int compared)
        {
            bool result = (value < compared);
            return result;
        }

        public bool Less(int value, int compared, bool containEqual)
        {
            if (containEqual)
            {
                return (value <= compared);
            }
            else
            {
                return (value < compared);
            }
        }

        public bool Equals(String value, String compared)
        {
            return value.Equals(compared);
        }

        public bool EqualsIgnoreCase(String value, String compared)
        {
            return value.Equals(compared, StringComparison.OrdinalIgnoreCase);
        }

        public bool Contains(String value, String searchValue)
        {
            return value.Contains(searchValue);
        }

        public String Concatenate(String first, String second, String delimiter)
        {

            return first + delimiter + second;
        }

        public String Concatenate(String first, String second, String third, String delimiter)
        {

            return first + delimiter + second + delimiter + third;
        }

        public String Concatenate(String first, String second, String third, String fourth, String delimiter)
        {

            return first + delimiter + second + delimiter + third + delimiter + fourth;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }


}
