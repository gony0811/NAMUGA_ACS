using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Collections;

namespace ACS.Utility
{
    public class XmlUtility
    {
        public static XmlElement GetXmlElement(XmlDocument xml, string xPath)
        {
            XmlElement result = null;

            result = (XmlElement)xml.SelectSingleNode(xPath);

            return result;
        }

        public static bool SetDataToXml(XmlDocument document, string xpathName, string data)
        {
            XmlNode node = document.SelectSingleNode(xpathName);
            if (node == null) return false;

            //180928 ACS→ADS Report XML Format Pair
            if (String.IsNullOrEmpty(data) && node.InnerText == "\u200B")
            {

            }
            else
            {
                //node.InnerText = data + "\r\n";
                node.InnerText = data;
            }

            //node.InnerText = data;

            return true;
        }

        public static string GetDataFromXml(XmlDocument document, string xpathName)
        {
            XmlNode node = document.SelectSingleNode(xpathName);

            if (node == null) return "";
            
            return node.InnerText;
        }
       

        public static string GetLogStringFromXml(XmlElement element)
        {
            string onlyContent = string.Empty;

            if (element.ChildNodes.Count > 1)
            {
                onlyContent += string.Format("<{0}>", element.Name);
                onlyContent += string.Format("{0}", Environment.NewLine);

                foreach (XmlNode node in element.ChildNodes)
                {
                    onlyContent += GetLogStringFromXml((XmlElement)node);
                }

                onlyContent += string.Format("</{0}>", element.Name);
                onlyContent += string.Format("{0}", Environment.NewLine);
            }
            else
            {
                onlyContent += string.Format("<{0}>{1}</{0}>", element.Name, element.InnerText);
                onlyContent += string.Format("{0}", Environment.NewLine);
            }

            //for(int i= 0; i<nodes.Count; i++)
            //{
            //    foreach(XmlNode node in nodes[i].ChildNodes)
            //    {
            //        if(node.ChildNodes.Count > 0)
            //        {
            //            onlyContent += GetLogStringFromXml((XmlElement)node, "");
            //        }
            //        onlyContent += string.Format("<{0}>{1}</{0}>", node.Name, node.InnerText);
            //        onlyContent += string.Format("{0}", Environment.NewLine);
            //    }
            //}

            return onlyContent;
        }
    }
    public class MapUtility
    { 
        //SJP 
        // AdditionalInfo가 어떤 포맷인지 확인 필요
        // 임시로 "newdest,B2RTP01:B2RTP01_UD01_OP01" 이런 형태로 사용
        public static Hashtable StringToMap(string AdditionalInfo)
        {
            Hashtable map = new Hashtable();
            if (string.IsNullOrEmpty(AdditionalInfo)) return map;

            string[] nominator = AdditionalInfo.Split(',');

            //for (int index = 0; index < nominator.Length; index++)
            {
                //string[] values = nominator[index].Split('=');
                //if (values.Length == 2)
                {
                    if (!string.IsNullOrEmpty(nominator[0]) && nominator.Length > 1)
                        map[nominator[0]] = nominator[1];
                        
                }
                //else
                //{

                //}
            }
            return map;
        }
        public static String MapToString(Hashtable map)
        {
            StringBuilder str = new StringBuilder();
            foreach (DictionaryEntry e in map)
            {
                str.Append(e.Key + "," + e.Value);
            }

            return str.ToString();
        }
    }
    
}
