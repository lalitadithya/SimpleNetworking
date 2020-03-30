using NUnit.Framework;
using SimpleNetworking.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            GetHeaderFromStream(stream, out byte packetType, out long length);

            Assert.AreEqual(1, packetType);
        }

        [Test]
        public async Task SendShouldAddPacketLengthInHeader()
        {
            MemoryStream stream = new MemoryStream();
            ConcreteNetworkTransport networkTransport = new ConcreteNetworkTransport(stream);

            await networkTransport.SendData(new byte[] { 100, 101 });
            GetHeaderFromStream(stream, out byte packetType, out long length);

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
            GetHeaderFromStream(stream, out byte packetType, out long length);
            byte[] payloadReceived = GetPayloadFromStream(stream, (int)length);

            Assert.AreEqual(1, packetType);
            Assert.AreEqual(2, length);
            CollectionAssert.AreEqual(payloadSent, payloadReceived);
        }

        private void GetHeaderFromStream(Stream stream, out byte packetType, out long length)
        {
            long currentPosition = stream.Position;
            stream.Position = 0;
            byte[] buffer = new byte[1];
            stream.Read(buffer, 0, 1);
            packetType = buffer[0];
            buffer = new byte[sizeof(long)];
            stream.Read(buffer, 0, sizeof(long));
            length = BitConverter.ToInt64(buffer, 0);
            stream.Position = currentPosition;
        }

        private byte[] GetPayloadFromStream(Stream stream, int payloadLength)
        {
            long currentPosition = stream.Position;
            stream.Position = sizeof(byte) + sizeof(long);
            byte[] payload = new byte[payloadLength];
            stream.Read(payload, 0, payloadLength);
            stream.Position = currentPosition;
            return payload;
        }
    }
}
