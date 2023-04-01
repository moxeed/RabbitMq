using RabbitMQ.Client;
using System.Text.Json;

namespace RabbitMqAck.Test.RabbitMq
{
    public class RabbitMqSender : IDisposable
    {
        private readonly IModel _channel;
        private readonly IConnection _connection;
        public RabbitMqSender(string queueName)
        {
            Exchange = Guid.NewGuid().ToString();

            var factory = new ConnectionFactory();
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(Exchange, "direct", false, true);
            _channel.QueueDeclare(queueName, false, false, true);
            _channel.QueueBind(queueName, Exchange, "test");
        }

        public string Exchange { get; }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Send<T>(T data)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(data);
            _channel.BasicPublish(Exchange, "test", null, body);
        }

        public void Send<T>(IEnumerable<T> data)
        {
            foreach (var item in data)
            {
                Send(item);
            }
        }
    }
}
