﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworking.Networking
{
    public delegate void DataReceivedHandler(byte[] data);
    public delegate void ConnectionLostHandler();
    public abstract class NetworkTransport
    {
        protected Stream stream;
        protected CancellationToken cancellationToken;
        protected ILogger logger;

        private enum PacketTypes { KeepAlive = 0, DataPacket = 1 }

        public event DataReceivedHandler OnDataReceived;
        public event ConnectionLostHandler OnConnectionLost;

        public virtual async Task SendData(byte[] data)
        {
            if (stream.CanWrite)
            {
                byte[] payload = ConstructPayload(data);
                try
                {
                    await stream.WriteAsync(payload);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Sending data failed");
                    DropConnection();
                }
            }
            else
            {
                logger?.LogWarning("Sream is not in a writeable state");
            }
        }

        private static byte[] ConstructPayload(byte[] data)
        {
            byte[] payload = new byte[sizeof(byte) + sizeof(int) + data.Length];
            payload[0] = (byte)PacketTypes.DataPacket;
            byte[] lengthInBytes = BitConverter.GetBytes(data.Length);
            Array.Copy(lengthInBytes, 0, payload, 1, lengthInBytes.Length);
            Array.Copy(data, 0, payload, 1 + sizeof(int), data.Length);
            return payload;
        }

        public virtual void StartReading()
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && stream.CanRead)
                {
                    try
                    {
                        await ListenLoop();
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "Listen loop failed");
                        DropConnection();
                        break;
                    }
                }
            });
        }

        private async Task ListenLoop()
        {
            int headerLength = sizeof(byte) + sizeof(int);

            byte[] header = await ReadData(headerLength);
            PacketTypes packetType = (PacketTypes)header[0];
            int payloadSize = BitConverter.ToInt32(header, 1);

            byte[] payload = await ReadData(payloadSize);

            RaiseOnDataReceivedEvent(payload);
        }

        protected void RaiseOnDataReceivedEvent(byte[] payload)
        {
            try
            {
                OnDataReceived?.Invoke(payload);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "OnDataReceived threw an exception");
            }
        }

        private async Task<byte[]> ReadData(int length)
        {
            byte[] data = new byte[length];
            int offset = 0;
            do
            {
                offset += await stream.ReadAsync(data, offset, length - offset, cancellationToken);
            } while (offset < length && !cancellationToken.IsCancellationRequested);
            return data;
        }

        protected void DropConnection()
        {
            try
            {
                stream?.Close();
                stream?.Dispose();
                OnConnectionLost?.Invoke();
            }
            catch (Exception e)
            {
                logger?.LogWarning(e, "Drop connection");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DropConnection();
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
