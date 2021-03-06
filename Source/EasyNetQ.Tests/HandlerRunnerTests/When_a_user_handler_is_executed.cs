﻿// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Loggers;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.HandlerRunnerTests
{
    [TestFixture]
    public class When_a_user_handler_is_executed
    {
        private IHandlerRunner handlerRunner;

        byte[] deliveredBody = null;
        MessageProperties deliveredProperties = null;
        MessageReceivedInfo deliveredInfo = null;

        readonly MessageProperties messageProperties = new MessageProperties
            {
                CorrelationId = "correlation_id"
            };
        readonly MessageReceivedInfo messageInfo = new MessageReceivedInfo("consumer_tag", 123, false, "exchange", "routingKey", "queue");
        readonly byte[] messageBody = new byte[0];

        private IModel channel;

        private bool postAckCallbackWasRun;

        [SetUp]
        public void SetUp()
        {
            postAckCallbackWasRun = false;

            //var logger = new ConsoleLogger();
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();
            var consumerErrorStrategy = MockRepository.GenerateStub<IConsumerErrorStrategy>();

            handlerRunner = new HandlerRunner(logger, consumerErrorStrategy);

            Func<byte[], MessageProperties, MessageReceivedInfo, Task> userHandler = (body, properties, info) => 
                Task.Factory.StartNew(() =>
                    {
                        deliveredBody = body;
                        deliveredProperties = properties;
                        deliveredInfo = info;
                    });

            var consumer = MockRepository.GenerateStub<IBasicConsumer>();
            channel = MockRepository.GenerateStub<IModel>();
            consumer.Stub(x => x.Model).Return(channel).Repeat.Any();

            var context = new ConsumerExecutionContext(
                userHandler, messageInfo, messageProperties, messageBody, consumer);

            context.SetPostAckCallback(() => postAckCallbackWasRun = true);

            var autoResetEvent = new AutoResetEvent(false);
            ((HandlerRunner) handlerRunner).SynchronisationAction = () => autoResetEvent.Set();

            handlerRunner.InvokeUserMessageHandler(context);

            autoResetEvent.WaitOne(1000);
        }

        [Test]
        public void Should_deliver_body()
        {
            deliveredBody.ShouldBeTheSameAs(messageBody);
        }

        [Test]
        public void Should_deliver_properties()
        {
            deliveredProperties.ShouldBeTheSameAs(messageProperties);
        }

        [Test]
        public void Should_deliver_info()
        {
            deliveredInfo.ShouldBeTheSameAs(messageInfo);
        }

        [Test]
        public void Should_ACK_message()
        {
            channel.AssertWasCalled(x => x.BasicAck(123, false));
        }

        [Test]
        public void Should_run_PostAckCallback()
        {
            postAckCallbackWasRun.ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming