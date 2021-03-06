﻿// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture, Explicit]
    public class AdvancedApiTransientQueueTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test, Explicit]
        public void Does_transient_queue_cause_channel_to_close_after_consuming_one_message()
        {
            var queue = bus.Advanced.QueueDeclare();

            bus.Advanced.Consume(queue, (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    var message = Encoding.UTF8.GetString(body);
                    Console.Out.WriteLine("message = '{0}'", message);
                }));

            Thread.Sleep(5000);

            using (var channel = bus.Advanced.OpenPublishChannel())
            {
                var body = Encoding.UTF8.GetBytes("Publish 1");
                channel.Publish(Exchange.GetDefault(), queue.Name, new MessageProperties(), body);
            }

            Thread.Sleep(5000);

            using (var channel = bus.Advanced.OpenPublishChannel())
            {
                var body = Encoding.UTF8.GetBytes("Publish 2");
                channel.Publish(Exchange.GetDefault(), queue.Name, new MessageProperties(), body);
            }

            Thread.Sleep(1000);
        }
    }
}

// ReSharper restore InconsistentNaming