using RabbitMQ.Client;

namespace Services.Common;
public interface IRabbitMQPersistentConnection : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    IModel CreateModel();
}
