using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Services.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEventBus();
var provider = builder.Services.BuildServiceProvider();
var connection = provider.GetService<IRabbitMQPersistentConnection>();

if (!connection.IsConnected)
{
    connection.TryConnect();
}

using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "rpc_queue",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);
channel.BasicConsume(
    queue: "rpc_queue",
    autoAck: false,
    consumer: consumer);

consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var props = ea.BasicProperties;
    var replyProps = channel.CreateBasicProperties();
    var message = Encoding.UTF8.GetString(body);
    replyProps.CorrelationId = props.CorrelationId;

    try
    {
        Console.WriteLine($" [x] Received {message}");
        int dots = message.Split('.').Length - 1;
        Thread.Sleep(dots * 1000);
        Console.WriteLine($" [x] {message} Done");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
    finally
    {
        var responseBytes = Encoding.UTF8.GetBytes(message + "Success!");
        channel.BasicPublish(exchange: string.Empty,
            routingKey: props.ReplyTo,
            basicProperties: replyProps,
            body: responseBytes);
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }

    return Task.CompletedTask;
};

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();