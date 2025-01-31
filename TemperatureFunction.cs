using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public static class TemperatureFunction
{
    private static readonly HttpClient client = new HttpClient();
    private const string WebhookUrl = "https://your-webhook-url.com/notify";
    private const double TempThreshold = 30.0; // Limiar de temperatura

    [Function("LogTemperature")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("TemperatureFunction");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<TemperatureData>(requestBody);

        if (data == null)
        {
            return new BadRequestObjectResult("Invalid data format");
        }

        logger.LogInformation($"Received temperature: {data.Temperature}°C from {data.DeviceId}");

        if (data.Temperature > TempThreshold)
        {
            await SendWebhookNotification(data);
        }

        return new OkObjectResult("Temperature logged successfully");
    }

    private static async Task SendWebhookNotification(TemperatureData data)
    {
        var payload = new { message = $"High temperature alert: {data.Temperature}°C", device = data.DeviceId };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        await client.PostAsync(WebhookUrl, content);
    }
}

public class TemperatureData
{
    public string DeviceId { get; set; }
    public double Temperature { get; set; }
}
