using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace RabbitMqAck.Test.RabbitMq
{
    public class RabbitMqDetector<T> : IDisposable
    {
        private readonly IModel _channel;
        private readonly IConnection _connection;
        public ICollection<DetectorAction> Actions;

        public RabbitMqDetector(string queueName,
            bool autoAck,
            ushort prefetchCount,
            int messageCount,
            Action<T> processor,
            Action<IModel, BasicDeliverEventArgs> ack,
            Action<IModel, BasicDeliverEventArgs> reject)
        {
            MessageCount = messageCount;
            Processor = processor;
            Ack = ack;
            Reject = reject;
            Actions = new HashSet<DetectorAction>();

            var factory = new ConnectionFactory();
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queueName, false, false, true);
            _channel.BasicQos(0, prefetchCount, false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += HandleMessage;

            _channel.BasicConsume(consumer, queueName, autoAck);
        }

        public bool Finished => MessageCount <= ProcessedCount;
        public int ProcessedCount { get; private set; }
        public int MessageCount { get; private set; }
        public Action<T> Processor { get; }
        public Action<IModel, BasicDeliverEventArgs> Ack { get; }
        public Action<IModel, BasicDeliverEventArgs> Reject { get; }

        private void HandleMessage(object? sender, BasicDeliverEventArgs args)
        {
            Actions.Add(new DetectorAction(args.DeliveryTag, "Message Recieved", 1));
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<T>(body);

            if (message != null)
            {
                Actions.Add(new DetectorAction(args.DeliveryTag, "Before Process", 2));
                Processor(message);
                Actions.Add(new DetectorAction(args.DeliveryTag, "After Process", 2));
                Actions.Add(new DetectorAction(args.DeliveryTag, "Before Ack", 3));
                Ack(_channel, args);
                Actions.Add(new DetectorAction(args.DeliveryTag, "After Ack", 4));
            }
            else
            {
                Actions.Add(new DetectorAction(args.DeliveryTag, "Before Reject", 5));
                Reject(_channel, args);
                Actions.Add(new DetectorAction(args.DeliveryTag, "Before Rejcet", 6));
            }

            ProcessedCount++;
            Actions.Add(new DetectorAction(args.DeliveryTag, "Message Handled", 7));
        }

        public void Analyze(ITestOutputHelper output)
        {
            var starts = Actions.Where(a => a.Step == 1);
            var ends = Actions.Where(a => a.Step == 7);

            var processTimes = starts.Join(ends, s => s.DeliveryTag, e => e.DeliveryTag,
                (s, e) => (e.CreatedDateTime - s.CreatedDateTime).TotalMilliseconds);

            var waitForMessageTimes = starts.Join(ends, s => s.DeliveryTag, e => e.DeliveryTag + 1,
                (s, e) => (s.CreatedDateTime - e.CreatedDateTime).TotalMilliseconds);

            if (!processTimes.Any())
            {
                output.WriteLine("Insufficent Process Data");
            }
            else
            {
                output.WriteLine($"Mean Process(ms): {processTimes.Sum() / processTimes.Count()}");
                output.WriteLine($"Max Process(ms): {processTimes.Max()}");
                output.WriteLine($"Min Process(ms): {processTimes.Min()}");
            }
            if (!waitForMessageTimes.Any())
            {
                output.WriteLine("Insufficent WaitForMessage Data");
            }
            else
            {
                output.WriteLine($"Mean WaitForMessage(ms): {waitForMessageTimes.Sum() / waitForMessageTimes.Count()}");
                output.WriteLine($"Max WaitForMessage(ms): {waitForMessageTimes.Max()}");
                output.WriteLine($"Min WaitForMessage(ms): {waitForMessageTimes.Min()}");
            }
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
