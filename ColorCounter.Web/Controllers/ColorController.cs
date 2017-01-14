namespace ColorCounter.Web.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using ColorCounter.Interfaces;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;

    [ServiceRequestActionFilter]
    public class ColorController : ApiController
    {
        private static readonly Uri ServiceUri = new Uri("fabric:/RemindableActors/ColorCounter");

        private readonly CancellationTokenSource cts = new CancellationTokenSource();


        [HttpGet]
        public async Task<HttpResponseMessage> Hello()
        {
            return this.Request.CreateResponse(HttpStatusCode.OK, "submitted");
        }

        [HttpGet]
        public async Task<HttpResponseMessage> CountPixels(string actorId, string colorName)
        {
            try
            {
                var colorCounterActor = ActorProxy.Create<IColorCounter>(new ActorId(actorId), ServiceUri);
                var token = this.cts.Token;
                await colorCounterActor.CountPixels(colorName, token);
                return this.Request.CreateResponse(HttpStatusCode.OK, "submitted");
            }
            catch (Exception e)
            {
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, e.ToString());
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetPixelCount(string actorId)
        {
            try
            {
                var colorCounterActor = ActorProxy.Create<IColorCounter>(new ActorId(actorId), ServiceUri);
                var token = this.cts.Token;
                var result = await colorCounterActor.GetPixelCount(token);
                return this.Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception e)
            {
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, e.ToString());
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> SubmitImage(string actorId, Uri uri)
        {
            try
            {
                var colorCounterActor = ActorProxy.Create<IColorCounter>(new ActorId(actorId), ServiceUri);
                var token = this.cts.Token;
                await colorCounterActor.SetImage(uri, token);
                return this.Request.CreateResponse(HttpStatusCode.OK, "submitted");
            }
            catch (Exception e)
            {
                return this.Request.CreateResponse(HttpStatusCode.InternalServerError, e.ToString());
            }
        }
    }
}