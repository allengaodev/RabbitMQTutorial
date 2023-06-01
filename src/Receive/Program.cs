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

channel.ExchangeDeclare("header_exchange", ExchangeType.Headers);
var queueName = channel.QueueDeclare().QueueName;
var header = new Dictionary<string, object>()
{
    { "group", "vip" },
    { "level", "1" },
    { "x-match", "any" }
};
channel.QueueBind(queueName, "header_exchange", string.Empty, header);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");
    int dots = message.Split('.').Length - 1;
    Thread.Sleep(dots * 1000);
    Console.WriteLine(" [x] Done");
    return Task.CompletedTask;
};

channel.BasicConsume(
    queue: queueName,
    autoAck: true,
    consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();