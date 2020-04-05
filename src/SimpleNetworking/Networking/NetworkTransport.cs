using Microsoft.Extensions.Logging;
using SimpleNetworking.Exceptions;
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
        protected bool connectionDroppedEventRaised = false;
        private SemaphoreSlim dropConnectionSemaphore = new SemaphoreSlim(1, 1);

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
                    await stream.WriteAsync(payload, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    DropConnection();
                }
                catch (Exception e)
                {
                    logger?.LogError("SendData failed - {0}", e.Message);
                    DropConnection();
                }
            }
            else
            {
                logger?.LogWarning("Stream is not in a writeable state");
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
                    catch(OperationCanceledException)
                    {
                        DropConnection();
                        break;
                    }
                    catch (Exception e)
                    {
                        logger?.LogError("ListenLoop failed - {0}", e.Message);
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
                int readResult = await stream.ReadAsync(data, offset, length - offset, cancellationToken);
                if(readResult <= 0)
                {
                    throw new EndOfStreamReachedException();
                }

                offset += readResult;
            } while (offset < length && !cancellationToken.IsCancellationRequested && stream.CanRead);
            return data;
        }

        public void DropConnection()
        {
            dropConnectionSemaphore.Wait();
            if (!connectionDroppedEventRaised)
            {
                try
                {
                    stream?.Close();
                    stream?.Dispose();
                    OnConnectionLost?.Invoke();
                    connectionDroppedEventRaised = true;
                }
                catch (Exception e)
                {
                    logger?.LogWarning(e, "Drop connection");
                }
                finally
                {
                    dropConnectionSemaphore.Release();
                }
            }
        }
    }
}
