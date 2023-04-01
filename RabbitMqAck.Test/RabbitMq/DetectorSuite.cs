using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit.Abstractions;

namespace RabbitMqAck.Test.RabbitMq
{
    public class DetectorSuite<T> : IDisposable
    {
        public ICollection<RabbitMqDetector<T>> Detectors { get; set; }
        public int MessageCount { get; }

        public DetectorSuite(int detectorCount,
            string queueName,
            bool autoAck,
            ushort prefechCount,
            int messageCount,
            Action<T> processor,
            Action<IModel, BasicDeliverEventArgs> ack,
            Action<IModel, BasicDeliverEventArgs> reject)
        {
            Detectors = new List<RabbitMqDetector<T>>();
            for (int i = 0; i < detectorCount; i++)
            {
                var detector = new RabbitMqDetector<T>(queueName, autoAck, prefechCount, messageCount, processor, ack, reject);
                Detectors.Add(detector);
            }
            MessageCount = messageCount;
        }

        public bool Finished => MessageCount <= Detectors.Sum(d => d.ProcessedCount);

        public void Analyze(ITestOutputHelper output)
        {
            var i = 0;
            foreach (var detector in Detectors)
            {
                output.WriteLine($"Detector {i++}");
                detector.Analyze(output);
            }
        }

        public void Dispose()
        {
            foreach (var detector in Detectors)
            {
                detector.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
