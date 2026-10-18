using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace Infrastructure.Messaging;

public sealed class MqttTelemetryWorker(IConfiguration configuration, IServiceScopeFactory scopes,
    ILogger<MqttTelemetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        using var client = factory.CreateMqttClient();
        var host = configuration["Mqtt:Host"] ?? "localhost";
        var port = int.TryParse(configuration["Mqtt:Port"], out var parsed) ? parsed : 1883;
        var options = new MqttClientOptionsBuilder().WithClientId("rule-engine-telemetry-worker")
            .WithTcpServer(host, port).WithCleanSession().Build();

        client.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var topic = e.ApplicationMessage.Topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (topic.Length != 3 || topic[0] != "devices" || topic[2] != "telemetry") return;
                var deviceId = topic[1];
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.AsSpan());
                var telemetry = JsonSerializer.Deserialize<TelemetryMessage>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (telemetry is null || string.IsNullOrWhiteSpace(telemetry.EventId))
                { logger.LogWarning("Ignoring invalid telemetry on {Topic}", e.ApplicationMessage.Topic); return; }
                using var scope = scopes.CreateScope();
                var evaluator = scope.ServiceProvider.GetRequiredService<EvaluateDeviceService>();
                await evaluator.ExecuteAsync(deviceId, telemetry, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            { logger.LogError(ex, "Telemetry processing failed"); }
        };

        await client.ConnectAsync(options, stoppingToken);
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic("devices/+/telemetry")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
        await client.SubscribeAsync(subscribeOptions, stoppingToken);
        logger.LogInformation("Subscribed to devices/+/telemetry at MQTT {Host}:{Port}", host, port);
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        if (client.IsConnected) await client.DisconnectAsync();
    }
}
