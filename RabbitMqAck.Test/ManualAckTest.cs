using RabbitMqAck.Test.RabbitMq;
using Xunit.Abstractions;

namespace RabbitMqAck.Test
{
    public class ManualAckTest
    {
        private readonly ITestOutputHelper _output;

        public ManualAckTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 100)]
        [InlineData(1, 10000)]
        [InlineData(10, 1)]
        [InlineData(10, 100)]
        [InlineData(10, 10000)]
        [InlineData(100, 1)]
        [InlineData(100, 100)]
        [InlineData(100, 10000)]
        public async Task ManualAck_SendMessage_PerfomanceEvaludated(ushort prefetchCount, int messageCount)
        {
            var queueName = nameof(ManualAck_SendMessage_PerfomanceEvaludated);

            using var detector = new RabbitMqDetector<int>(queueName, false, prefetchCount, messageCount,
                (data) => { },
                (channel, args) => channel.BasicAck(args.DeliveryTag, false),
                (channel, args) => channel.BasicReject(args.DeliveryTag, false)
                );

            using var sender = new RabbitMqSender(queueName);
            sender.Send(Enumerable.Range(0, messageCount));

            while (!detector.Finished) await Task.Delay(100);

            detector.Analyze(_output);
        }
    }
}