using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SimpleNetworking.IdempotencyService;
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
            public ClientConcrete(NetworkTransport networkTransport, ISerializer serializer, int? packetResendTime = null)
            {
                this.networkTransport = networkTransport;
                this.serializer = serializer;
                this.idempotencyService = new SimpleIdempotencyService<Guid, Packet>(10);
                networkTransport.OnDataReceived += DataReceived;
                if (packetResendTime.HasValue)
                {
                    StartPacketResend(packetResendTime.Value);
                }
            }

            public new void StopPacketResend()
            {
                base.StopPacketResend();
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

        [Test]
        public void ClientShouldSendAckForEveryPacketReceived()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            clientConcrete.OnPacketReceived += (packet) =>
            {
                // do nothing
            };

            Packet data = ConstructPacket(myData, 1);
            networkTransport.InvokeDataReceived(serializer.Serilize(data));

            Thread.Sleep(2 * 1000);

            Packet ackRecieved = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));

            Assert.AreEqual(data.PacketHeader.IdempotencyToken, ackRecieved.PacketHeader.IdempotencyToken);
        }

        [Test]
        public async Task ClientShouldNotRaiseEventForAckPacket()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            bool dataReceived = false;
            clientConcrete.OnPacketReceived += (packet) =>
            {
                dataReceived = true;
            };


            await clientConcrete.SendData(myData);
            Packet sentPacket = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));

            Packet ackPacket = ConstructAckPacket(sentPacket.PacketHeader.SequenceNumber, sentPacket.PacketHeader.IdempotencyToken);
            networkTransport.InvokeDataReceived(serializer.Serilize(ackPacket));

            Thread.Sleep(2 * 1000);

            Assert.IsFalse(dataReceived);
        }

        [Test]
        public async Task ClientShouldResendPackets()
        {
            NetworkTransportMock networkTransport = new NetworkTransportMock();
            JsonSerializer serializer = new JsonSerializer();
            ClientConcrete clientConcrete = new ClientConcrete(networkTransport, serializer, 10 * 1000);
            MyData myData = new MyData
            {
                Name = "John Doe"
            };

            bool dataReceived = false;
            clientConcrete.OnPacketReceived += (packet) =>
            {
                dataReceived = true;
            };

            await clientConcrete.SendData(myData);

            Packet sentPacket = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));
            Thread.Sleep(15 * 1000);
            Packet sentPacket1 = (Packet)serializer.Deserilize(networkTransport.Data, typeof(Packet));

            clientConcrete.StopPacketResend();

            Assert.AreEqual(sentPacket.PacketHeader.IdempotencyToken, sentPacket1.PacketHeader.IdempotencyToken);
            Assert.AreEqual(sentPacket.PacketHeader.SequenceNumber, sentPacket1.PacketHeader.SequenceNumber);
            Assert.AreEqual(sentPacket.PacketHeader.ClassType, sentPacket1.PacketHeader.ClassType);
            Assert.AreEqual(((MyData)(sentPacket.PacketPayload as JObject).ToObject(typeof(MyData))).Name,
                ((MyData)(sentPacket1.PacketPayload as JObject).ToObject(typeof(MyData))).Name);
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

        private Packet ConstructAckPacket(long sequenceNumber, string idempotencyToken)
        {
            return new Packet
            {
                PacketHeader = new Header
                {
                    PacketType = Header.PacketTypes.Ack,
                    ClassType = null,
                    IdempotencyToken = idempotencyToken,
                    SequenceNumber = sequenceNumber
                },
                PacketPayload = null
            };
        }
    }
}
