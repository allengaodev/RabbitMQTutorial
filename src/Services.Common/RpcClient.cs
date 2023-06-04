using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Services.Common;

public class RpcClient : IDisposable
{
    private const string QUEUE_NAME = "rpc_queue";

    private readonly IRabbitMQPersistentConnection _connection;
    private readonly IModel _channel;
    private readonly string _replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();

    public RpcClient(IRabbitMQPersistentConnection connection)
    {
        _connection = connection;

        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        _channel = _connection.CreateModel();
        _replyQueueName = _channel.QueueDeclare("replyQueue", false, false, false).QueueName;

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                return Task.CompletedTask;
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(response);

            return Task.CompletedTask;
        };

        _channel.BasicConsume(consumer: consumer,
            queue: _replyQueueName,
            autoAck: true);
    }

    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        IBasicProperties props = _channel.CreateBasicProperties();
        var correlationId = Guid.NewGuid().ToString();
        props.CorrelationId = correlationId;
        props.ReplyTo = _replyQueueName;
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var tcs = new TaskCompletionSource<string>();
        _callbackMapper.TryAdd(correlationId, tcs);

        _channel.BasicPublish(exchange: string.Empty,
            routingKey: QUEUE_NAME,
            basicProperties: props,
            body: messageBytes);

        Console.WriteLine($" [x] Sent {message}");

        cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));
        return tcs.Task;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}