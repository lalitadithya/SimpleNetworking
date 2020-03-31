using NUnit.Framework;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Tests.Client
{
    [TestFixture]
    public class ClientTests
    {
        #region Concrete classes and mocks

        public class ClientConcrete : SimpleNetworking.Client.Client
        {
            public ClientConcrete(NetworkTransport networkTransport, ISerializer serializer)
            {
                this.networkTransport = networkTransport;
                this.serializer = serializer;
            }
        }

        public class NetworkTransportMock : NetworkTransport
        {
            public byte[] Data { get; set; }
            override public Task SendData(byte[] data)
            {
                Data = data;
                return Task.FromResult(true);
            }
        }

        public class MyData
        {
            public string Name { get; set; }
        }

        #endregion

        [Test]
        public async Task ClassTypeInHeaderShouldMatchTypeOf()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            await clientConcrete.SendData(myData);

            Packet packet = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Assert.AreEqual(typeof(MyData).ToString(), packet.PacketHeader.ClassType);
        }

        [Test]
        public async Task IdempotencyTokenShouldBeValidGuid()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            await clientConcrete.SendData(myData);

            Packet packet = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Assert.IsTrue(Guid.TryParse(packet.PacketHeader.IdempotencyToken, out _));
        }

        [Test]
        public async Task SequenceNumberShouldStartAtOne()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            await clientConcrete.SendData(myData);

            Packet packet = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Assert.AreEqual(1, packet.PacketHeader.SequenceNumber);
        }

        [Test]
        public async Task SequenceNumberShouldIncreaseByOneWithEachSend()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            await clientConcrete.SendData(myData);
            Packet packet = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Assert.AreEqual(1, packet.PacketHeader.SequenceNumber);

            await clientConcrete.SendData(myData);
            packet = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Assert.AreEqual(2, packet.PacketHeader.SequenceNumber);

            await clientConcrete.SendData(myData);
            packet = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Assert.AreEqual(3, packet.PacketHeader.SequenceNumber);
        }
    }
}
