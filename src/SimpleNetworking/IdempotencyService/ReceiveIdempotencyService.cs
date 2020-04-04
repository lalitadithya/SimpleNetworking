using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SimpleNetworking.IdempotencyService
{
    public class ReceiveIdempotencyService<T> : IReceiveIdempotencyService<T>
    {
        private readonly ConcurrentDictionary<T, DateTime> receivedPackets;
        private readonly Timer expiryTimer;
        private readonly int expiryTime;
        private readonly SemaphoreSlim cacheRemovalSemaphore = new SemaphoreSlim(1, 1);

        private bool isCacheRemovalInProgress;
        private bool isPacketExpiryPaused;

        private const int timerBuffer = 5 * 1000;

        public ReceiveIdempotencyService(int expiryTime)
        {
            this.expiryTime = expiryTime;
            receivedPackets = new ConcurrentDictionary<T, DateTime>();
            isCacheRemovalInProgress = false;
            isPacketExpiryPaused = false;
            expiryTimer = new Timer(RemoveExpiredItems, null, expiryTime, expiryTime + timerBuffer);
        }

        public bool Add(T value)
        {
            return receivedPackets.TryAdd(value, DateTime.UtcNow);
        }

        public bool Remove(T value)
        {
            return receivedPackets.TryRemove(value, out _);
        }

        public bool Find(T value)
        {
            return receivedPackets.ContainsKey(value);
        }

        private async void RemoveExpiredItems(object state)
        {
            await cacheRemovalSemaphore.WaitAsync();
            if (!isCacheRemovalInProgress)
            {
                isCacheRemovalInProgress = true;
                cacheRemovalSemaphore.Release();
                ICollection<T> keys = receivedPackets.Keys;
                foreach (var key in keys)
                {
                    if (receivedPackets.TryGetValue(key, out DateTime addedTime) && (DateTime.Now - addedTime).TotalMilliseconds > expiryTime)
                    {
                        if (isPacketExpiryPaused)
                        {
                            receivedPackets.TryUpdate(key, DateTime.UtcNow, addedTime);
                        }
                        else
                        {
                            receivedPackets.TryRemove(key, out _);
                        }
                    }
                }
                isCacheRemovalInProgress = false;
            }
            else
            {
                cacheRemovalSemaphore.Release();
                return;
            }
        }

        public void PausePacketExpiry()
        {
            isPacketExpiryPaused = true;
        }

        public void ResumePacketExpiry()
        {
            isPacketExpiryPaused = false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    expiryTimer.Dispose();
                    cacheRemovalSemaphore.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ReceiveIdempotencyService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
