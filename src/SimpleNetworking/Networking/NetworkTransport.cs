using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworking.Networking
{
    public delegate void DataReceivedHandler(byte[] data);
    public abstract class NetworkTransport
    {
        protected Stream stream;
        private enum PacketTypes { KeepAlive = 0, DataPacket = 1 }

        public event DataReceivedHandler OnDataReceived;

        public async Task SendData(byte[] data)
        {
            byte[] payload = ConstructPayload(data);
            await stream.WriteAsync(payload);
        }

        private static byte[] ConstructPayload(byte[] data)
        {
            byte[] payload = new byte[sizeof(byte) + sizeof(long) + data.LongLength];
            payload[0] = (byte)PacketTypes.DataPacket;
            byte[] lengthInBytes = BitConverter.GetBytes(data.LongLength);
            Array.Copy(lengthInBytes, 0, payload, 1, lengthInBytes.LongLength);
            Array.Copy(data, 0, payload, 1 + sizeof(long), data.LongLength);
            return payload;
        }

        public void StartReading()
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~NetworkTransport()
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
