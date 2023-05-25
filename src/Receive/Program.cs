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

channel.QueueDeclare(queue: "task_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

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
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false ); 
    return Task.CompletedTask;
};

channel.BasicConsume(
    queue: "task_queue",
    autoAck: false,
    consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();