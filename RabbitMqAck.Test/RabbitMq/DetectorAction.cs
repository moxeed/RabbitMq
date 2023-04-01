using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqAck.Test.RabbitMq
{
    public class DetectorAction
    {
        public DateTime CreatedDateTime;
        public ulong DeliveryTag;
        public string Message;
        public ushort Step;
        public DetectorAction(ulong deliveryTag, string message, ushort step)
        {
            CreatedDateTime = DateTime.Now;
            DeliveryTag = deliveryTag;
            Message = message;
            Step = step;
        }
    }
}
