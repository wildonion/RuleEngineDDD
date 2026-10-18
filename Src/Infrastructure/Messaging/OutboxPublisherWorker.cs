using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Messaging;

public sealed class OutboxPublisherWorker(IServiceScopeFactory scopes, ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var publisher = scope.ServiceProvider.GetRequiredService<IActionPublisher>();
                var pending = await outbox.GetPendingAsync(50, stoppingToken);
                if (pending.Count == 0) { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); continue; }
                foreach (var message in pending)
                {
                    try
                    {
                        await publisher.PublishAsync(message.RoutingKey, message.PayloadJson, stoppingToken);
                        await outbox.MarkPublishedAsync(message.Id, DateTimeOffset.UtcNow, stoppingToken);
                        await uow.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogError(ex, "Outbox publish failed for {OutboxId}", message.Id);
                        // Keep pending. Add bounded backoff/dead-letter policy for production.
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Outbox worker iteration failed"); await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
        }
    }
}
