using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNative.CloudEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Vigilantes.DaprWorkshop.ReceiptGenerationService.Models;

namespace Vigilantes.DaprWorkshop.ReceiptGenerationService.Controllers
{
    [ApiController]
    public class ReceiptGenerationConsumerController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReceiptGenerationConsumerController> _logger;

        public ReceiptGenerationConsumerController(IHttpClientFactory httpClientFactory, ILogger<ReceiptGenerationConsumerController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpPost]
        [Route("/orders")]
        public async Task<IActionResult> GenerateReceipt(CloudEvent cloudEvent)
        {
            var orderSummary = ((JToken)cloudEvent.Data).ToObject<OrderSummary>();

            _logger.LogInformation("Writing Order Summary (receipt) to storage: {@OrderSummary}", orderSummary);

            var receipt = new
            {
                operation = "create",
                data = orderSummary,
                metadata = new { key = orderSummary.OrderId }
            };

            var response = await _httpClient.PostAsync($"http://localhost:5380/v1.0/bindings/receipts", new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(receipt), null, "application/json"));
            response.EnsureSuccessStatusCode();


            return Ok();
        }
    }
}
