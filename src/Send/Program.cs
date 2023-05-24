using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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
channel.ConfirmSelect();
channel.QueueDeclare(queue: "task_queue",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null);

var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);

var properties = channel.CreateBasicProperties();
properties.Persistent = true; // persistent

channel.BasicPublish(exchange: string.Empty,
    routingKey: "task_queue",
    mandatory: true,
    basicProperties: properties,
    body: body);

channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
Console.WriteLine($" [x] Sent {message}");

Console.WriteLine(" Press [enter] to exit.");

static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
}