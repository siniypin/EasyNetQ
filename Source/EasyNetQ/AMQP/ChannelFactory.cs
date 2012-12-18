using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.AMQP
{
    public class ChannelFactory : IChannelFactory
    {
        public IChannel OpenChannel(IConnection connection, ChannelSettings settings)
        {
            try
            {
                var model = connection.CreateModel();
                if (settings.PublisherConfirmsOn)
                {
                    model.ConfirmSelect();
                }
                model.BasicQos(0, settings.PrefetchCount, false);
                return new Channel(model);
            }
            catch (OperationInterruptedException operationInterruptedException)
            {
                throw new EasyNetQAmqpException(operationInterruptedException, operationInterruptedException.Message);
            }
        }
    }
}