using SimpleNetworking.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.OrderingService
{
    public class SimplePacketOrderingService : IOrderingService
    {
        private readonly List<Packet> packetBacklog;
        private readonly SemaphoreSlim getNextPacketSemaphoreSlim;
        private long nextSequenceNumber;

        public SimplePacketOrderingService()
        {
            packetBacklog = new List<Packet>();
            getNextPacketSemaphoreSlim = new SemaphoreSlim(1, 1);
            nextSequenceNumber = 1;
        }

        public async Task<List<Packet>> GetNextPacket(Packet packet)
        {
            await getNextPacketSemaphoreSlim.WaitAsync();
            try
            {
                List<Packet> result = new List<Packet>();
                if (packet.PacketHeader.SequenceNumber == nextSequenceNumber && packetBacklog.Count == 0)
                {
                    nextSequenceNumber++;
                    result.Add(packet);
                    return result;
                }
                else
                {
                    AddPacketToBacklog(packet);
                }

                GetPacketsFromBacklogToReturn(result);

                return result;
            }
            finally
            {
                getNextPacketSemaphoreSlim.Release();
            }
        }

        private void GetPacketsFromBacklogToReturn(List<Packet> result)
        {
            int numberOfPacketsToRemoveFromBacklog = 0;
            foreach (var packetInBacklog in packetBacklog)
            {
                if (packetInBacklog.PacketHeader.SequenceNumber == nextSequenceNumber)
                {
                    numberOfPacketsToRemoveFromBacklog++;
                    result.Add(packetInBacklog);
                    nextSequenceNumber++;
                }
            }
            packetBacklog.RemoveRange(0, numberOfPacketsToRemoveFromBacklog);
        }

        private void AddPacketToBacklog(Packet packet)
        {
            if (packetBacklog.Count == 0)
            {
                packetBacklog.Add(packet);
            }
            else if (packetBacklog.Count == 1)
            {
                if (packet.PacketHeader.SequenceNumber > packetBacklog[0].PacketHeader.SequenceNumber)
                {
                    packetBacklog.Insert(1, packet);
                }
                else
                {
                    packetBacklog.Insert(0, packet);
                }
            }
            else if (packet.PacketHeader.SequenceNumber > packetBacklog.Last().PacketHeader.SequenceNumber)
            {
                packetBacklog.Insert(packetBacklog.Count, packet);
            }
            else if (packet.PacketHeader.SequenceNumber < packetBacklog.First().PacketHeader.SequenceNumber)
            {
                packetBacklog.Insert(0, packet);
            }
            else
            {
                int indexToInstetAt = 0;
                for (int i = 1; i < packetBacklog.Count; i++)
                {
                    long currentSequenceNumber = packet.PacketHeader.SequenceNumber;
                    if (currentSequenceNumber > packetBacklog[i - 1].PacketHeader.SequenceNumber &&
                        currentSequenceNumber < packetBacklog[i].PacketHeader.SequenceNumber)
                    {
                        indexToInstetAt = i;
                    }
                }
                packetBacklog.Insert(indexToInstetAt, packet);
            }
        }

        #region IDisposable Support
        private bool isDisposed = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    packetBacklog.Clear();
                    getNextPacketSemaphoreSlim.Dispose();
                    nextSequenceNumber = 1;
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
