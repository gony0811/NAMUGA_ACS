using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;
using ACS.Communication.Http.uHttpSharp.Handlers;
using ACS.Communication.Http.uHttpSharp;
using ACS.Communication.Http.Handler;
using ACS.Core.Logging;
using ACS.Core.Workflow;

namespace ACS.Communication.Http.Controllers
{
    class StringsRestController : IRestController<string>
    {
        public Logger logger = Logger.GetLogger(typeof(StringsRestController));
        private readonly ICollection<string> _collection = new HashSet<string>();
        //public Task<IEnumerable<string>> Get(IHttpRequest request)
        //{
        //    var id = request.RequestParameters[2];
        //    var command = request.QueryString.GetByName("IF_NAME");
        //    _collection.Add(command);

        //    var model = new ModelBinder(new ObjectActivator()).Get<MyModel>(request.QueryString);

        //    return Task.FromResult<IEnumerable<string>>(_collection);
        //}
        //private HostHttpInterfaceServiceEx hosthttpinterfaceservice { get; set; }

        //public StringsRestController(HostHttpInterfaceServiceEx HostHttpInterfaceServiceEx)
        //{
        //    hosthttpinterfaceservice = HostHttpInterfaceServiceEx;
        //}




        private IWorkflowManager WorkflowManager { get; set; }

        public StringsRestController(IWorkflowManager manager)
        {
            WorkflowManager = manager;
        }

        /// <summary>
        /// GET이면... (미사용)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<string> Get(IHttpRequest request)
        {
            try
            {
                ////IHttpRequest에 ReqUri를 추가함..
                //string originalUri = request.ReqUri;        //내부변수에 옮겨담음..
                //int index = request.ReqUri.IndexOf('?');        //'?'구분자 index 확인..
                //string query = "";

                //if (originalUri.IndexOf('?') > -1)
                //{
                //    query = originalUri.Substring(index + 1, originalUri.Length - index - 1);       //'?'구분자 이후 querystring 가져옴..
                //}

                //var dict = HttpUtility.ParseQueryString(query);     //dictionary형태??? 로 변경..?
                //var json = dict.AllKeys.ToDictionary(k => k, k => dict[k]);     //구분자 =,&에 따라서 Key Value 형태로 변경..
                //var result = JsonConvert.SerializeObject(json, new KeyValuePairConverter());        //Key Value Converter를 통해 Json형태로 변경..

                ////Xml구조로 만드는과정....보류...
                //string strData = string.Format("{{DATA:{0}}}", result);

                //XmlDocument document = JsonConvert.DeserializeXmlNode(strData.ToString(), "MSG");

                return Task.FromResult<string>(string.Empty);
            }
            catch(Exception e)
            {
                logger.Info("Get Method: Json to Xml Parsing Error - " + e.ToString());
                throw;
            }

        }

        /// <summary>
        /// Post의 Body로 실려온 경우..
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        //public Task<string> Post_old(IHttpRequest request)
        //{            
        //    try
        //    {
        //        var body = Encoding.UTF8.GetString(request.Post.Raw);       //Post의 Body에 실려온 Byte[] Data를 Json으로 변환
        //        logger.Info("\nWAS Http Message Receive : \n" + body);
        //        JObject obj = new JObject(JObject.Parse(body));     //Json Object형태로 변경하고..
        //        XmlDocument document = JsonConvert.DeserializeXmlNode(obj.ToString(), "root");       //Xml형태로 변경한다..

        //        //hosthttpinterfaceservice.ConveyHostMessage(document, XmlUtility.GetDataFromXml(document, "root/DS_TIBCO_MSG/CMD_TYPE"));

        //        switch (XmlUtility.GetDataFromXml(document, "root/DS_TIBCO_MSG/CMD_TYPE"))
        //        {
        //            case "MOVECMD":
        //                hosthttpinterfaceservice.ConveyMOVECMD(document);
        //                break;
        //            case "MOVECANCEL":
        //                hosthttpinterfaceservice.ConveyMOVECANCEL(document);
        //                break;
        //            case "MOVEUPDATE":
        //                hosthttpinterfaceservice.ConveyMOVEUPDATE(document);
        //                break;
        //            default:
        //                break;
        //        }

        //        return Task.FromResult(string.Empty);     //Response Body 내용 없음으로..
        //    }
        //    catch(Exception e)
        //    {
        //        logger.Info("Post Method: Json to Xml Parsing Error - " + e.ToString());
        //        throw;
        //    }
        //}

        //suji edit
        public Task<string> Post(IHttpRequest request)
        {
            try
            {
                var body = Encoding.UTF8.GetString(request.Post.Raw);       //Post의 Body에 실려온 Byte[] Data를 Json으로 변환
                logger.Info("\nWAS Http Message Receive : \n" + body);
                JObject obj = new JObject(JObject.Parse(body));     //Json Object형태로 변경하고..
                XmlDocument document = JsonConvert.DeserializeXmlNode(obj.ToString());
                // XmlDocument document = JsonConvert.DeserializeXmlNode(obj.ToString(), "root");       //Xml형태로 변경한다..


                executeWorkflow("TRSJOBREQ", document);

                //hosthttpinterfaceservice.Convey(document);


                return Task.FromResult(string.Empty);     //Response Body 내용 없음으로..
            }
            catch(Exception e)
            {
                logger.Info("Post Method: Json to Xml Parsing Error - " + e.ToString());
                throw;
            }
        }

        public void executeWorkflow(string messageName, XmlDocument document)
        {
            this.WorkflowManager.Execute(messageName, document);
        }


        //public Task<string> PostBridge(IHttpRequest request)
        //{
        //    try
        //    {
        //        switch(request.RequestParameters[1].ToString())
        //        {
        //            case "1D1F":
        //                // Post1
        //                break;
        //            case "1D2F":
        //                // Post2
        //                break;
        //            case "2D1F":
        //                // Post3
        //                break;
        //            case "2D2F":
        //                // Post4
        //                break;
        //            default:
        //                break;
        //        }

        //        return Task.FromResult(string.Empty);     //Response Body 내용 없음으로..
        //    }
        //    catch (Exception e)
        //    {
        //        logger.Info("Post Method: Json to Xml Parsing Error - " + e.ToString());
        //        throw;
        //    }
        //}




        // 미사용
        //public Task<string> TrsJobReq(IHttpRequest request)
        //{
        //    var body = Encoding.UTF8.GetString(request.Post.Raw);
        //    JObject obj = new JObject(JObject.Parse(body));
        //    XmlDocument document = JsonConvert.DeserializeXmlNode(obj.ToString());

        //    //send(document)

        //    return Task.FromResult(string.Empty);
        //}

        //public Task<string> MoveCancel(IHttpRequest request)
        //{
        //    var body = Encoding.UTF8.GetString(request.Post.Raw);
        //    JObject obj = new JObject(JObject.Parse(body));
        //    XmlDocument document = JsonConvert.DeserializeXmlNode(obj.ToString());

        //    //send(document)

        //    return Task.FromResult(string.Empty);
        //}

        //public Task<string> MoveUpdate(IHttpRequest request)
        //{
        //    var body = Encoding.UTF8.GetString(request.Post.Raw);
        //    JObject obj = new JObject(JObject.Parse(body));
        //    XmlDocument document = JsonConvert.DeserializeXmlNode(obj.ToString());

        //    //send(document)

        //    return Task.FromResult(string.Empty);
        //}





        // 미사용
        public Task<string> GetItem(IHttpRequest request)
        {
            var id = GetId(request);

            if (_collection.Contains(id))
            {
                return Task.FromResult(id);
            }

            throw GetNotFoundException();
        }
        private static string GetId(IHttpRequest request)
        {
            var id = request.RequestParameters[1];

            return id;
        }
        public Task<string> Update(IHttpRequest request)
        {
            return Post(request);
        }
        public Task<string> Delete(IHttpRequest request)
        {
            var id = GetId(request);

            if (_collection.Remove(id))
            {
                return Task.FromResult(id);
            }

            throw GetNotFoundException();
        }
        private static Exception GetNotFoundException()
        {
            return new HttpException(HttpResponseCode.NotFound, "The resource you've looked for is not found");
        }
        //
    }
}
