using NUnit.Framework;
using SimpleNetworking.Models;
using SimpleNetworking.OrderingService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Tests.OrderingService
{
    [TestFixture]
    class SimplePacketOrderingServiceTests
    {

        [Test]
        public async Task OrderingServiceShouldReturnPacketIfSequenceNumberIsCorrect()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();
            Packet packet = ConstructPacket(1, "Hello");

            Packet packet1 = (await orderingService.GetNextPacket(packet))[0];

            Assert.AreEqual(1, packet1.PacketHeader.SequenceNumber);
            Assert.AreEqual("Hello", packet1.PacketPayload);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenFirstSequenceNumberIsIncorrect()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();
            Packet packet = ConstructPacket(10, "Hello");

            List<Packet> packets = (await orderingService.GetNextPacket(packet));

            Assert.AreEqual(0, packets.Count);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenSecondSequenceNumberIsIncorrect()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(10, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(20, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenSecondSequenceNumberIsIncorrect1()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(20, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(10, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenThirdSequenceNumberIsIncorrect()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(10, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(20, "Hello")));
            List<Packet> packets2 = (await orderingService.GetNextPacket(ConstructPacket(30, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
            Assert.AreEqual(0, packets2.Count);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenThirdSequenceNumberIsIncorrect1()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(30, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(20, "Hello")));
            List<Packet> packets2 = (await orderingService.GetNextPacket(ConstructPacket(10, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
            Assert.AreEqual(0, packets2.Count);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenThirdSequenceNumberIsIncorrect2()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(10, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(30, "Hello")));
            List<Packet> packets2 = (await orderingService.GetNextPacket(ConstructPacket(20, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
            Assert.AreEqual(0, packets2.Count);
        }

        [Test]
        public async Task OrderingServiceShouldAddPacketToBacklogWhenThirdSequenceNumberIsIncorrect3()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(10, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(30, "Hello")));
            List<Packet> packets2 = (await orderingService.GetNextPacket(ConstructPacket(20, "Hello")));
            List<Packet> packets3 = (await orderingService.GetNextPacket(ConstructPacket(25, "Hello")));
            List<Packet> packets4 = (await orderingService.GetNextPacket(ConstructPacket(15, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
            Assert.AreEqual(0, packets2.Count);
            Assert.AreEqual(0, packets3.Count);
            Assert.AreEqual(0, packets4.Count);
        }

        [Test]
        public async Task OrderingServiceShouldReturnPacketListWhenSequenceNumberIsCorrect()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(2, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(1, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(2, packets1.Count);
        }

        [Test]
        public async Task OrderingServiceShouldReturnPacketListWhenSequenceNumberIsCorrect1()
        {
            IOrderingService orderingService = new SimplePacketOrderingService();

            List<Packet> packets = (await orderingService.GetNextPacket(ConstructPacket(2, "Hello")));
            List<Packet> packets1 = (await orderingService.GetNextPacket(ConstructPacket(4, "Hello")));
            List<Packet> packets2 = (await orderingService.GetNextPacket(ConstructPacket(1, "Hello")));
            List<Packet> packets3 = (await orderingService.GetNextPacket(ConstructPacket(3, "Hello")));

            Assert.AreEqual(0, packets.Count);
            Assert.AreEqual(0, packets1.Count);
            Assert.AreEqual(2, packets2.Count);
            Assert.AreEqual(2, packets3.Count);
        }

        private static Packet ConstructPacket(int sequenceNumber, string data)
        {
            return new Packet
            {
                PacketHeader = new Header
                {
                    SequenceNumber = sequenceNumber
                },
                PacketPayload = data
            };
        }
    }
}
