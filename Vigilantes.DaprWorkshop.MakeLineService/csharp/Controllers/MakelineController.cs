using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vigilantes.DaprWorkshop.MakeLineService.Models;

namespace Vigilantes.DaprWorkshop.MakeLineService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MakelineController : ControllerBase
    {
        #region  Data members

        private readonly ILogger<MakelineController> _logger;
        private readonly HttpClient _httpClient;
        private IServiceManager _serviceManager;

        #endregion

        public MakelineController(IHttpClientFactory httpClientFactory,
                                  ILogger<MakelineController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpPost("/orders")]
        public async Task<IActionResult> MakeOrder(CloudEvent cloudEvent)
        {
            // Deserialize incoming order summary
            var orderSummary = ((JToken)cloudEvent.Data).ToObject<OrderSummary>();
            _logger.LogInformation("Received Order: {@OrderSummary}", orderSummary);

            var orderItemSummaries = new List<OrderSummary>();

            var response = await _httpClient.GetAsync($"http://localhost:5280/v1.0/state/orders/{orderSummary.StoreId}");
            if(response.StatusCode != HttpStatusCode.NoContent && response.IsSuccessStatusCode)
            {
                try
                {
                    orderItemSummaries = JsonConvert.DeserializeObject<List<OrderSummary>>(await response.Content.ReadAsStringAsync());
                }
                catch
                {
                    _logger.LogInformation("Deserialize failed: {@X}", await response.Content.ReadAsStringAsync());
                }
            }

            orderItemSummaries.Add(orderSummary);
            var newOrderItemSummaries = new KeyValuePair<string,List<OrderSummary>>(orderSummary.StoreId, orderItemSummaries);
            
  
            response = await _httpClient.PostAsync($"http://localhost:5280/v1.0/state/orders", new StringContent($"[{JsonConvert.SerializeObject(newOrderItemSummaries)}]", null, "application/json"));
            response.EnsureSuccessStatusCode();

            
            // TODO: Challenge 6 - Call the SignalR output binding with the incoming order summary


            return Ok();
        }


        [HttpGet("/orders/{storeId}")]
        public async Task<IActionResult> GetAllOrders(string storeId)
        {
            var response = await _httpClient.GetAsync($"http://localhost:5280/v1.0/state/orders/{storeId}");
            response.EnsureSuccessStatusCode();

            return Ok(await response.Content.ReadAsStringAsync());
        }


        [HttpDelete("/orders/{storeId}/{orderId}")]
        public async Task<IActionResult> CompleteOrder(string storeId, Guid orderId)
        {                
            var orderItemSummaries = new List<OrderSummary>();

            var response = await _httpClient.GetAsync($"http://localhost:5280/v1.0/state/orders/{storeId}");
            response.EnsureSuccessStatusCode();
            if(response.StatusCode != HttpStatusCode.NoContent && response.IsSuccessStatusCode)
            {
                try
                {
                    orderItemSummaries = JsonConvert.DeserializeObject<List<OrderSummary>>(await response.Content.ReadAsStringAsync());
                }
                catch
                {
                    _logger.LogInformation("Deserialize failed: {@X}", await response.Content.ReadAsStringAsync());
                }
            }

            var newOrderItemSummaries = new KeyValuePair<string,List<OrderSummary>>(storeId, orderItemSummaries.Where(o => o.OrderId != orderId).ToList());
            
  
            response = await _httpClient.PostAsync($"http://localhost:5280/v1.0/state/orders", new StringContent($"[{JsonConvert.SerializeObject(newOrderItemSummaries)}]", null, "application/json"));
            response.EnsureSuccessStatusCode();

            return Ok();
        }


        // TODO: Challenge 4 - Implement a method to get all orders
        //                   - Implement a method to delete an order
        [EnableCors("CorsPolicy")]
        [HttpPost("/negotiate")]
        public async Task<ActionResult> Negotiate()
        {
            // This method will be called by the signalr client when connecting to
            // the azure signalr service. It will return an access token and the 
            // endpoint details for the client to use when sending and receiving events.            
            if (_serviceManager == null)
            {
                var connectionString = await GetSignalrConnectionString();
                if (string.IsNullOrEmpty(connectionString)) return new OkResult();

                _serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = connectionString)
                .Build();
            }

            return new JsonResult(new Dictionary<string, string>()
            {
                { "url", _serviceManager.GetClientEndpoint("orders") },
                { "accessToken", _serviceManager.GenerateClientAccessToken("orders", "vigilantes.ui") }
            });
        }

        private async Task<IActionResult> SendOrderUpdate(string eventName, OrderSummary orderSummary)
        {
            // TODO: Challenge 6 - Send event to SignalR output binding
            // References: 
            //    https://github.com/dapr/docs/tree/master/howto/send-events-with-output-bindings
            //    https://github.com/dapr/docs/blob/master/reference/specs/bindings/signalr.md 
            //    Option: use the OrderSummaryUpdateData object
            
            return new OkResult();
        }

        private async Task<string> GetSignalrConnectionString()
        {
            // TODO: Challenge 6 - Call the secrets component to retrieve the connection string
            // Reference: https://github.com/dapr/docs/blob/master/reference/api/secrets_api.md
            return "";
        }

    }
}
