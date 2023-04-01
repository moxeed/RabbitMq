using RabbitMqAck.Test.RabbitMq;
using Xunit.Abstractions;

namespace RabbitMqAck.Test
{
    public class AutoAckTest
    {
        private readonly ITestOutputHelper _output;

        public AutoAckTest(ITestOutputHelper output)
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
        public async Task AutoAck_SendMessage_PerfomanceEvaludated(ushort prefetchCount, int messageCount)
        {
            var queueName = nameof(AutoAck_SendMessage_PerfomanceEvaludated);

            using var detector = new RabbitMqDetector<int>(queueName, true, prefetchCount, messageCount,
                (data) => { },
                (channel, args) => { },
                (channel, args) => { }
                );

            using var sender = new RabbitMqSender(queueName);
            sender.Send(Enumerable.Range(0, messageCount));

            while (!detector.Finished) await Task.Delay(100);

            detector.Analyze(_output);
        }

        [Theory]
        [InlineData(2, 1, 1)]
        [InlineData(2, 1, 100)]
        [InlineData(2, 1, 10000)]
        [InlineData(4, 10, 1)]
        [InlineData(4, 10, 100)]
        [InlineData(4, 10, 10000)]
        [InlineData(8, 100, 1)]
        [InlineData(8, 100, 100)]
        [InlineData(8, 100, 10000)]
        public async Task MultipleDetectorsAutoAck_SendMessage_PerfomanceEvaludated(int detectorCount, ushort prefetchCount, int messageCount)
        {
            var queueName = nameof(MultipleDetectorsAutoAck_SendMessage_PerfomanceEvaludated);

            using var detector = new DetectorSuite<int>(detectorCount, queueName, false, prefetchCount, messageCount,
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