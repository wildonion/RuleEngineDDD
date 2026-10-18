using System.Text;
using Application.Interfaces;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public sealed class RabbitMqActionPublisher(IConnection connection) : IActionPublisher
{
    public Task PublishAsync(string routingKey, string jsonPayload, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var channel = connection.CreateModel();
        channel.ConfirmSelect();
        channel.ExchangeDeclare("rule.actions", ExchangeType.Topic, durable: true, autoDelete: false);
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = Guid.NewGuid().ToString("N");
        var body = Encoding.UTF8.GetBytes(jsonPayload);
        channel.BasicPublish("rule.actions", routingKey, mandatory: false, basicProperties: props, body: body);
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
}
