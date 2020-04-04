using NUnit.Framework;
using SimpleNetworking.Models;
using SimpleNetworking.Networking;
using SimpleNetworking.Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
                networkTransport.OnDataReceived += DataReceived;
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
            override public void StartReading()
            {
                // do nothing
            }

            public void InvokeDataReceived(byte[] data)
            {
                RaiseOnDataReceivedEvent(data);
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
            Assert.AreEqual(typeof(MyData).AssemblyQualifiedName.ToString(), packet.PacketHeader.ClassType);
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

        [Test]
        public void ReadShouldReturnCorrectData()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };
            MyData myDataRecieved = null;
            clientConcrete.OnPacketReceived += (packet) =>
            {
                myDataRecieved = (MyData)packet;
            };

            networkTransport.InvokeDataReceived(serializer.Serilize(ConstructPacket(myData, 1)));

            Assert.AreEqual(myData.Name, myDataRecieved.Name);
        }


        private Packet ConstructPacket(object payload, long sequenceNumber)
        {
            return new Packet
            {
                PacketHeader = new Header
                {
                    PacketType = Header.PacketTypes.Data,
                    ClassType = payload.GetType().AssemblyQualifiedName.ToString(),
                    IdempotencyToken = Guid.NewGuid().ToString(),
                    SequenceNumber = sequenceNumber
                },
                PacketPayload = payload
            };
        }
    }
}
