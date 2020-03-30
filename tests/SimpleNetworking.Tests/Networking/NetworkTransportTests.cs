using NUnit.Framework;
using SimpleNetworking.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Tests.Networking
{
    #region Concrete classe for abstract class
    public class ConcreteNetworkTransport : NetworkTransport
    {
        public ConcreteNetworkTransport(Stream stream)
        {
            this.stream = stream;
        }
    }
    #endregion

    [TestFixture]
    class NetworkTransportTests
    {
        [Test]
        public async Task SendShouldAddPacketTypeInHeader()
        {
            MemoryStream stream = new MemoryStream();
            ConcreteNetworkTransport networkTransport = new ConcreteNetworkTransport(stream);

            await networkTransport.SendData(new byte[] { 100, 101 });
            GetHeaderFromStream(stream, out byte packetType, out int length);

            Assert.AreEqual(1, packetType);
        }

        [Test]
        public async Task SendShouldAddPacketLengthInHeader()
        {
            MemoryStream stream = new MemoryStream();
            ConcreteNetworkTransport networkTransport = new ConcreteNetworkTransport(stream);

            await networkTransport.SendData(new byte[] { 100, 101 });
            GetHeaderFromStream(stream, out byte packetType, out int length);

            Assert.AreEqual(1, packetType);
            Assert.AreEqual(2, length);
        }

        [Test]
        public async Task SendShouldWritePayloadAfterHeader()
        {
            MemoryStream stream = new MemoryStream();
            ConcreteNetworkTransport networkTransport = new ConcreteNetworkTransport(stream);

            byte[] payloadSent = new byte[] { 100, 101 };
            await networkTransport.SendData(payloadSent);
            GetHeaderFromStream(stream, out byte packetType, out int length);
            byte[] payloadReceived = GetPayloadFromStream(stream, (int)length);

            Assert.AreEqual(1, packetType);
            Assert.AreEqual(2, length);
            CollectionAssert.AreEqual(payloadSent, payloadReceived);
        }

        [Test]
        public void ReceiveShouldContainCorrectData()
        {
            MemoryStream stream = new MemoryStream();
            ConcreteNetworkTransport networkTransport = new ConcreteNetworkTransport(stream);
            byte[] payloadRecived = null;
            networkTransport.OnDataReceived += (data) => 
            {
                payloadRecived = data;
            };

            byte[] payloadSent = new byte[] { 100, 101 };
            byte[] payload = ConstructPayload(payloadSent);
            stream.Write(payload, 0, payload.Length);
            stream.Position = 0;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            networkTransport.StartReading();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            Thread.Sleep(100);

            CollectionAssert.AreEqual(payloadSent, payloadRecived);
        }

        private void GetHeaderFromStream(Stream stream, out byte packetType, out int length)
        {
            long currentPosition = stream.Position;
            stream.Position = 0;
            byte[] buffer = new byte[1];
            stream.Read(buffer, 0, 1);
            packetType = buffer[0];
            buffer = new byte[sizeof(int)];
            stream.Read(buffer, 0, sizeof(int));
            length = BitConverter.ToInt32(buffer, 0);
            stream.Position = currentPosition;
        }

        private byte[] GetPayloadFromStream(Stream stream, int payloadLength)
        {
            long currentPosition = stream.Position;
            stream.Position = sizeof(byte) + sizeof(int);
            byte[] payload = new byte[payloadLength];
            stream.Read(payload, 0, payloadLength);
            stream.Position = currentPosition;
            return payload;
        }

        private static byte[] ConstructPayload(byte[] data)
        {
            byte[] payload = new byte[sizeof(byte) + sizeof(int) + data.Length];
            payload[0] = 1;
            byte[] lengthInBytes = BitConverter.GetBytes(data.Length);
            Array.Copy(lengthInBytes, 0, payload, 1, lengthInBytes.Length);
            Array.Copy(data, 0, payload, 1 + sizeof(int), data.Length);
            return payload;
        }
    }
}
