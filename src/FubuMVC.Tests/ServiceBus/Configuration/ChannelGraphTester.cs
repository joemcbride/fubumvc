﻿using System;
using System.Collections.Generic;
using FubuCore;
using FubuCore.Logging;
using FubuMVC.Core.ServiceBus.Configuration;
using FubuMVC.Core.ServiceBus.Runtime;
using FubuMVC.Core.ServiceBus.Runtime.Headers;
using FubuMVC.Core.ServiceBus.Runtime.Invocation;
using FubuMVC.Core.ServiceBus.Runtime.Serializers;
using Xunit;
using Rhino.Mocks;
using Shouldly;

namespace FubuMVC.Tests.ServiceBus.Configuration
{
    
    public class ChannelGraphTester
    {
        [Fact]
        public void the_default_content_type_should_be_xml_serialization()
        {
            new ChannelGraph().DefaultContentType.ShouldBe(new XmlMessageSerializer().ContentType);
        }

        [Fact]
        public void to_key_by_expression()
        {
            ChannelGraph.ToKey<ChannelSettings>(x => x.Outbound)
                        .ShouldBe("Channel:Outbound");
        }

        [Fact]
        public void channel_for_by_accessor()
        {
            var graph = new ChannelGraph();
            var channelNode = graph.ChannelFor<ChannelSettings>(x => x.Outbound);
            channelNode
                 .ShouldBeTheSameAs(graph.ChannelFor<ChannelSettings>(x => x.Outbound));


            channelNode.Key.ShouldBe("Channel:Outbound");
            channelNode.SettingAddress.Name.ShouldBe("Outbound");

        }

        [Fact]
        public void reading_settings()
        {
            var channel = new ChannelSettings
            {
                Outbound = new Uri("channel://outbound"),
                Downstream = new Uri("channel://downstream")
            };

            var bus = new BusSettings
            {
                Outbound = new Uri("bus://outbound"),
                Downstream = new Uri("bus://downstream")
            };

            var services = new InMemoryServiceLocator();
            services.Add(channel);
            services.Add(bus);

            var graph = new ChannelGraph();
            graph.ChannelFor<ChannelSettings>(x => x.Outbound);
            graph.ChannelFor<ChannelSettings>(x => x.Downstream);
            graph.ChannelFor<BusSettings>(x => x.Outbound);
            graph.ChannelFor<BusSettings>(x => x.Downstream);

            graph.ReadSettings(services);

            graph.ChannelFor<ChannelSettings>(x => x.Outbound)
                 .Uri.ShouldBe(channel.Outbound);
            graph.ChannelFor<ChannelSettings>(x => x.Downstream)
                 .Uri.ShouldBe(channel.Downstream);
            graph.ChannelFor<BusSettings>(x => x.Outbound)
                .Uri.ShouldBe(bus.Outbound);
            graph.ChannelFor<BusSettings>(x => x.Downstream)
                .Uri.ShouldBe(bus.Downstream);
        }


        public class FakeChannel : IChannel
        {
            public void Dispose()
            {
            }

            public Uri Address { get; private set; }
            public ReceivingState Receive(IReceiver receiver)
            {
                ReceivedCount++;
                return ReceivingState.CanContinueReceiving;
            }

            public int ReceivedCount { get; set; }

            public void Send(byte[] data, IHeaders headers)
            {
            }
        }


    }

    public class ChannelSettings
    {
        public Uri Outbound { get; set; }
        public Uri Downstream { get; set; }
        public Uri Upstream { get; set; }

        public int UpstreamCount { get; set; }
        public int OutboundCount { get; set; }
    }

    public class BusSettings
    {
        public Uri Outbound { get; set; }
        public Uri Downstream { get; set; }
        public Uri Upstream { get; set; }
    }
}