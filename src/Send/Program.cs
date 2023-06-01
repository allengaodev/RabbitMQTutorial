using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
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

var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);

var properties = channel.CreateBasicProperties();
properties.Headers = new Dictionary<string, object>()
{
    {"group", "vip"},
    {"level", "2"},
};

channel.BasicPublish(exchange: "header_exchange",
    routingKey: string.Empty,
    mandatory: true,
    basicProperties: properties,
    body: body);

channel.BasicReturn += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine(message);
    Console.WriteLine(" No Binding Queue");
    Console.WriteLine(" [x] Done");
};

Console.WriteLine($" [x] Sent {message}");
Console.WriteLine(" Press [enter] to exit.");

static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
}