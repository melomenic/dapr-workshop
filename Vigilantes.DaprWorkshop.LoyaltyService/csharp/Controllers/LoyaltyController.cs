using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vigilantes.DaprWorkshop.LoyaltyService.Models;

namespace Vigilantes.DaprWorkshop.LoyaltyService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoyaltyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LoyaltyController> _logger;

        public LoyaltyController(IHttpClientFactory httpClientFactory, ILogger<LoyaltyController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpPost("/orders")]
        public async Task<IActionResult> UpdateLoyalty(CloudEvent cloudEvent)
        {
            var orderSummary = ((JToken)cloudEvent.Data).ToObject<OrderSummary>();
            _logger.LogInformation("Received Order Summary: {@OrderSummary}", orderSummary);

            var newLoyaltySummaryRecord = new KeyValuePair<string,LoyaltySummary>(
                orderSummary.LoyaltyId,
                new LoyaltySummary 
                {
                    CustomerName = orderSummary.CustomerName,
                    LoyaltyId = orderSummary.LoyaltyId,
                    PointsEarned = (int)Math.Floor(orderSummary.OrderTotal * 10),
                    PointTotal = (int)Math.Floor(orderSummary.OrderTotal * 10)
                });

            var response = await _httpClient.GetAsync($"http://localhost:5480/v1.0/state/loyalty_points/{orderSummary.LoyaltyId}");
       
            if(response.StatusCode != HttpStatusCode.NoContent && response.IsSuccessStatusCode)
            {
                LoyaltySummary loyaltySummary;
                try
                {
                    loyaltySummary = JsonConvert.DeserializeObject<LoyaltySummary>(await response.Content.ReadAsStringAsync());
                    newLoyaltySummaryRecord.Value.PointTotal = loyaltySummary.PointTotal + newLoyaltySummaryRecord.Value.PointsEarned;
                }
                catch
                {
                    _logger.LogInformation("Deserialize failed: {@X}", await response.Content.ReadAsStringAsync());
                }
            }

            response = await _httpClient.PostAsync($"http://localhost:5480/v1.0/state/loyalty_points", new StringContent($"[{JsonConvert.SerializeObject(newLoyaltySummaryRecord)}]", null, "application/json"));

            _logger.LogInformation("Loyalty Updated: {@LoyaltySummary}", newLoyaltySummaryRecord);
            _logger.LogInformation("Loyalty Update Status: {@StatusCode}", response.StatusCode);
            _logger.LogInformation("Loyalty Update Message: {@Message}", await response.Content.ReadAsStringAsync());


            response = await _httpClient.GetAsync($"http://localhost:5480/v1.0/secrets/vault/dapr");
            _logger.LogInformation("And here's a secret: {@Message}", await response.Content.ReadAsStringAsync());


            return Ok(response.StatusCode);
        }
    }
}
