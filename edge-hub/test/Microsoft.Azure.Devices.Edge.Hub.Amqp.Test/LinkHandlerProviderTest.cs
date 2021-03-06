// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Hub.Amqp.Test
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Devices.Edge.Hub.Amqp.LinkHandlers;
    using Microsoft.Azure.Devices.Edge.Hub.Core;
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Moq;
    using Xunit;

    [Unit]
    public class LinkHandlerProviderTest
    {
        static IEnumerable<object[]> GetLinkTypeTestData()
        {
            yield return new object[] { "amqps://foo.bar/$cbs", true, LinkType.Cbs, new Dictionary<string, string>() };
            yield return new object[] { "amqps://foo.bar/$cbs", false, LinkType.Cbs, new Dictionary<string, string>() };
            yield return new object[] { "amqps://foo.bar//devices/device1/messages/events", true, LinkType.Events, new Dictionary<string, string> { { "deviceid", "device1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/messages/events", true, LinkType.Events, new Dictionary<string, string> { { "deviceid", "device1" }, { "moduleid", "module1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/messages/events", false, LinkType.ModuleMessages, new Dictionary<string, string> { { "deviceid", "device1" }, { "moduleid", "module1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/messages/deviceBound", false, LinkType.C2D, new Dictionary<string, string> { { "deviceid", "device1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/methods/deviceBound", false, LinkType.MethodSending, new Dictionary<string, string> { { "deviceid", "device1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/methods/deviceBound", false, LinkType.MethodSending, new Dictionary<string, string> { { "deviceid", "device1" }, { "moduleid", "module1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/methods/deviceBound", true, LinkType.MethodReceiving, new Dictionary<string, string> { { "deviceid", "device1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/methods/deviceBound", true, LinkType.MethodReceiving, new Dictionary<string, string> { { "deviceid", "device1" }, { "moduleid", "module1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/twin", false, LinkType.TwinSending, new Dictionary<string, string> { { "deviceid", "device1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/twin", false, LinkType.TwinSending, new Dictionary<string, string> { { "deviceid", "device1" }, { "moduleid", "module1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/twin", true, LinkType.TwinReceiving, new Dictionary<string, string> { { "deviceid", "device1" } } };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/twin", true, LinkType.TwinReceiving, new Dictionary<string, string> { { "deviceid", "device1" }, { "moduleid", "module1" } } };
        }

        [Theory]
        [MemberData(nameof(GetLinkTypeTestData))]
        public void GetLinkTypeTest(string linkUri, bool isReceiver, LinkType expectedLinkType, IDictionary<string, string> expectedBoundVariables)
        {
            // Arrange
            var messageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var twinMessageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var methodMessageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var linkHandlerProvider = new LinkHandlerProvider(messageConverter, twinMessageConverter, methodMessageConverter);

            var amqpLink = Mock.Of<IAmqpLink>(l => l.IsReceiver == isReceiver);
            var uri = new Uri(linkUri);

            // Act
            (LinkType LinkType, IDictionary<string, string> BoundVariables) match = linkHandlerProvider.GetLinkType(amqpLink, uri);

            // Assert
            Assert.Equal(expectedLinkType, match.LinkType);
            Assert.Equal(expectedBoundVariables, match.BoundVariables);
        }

        static IEnumerable<object[]> GetInvalidLinkTypeTestData()
        {
            yield return new object[] { "amqps://foo.bar/$cbs2", true };
            yield return new object[] { "amqps://foo.bar/cbs", false };
            yield return new object[] { "amqps://foo.bar//devices/device1/messages/events", false };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/messages", true };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/messages/foo", false };
            yield return new object[] { "amqps://foo.bar/devices/device1/messages/deviceBound", true };
            yield return new object[] { "amqps://foo.bar/devices/device1/twin2", false };
            yield return new object[] { "amqps://foo.bar/devices/device1/modules/module1/twin/method", true };
        }

        [Theory]
        [MemberData(nameof(GetInvalidLinkTypeTestData))]
        public void GetInvalidLinkTypeTest(string linkUri, bool isReceiver)
        {
            // Arrange
            var messageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var twinMessageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var methodMessageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var linkHandlerProvider = new LinkHandlerProvider(messageConverter, twinMessageConverter, methodMessageConverter);

            var amqpLink = Mock.Of<IAmqpLink>(l => l.IsReceiver == isReceiver);
            var uri = new Uri(linkUri);

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => linkHandlerProvider.GetLinkType(amqpLink, uri));
        }

        static IEnumerable<object[]> GetLinkHandlerTestData()
        {
            yield return new object[] { LinkType.Cbs, true, typeof(CbsLinkHandler) };
            yield return new object[] { LinkType.Cbs, false, typeof(CbsLinkHandler) };
            yield return new object[] { LinkType.Events, true, typeof(EventsLinkHandler) };
            yield return new object[] { LinkType.ModuleMessages, false, typeof(ModuleMessageLinkHandler) };
            yield return new object[] { LinkType.C2D, false, typeof(DeviceBoundLinkHandler) };
            yield return new object[] { LinkType.MethodReceiving, true, typeof(MethodReceivingLinkHandler) };
            yield return new object[] { LinkType.MethodSending, false, typeof(MethodSendingLinkHandler) };
            yield return new object[] { LinkType.TwinReceiving, true, typeof(TwinReceivingLinkHandler) };
            yield return new object[] { LinkType.TwinSending, false, typeof(TwinSendingLinkHandler) };
        }

        [Theory]
        [MemberData(nameof(GetLinkHandlerTestData))]
        public void GetLinkHandlerTest(LinkType linkType, bool isReceiver, Type expectedLinkHandlerType)
        {
            // Arrange
            var messageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var twinMessageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var methodMessageConverter = Mock.Of<IMessageConverter<AmqpMessage>>();
            var linkHandlerProvider = new LinkHandlerProvider(messageConverter, twinMessageConverter, methodMessageConverter);

            var uri = new Uri("amqps://foo.bar//abs/prq");
            var amqpConnection = Mock.Of<IAmqpConnection>(c => c.FindExtension<IConnectionHandler>() == Mock.Of<IConnectionHandler>());
            var amqpSession = Mock.Of<IAmqpSession>(s => s.Connection == amqpConnection);
            IAmqpLink amqpLink = isReceiver
                ? Mock.Of<IReceivingAmqpLink>(l => l.IsReceiver && l.Session == amqpSession)
                : Mock.Of<ISendingAmqpLink>(l => !l.IsReceiver && l.Session == amqpSession) as IAmqpLink;
            if (linkType == LinkType.Cbs)
            {
                Mock.Get(amqpConnection).Setup(c => c.FindExtension<ICbsNode>()).Returns(Mock.Of<ICbsNode>());
            }

            // Act
            ILinkHandler linkHandler = linkHandlerProvider.GetLinkHandler(linkType, amqpLink, uri, new Dictionary<string, string>());

            // Assert
            Assert.NotNull(linkHandler);
            Assert.IsType(expectedLinkHandlerType, linkHandler);
        }
    }
}
