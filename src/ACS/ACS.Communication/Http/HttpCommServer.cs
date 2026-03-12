using ACS.Communication.Http.uHttpSharp;
using ACS.Communication.Http.uHttpSharp.Listeners;
using ACS.Communication.Http.uHttpSharp.RequestProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Http.Handler;
using ACS.Communication.Http.uHttpSharp.Handlers;
using System.Dynamic;
using ACS.Communication.Http.Controllers;
using ACS.Communication.Http.uHttpSharp.Handlers.Compression;
using log4net;
using ACS.Workflow;

namespace ACS.Communication.Http
{
    public class HttpCommServer
    {

        public ILog logger = LogManager.GetLogger(typeof(HttpCommServer));
        public string httpServerListenIP { get; set; }
        public string httpServerListenPort { get; set; }

        public IWorkflowManager WorkflowManager { get; set; }


        //public HostHttpInterfaceServiceEx HostHttpInterfaceService { get; set; }
        public void Start()
        {
            try
            {
                var httpServer = new HttpServer(new HttpRequestProvider());

                httpServer.Use(new TcpListenerAdapter(new TcpListener(string.Compare(httpServerListenIP, "any", true) !=0 ? IPAddress.Parse(httpServerListenIP) : IPAddress.Any, Int32.Parse(httpServerListenPort))));

                /*** Add Handler.. Start ***/
                //exception : Server Error
                httpServer.Use(new ExceptionHandler());

                //session, cookies
                httpServer.Use(new SessionHandler<dynamic>(() => new ExpandoObject(), TimeSpan.FromMinutes(20)));

                //compression
                httpServer.Use(new CompressionHandler(DeflateCompressor.Default, GZipCompressor.Default));

                //authorization 
                //httpServer.Use(new BasicAuthenticationHandler("Hohoho", "username", "password5"));

                //control
                //httpServer.Use(new ControllerHandler(new DerivedController(), new ModelBinder(new ObjectActivator()), new JsonView()));
                //httpServer.Use(new ControllerHandler(new BaseController(), new JsonModelBinder(), new JsonView(), StringComparer.OrdinalIgnoreCase));
                httpServer.Use(new HttpRouter().With("acs_mod", new RestHandler<string>(new StringsRestController(WorkflowManager), JsonResponseProvider.Default)));

                //Error : Client Request Message Error (Not Found Handler)
                httpServer.Use(new ErrorHandler());
                /*** Add Handler.. End ***/

                httpServer.Start();

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
    }
}
