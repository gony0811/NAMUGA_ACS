using ACS.Communication.Http.uHttpSharp;
using ACS.Communication.Http.uHttpSharp.Attributes;
using ACS.Communication.Http.uHttpSharp.Controllers;
using ACS.Communication.Http.uHttpSharp.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Http.Controllers
{
    public class EmptyPipeline : IPipeline
    {
        public Task<IControllerResponse> Go(Func<Task<IControllerResponse>> injectedTask, IHttpContext context)
        {
            return injectedTask();
        }
    }

    public class JsonController : IController
    {
        public class Question
        {
            public string TheQuestion { get; set; }
        }
        public JsonController(int id)
        {
        }

        [HttpMethod(HttpMethods.Post)]
        public IControllerResponse Post([FromBody] Question question)
        {
            return Response.Render(HttpResponseCode.Ok, question).Result;
        }
        public IPipeline Pipeline
        {
            get { return new EmptyPipeline(); }
        }
    }
    public class MyController
    {
        private readonly int _id;
        public MyController(int id)
        {
            _id = id;
        }
        public MyController()
        {
        }

        [HttpMethod(HttpMethods.Post)]
        public Task<IControllerResponse> Post([FromPost("a")] MyRequest request, [FromHeaders("header")]string hello, [FromQuery("query")]string world)
        {
            return Response.Render(HttpResponseCode.Ok, null);
        }

        [Indexer]
        public Task<object> Get(IHttpContext context, int id)
        {
            return Task.FromResult<object>(new MyController(id));
        }
    }
    public class MyRequest : IValidate
    {
        public int A { get; set; }
        public void Validate(IErrorContainer container)
        {
            if (A == 0)
            {
                container.Log("A cannot be zero");
            }
        }
    }
    class BaseController : IController
    {
        [HttpMethod(HttpMethods.Get)]
        public Task<IControllerResponse> Get()
        {
            return Response.Render(HttpResponseCode.Ok, new { Hello = "Base!", Kaki = Enumerable.Range(0, 10000) });
        }

        [HttpMethod(HttpMethods.Post)]
        public Task<IControllerResponse> Post([FromBody] MyRequest a)
        {
            return Response.Render(HttpResponseCode.Ok, a);
        }

        public virtual IPipeline Pipeline
        {
            get { return new EmptyPipeline(); }
        }

        public IController Derived
        {
            get { return new DerivedController(); }
        }
    }

    class DerivedController : BaseController
    {
        [HttpMethod(HttpMethods.Get)]
        public new Task<IControllerResponse> Get()
        {
            return Response.Render(HttpResponseCode.Ok, new { Hello = "Derived!" });
        }

        [HttpMethod(HttpMethods.Get)]
        public Task<IControllerResponse> GetAGV1([FromQuery("SYSTEM")] string a, [FromQuery("IF_NAME")] string b, [FromQuery("EQPID")] string c, [FromQuery("LOCATION")] string d,
            [FromQuery("STATUS")] string e, [FromQuery("BOXID")] string f, [FromQuery("BOXLABEL")] string g, [FromQuery("GLASSID")] string h)
        {
            MyModel myModel = new MyModel
            {
                System = a,
                IF_Name = b,
                EqpId = c,
                Location = d,
                Status = e,
                BoxId = f,
                BoxLabel = g,
                GlassId = h,
            };

            return Response.Render(HttpResponseCode.Ok, new { Hello = "Derived!" });
        }

        [Indexer(0)]
        public Task<IController> Indexer(IHttpContext context, int hey)
        {
            return Task.FromResult<IController>(this);
        }


        [Indexer(1)]
        public Task<IController> Indexer(IHttpContext context, string hey)
        {
            return Task.FromResult<IController>(this);
        }
    }


    class MyModel
    {
        //public int MyProperty
        //{
        //    get;
        //    set;
        //}

        //public MyModel Model
        //{
        //    get;
        //    set;
        //}

        public string System { get; set; }
        public string IF_Name { get; set; }
        public string EqpId { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string BoxId { get; set; }
        public string BoxLabel { get; set; }
        public string GlassId { get; set; }

    }

    class MoveCMD
    {
        public string Command { get; set; }
        public string DestSubject { get; set; }
        public string ReplySubject { get; set; }
        public string CmdType { get; set; }
        public string FinalEQP { get; set; }
        public string JobID { get; set; }
        public string CarrID { get; set; }
        public string BatchID { get; set; }
        public string Priority { get; set; }
        public string SourceEQP { get; set; }
        public string SourcePort { get; set; }
        public string TransEQP { get; set; }
        public string UserID { get; set; }
    }

    class MoveCancel
    {
        public string Command { get; set; }
        public string DestSubject { get; set; }
        public string ReplySubject { get; set; }
        public string EqpID { get; set; }
        public string JobID { get; set; }
        public string FinalLoc { get; set; }
        public string FinalPort { get; set; }
        public string SourceLoc { get; set; }
        public string SourcePort { get; set; }
        public string DestSubject_2 { get; set; }
        public string Type { get; set; }
        public string UserID { get; set; }
    }

    class MoveUpdate
    {
        public string Command { get; set; }
        public string DestSubject { get; set; }
        public string ReplySubject { get; set; }
        public string JobID { get; set; }
        public string Type { get; set; }
        public string EqpID { get; set; }
        public string SourceLoc { get; set; }
        public string SourcePort { get; set; }
        public string FinalLoc { get; set; }
        public string FinalPort { get; set; }
        public string UserID { get; set; }
        public string Description { get; set; }
        public string DestSubject_2 { get; set; }
    }
}
