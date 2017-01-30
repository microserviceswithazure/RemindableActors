using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ColorCounter.Website.Controllers
{
    using System.Fabric;
    using System.Net;
    using System.Net.Http;
    using System.Threading;

    using ColorCounter.Website.Model;

    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    using Newtonsoft.Json.Linq;

    public class HomeController : Controller
    {

        private static readonly Uri CounterServiceUri =
        new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/Web");

        private readonly long defaultPartitionID = 0;


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Result(string userName)
        {
            return View(new User { UserName = userName });
        }

        public async Task<string> GetCount(string userName)
        {
            try
            {
                var tokenSource = new CancellationTokenSource();
                var servicePartitionResolver = ServicePartitionResolver.GetDefault();
                var httpClient = new HttpClient();
                var partition =
                    await
                        servicePartitionResolver.ResolveAsync(
                            CounterServiceUri,
                            new ServicePartitionKey(),
                            tokenSource.Token);
                var ep = partition.GetEndpoint();
                var addresses = JObject.Parse(ep.Address);
                var primaryReplicaAddress = (string)addresses["Endpoints"].First;
                var pixelResult =
                    await httpClient.GetAsync(
                        $"{primaryReplicaAddress}/api/Pixel?actorId={userName}", tokenSource.Token);
                return pixelResult.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public IActionResult Error()
        {
            return View();
        }

        public async Task<IActionResult> TriggerActor()
        {
            try
            {
                var tokenSource = new CancellationTokenSource();
                var servicePartitionResolver = ServicePartitionResolver.GetDefault();
                var name = Request.Form["name"].ToString();
                var color = Request.Form["color"].ToString();
                var imagePath = Request.Form["imagePath"].ToString();
                var httpClient = new HttpClient();
                var partition =
                    await
                        servicePartitionResolver.ResolveAsync(
                            CounterServiceUri,
                            new ServicePartitionKey(),
                            tokenSource.Token);
                var ep = partition.GetEndpoint();
                var addresses = JObject.Parse(ep.Address);
                var primaryReplicaAddress = (string)addresses["Endpoints"].First;
                var result = await httpClient.PostAsync($"{primaryReplicaAddress}/api/Image?actorId={name}&uri={WebUtility.HtmlEncode(imagePath)}", null, tokenSource.Token);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    // send second request.
                    var pixelResult =
                        await httpClient.PostAsync(
                            $"{primaryReplicaAddress}/api/Pixel?actorId={name}&colorName={WebUtility.HtmlEncode(color)}",
                            null,
                            tokenSource.Token);
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        return RedirectToAction("Result", new { userName = name });
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return Error();
        }
    }
}
