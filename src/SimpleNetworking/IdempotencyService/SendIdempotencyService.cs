using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.IdempotencyService
{
    public class SendIdempotencyService<K, T> : ISendIdempotencyService<K, T>
    {
        private readonly int maximumNumberOfPackets;
        private readonly ConcurrentDictionary<K, T> packets;
        private readonly AutoResetEvent maximumPacketLimitExceededEvent;
        private readonly SemaphoreSlim addSemaphoreSlim;

        public SendIdempotencyService(int maximumNumberOfPackets)
        {
            this.maximumNumberOfPackets = maximumNumberOfPackets;
            packets = new ConcurrentDictionary<K, T>();
            maximumPacketLimitExceededEvent = new AutoResetEvent(false);
            addSemaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public async Task<bool> Add(K token, T item)
        {
            await addSemaphoreSlim.WaitAsync();
            try
            {
                if (maximumNumberOfPackets == packets.Count)
                {
                    await Task.Run(() => maximumPacketLimitExceededEvent.WaitOne());
                }

                return packets.TryAdd(token, item);
            }
            finally
            {
                addSemaphoreSlim.Release();
            }
        }

        public bool Remove(K token, out T item)
        {
            bool removeResult = packets.TryRemove(token, out item);
            if(removeResult && !maximumPacketLimitExceededEvent.WaitOne(0))
            {
                maximumPacketLimitExceededEvent.Set();
            }
            return removeResult;
        }

        public IEnumerable<T> GetValues()
        {
            ICollection<K> keys = packets.Keys;
            foreach (K key in keys)
            {
                if(packets.TryGetValue(key, out T value))
                {
                    yield return value;
                }
            }
        }
    }
}
