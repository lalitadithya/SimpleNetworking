using Microsoft.Extensions.Logging;
using SimpleNetworking.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
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
        private readonly SemaphoreSlim keepAliveSemaphore = new SemaphoreSlim(1, 1);
        private AutoResetEvent keepAliveAutoResetEvent = new AutoResetEvent(false);
        private bool keepAliveInProgress = false;
        private DateTime lastReadTime = DateTime.UtcNow;

        protected Stream stream;
        protected TcpClient tcpClient;
        protected CancellationToken cancellationToken;
        protected ILogger logger;
        private ILoggerFactory loggerFactory;
        private Timer keepAliveTimer;
        private int keepAliveTimeOut;
        private int keepAliveResponseTimeOut;
        private int numberOfKeepAliveMisses;
        private int maximumNumberOfKeepAliveMisses;

        private enum PacketTypes { KeepAlive = 1, DataPacket = 2 }

        public event DataReceivedHandler OnDataReceived;
        public event ConnectionLostHandler OnConnectionLost;

        public abstract void Connect(string hostname, int port);

        public virtual async Task SendData(byte[] data)
        {
            if (stream.CanWrite)
            {
                byte[] payload = ConstructPayload(data);
                await WriteToStream(payload);
            }
            else
            {
                logger?.LogWarning("Stream is not in a writeable state");
            }
        }

        private async Task WriteToStream(byte[] payload)
        {
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
                    catch (OperationCanceledException)
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
            switch (packetType)
            {
                case PacketTypes.DataPacket:
                    RaiseOnDataReceivedEvent(payload);
                    break;
                case PacketTypes.KeepAlive:
                    string keepAliveMessage = Encoding.Unicode.GetString(payload);
                    if (keepAliveMessage == "PING")
                    {
                        await WriteToStream(ConstructKeepAlivePacket(Encoding.Unicode.GetBytes("PONG")));
                    }
                    else if (keepAliveMessage == "PONG")
                    {
                        keepAliveAutoResetEvent.Set();
                    }
                    break;
            }
        }

        protected void RaiseOnDataReceivedEvent(byte[] payload)
        {
            try
            {
                lastReadTime = DateTime.UtcNow;
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
                if (readResult <= 0)
                {
                    throw new EndOfStreamReachedException();
                }

                offset += readResult;
            } while (offset < length && !cancellationToken.IsCancellationRequested && stream.CanRead);
            return data;
        }

        private async void KeepAlive(object state)
        {
            await keepAliveSemaphore.WaitAsync();
            if (!keepAliveInProgress)
            {
                keepAliveInProgress = true;
                keepAliveSemaphore.Release();

                if ((DateTime.UtcNow - lastReadTime).TotalMilliseconds > keepAliveTimeOut)
                {
                    await WriteToStream(ConstructKeepAlivePacket(Encoding.Unicode.GetBytes("PING")));
                    if (!keepAliveAutoResetEvent.WaitOne(keepAliveResponseTimeOut))
                    {
                        numberOfKeepAliveMisses++;
                        logger?.LogWarning("Keep alive failed {0}/{1}", numberOfKeepAliveMisses, maximumNumberOfKeepAliveMisses);

                        if (numberOfKeepAliveMisses >= maximumNumberOfKeepAliveMisses)
                        {
                            logger?.LogError("Maximum number of keep alive misses exceeded");
                            DropConnection();
                        }
                    }
                    else
                    {
                        numberOfKeepAliveMisses = 0;
                    }
                }
                keepAliveInProgress = false;
            }
            else
            {
                keepAliveSemaphore.Release();
                return;
            }
        }

        private static byte[] ConstructKeepAlivePacket(byte[] data)
        {
            byte[] payload = new byte[sizeof(byte) + sizeof(int) + data.Length];
            payload[0] = (byte)PacketTypes.KeepAlive;
            byte[] lengthInBytes = BitConverter.GetBytes(data.Length);
            Array.Copy(lengthInBytes, 0, payload, 1, lengthInBytes.Length);
            Array.Copy(data, 0, payload, 1 + sizeof(int), data.Length);
            return payload;
        }

        public void StartKeepAlive(int keepAliveTimeOut, int maximumNumberOfKeepAliveMisses, int keepAliveResponseTimeOut)
        {
            this.keepAliveTimeOut = keepAliveTimeOut;
            this.maximumNumberOfKeepAliveMisses = maximumNumberOfKeepAliveMisses;
            this.keepAliveResponseTimeOut = keepAliveResponseTimeOut;
            keepAliveTimer = new Timer(KeepAlive, null, keepAliveTimeOut, keepAliveTimeOut);
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
                    keepAliveTimer?.Dispose();
                    keepAliveSemaphore?.Dispose();
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

        protected void Stop()
        {
            DropConnection();
            tcpClient?.Close();
        }

        protected void Init(ILoggerFactory loggerFactory, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.loggerFactory = loggerFactory;
            numberOfKeepAliveMisses = 0;

            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger(GetType());
            }

            cancellationToken.Register(() => Stop());
        }

        protected void Init(ILoggerFactory loggerFactory, CancellationToken cancellationToken, TcpClient tcpClient, Stream stream = null)
        {
            Init(tcpClient);
            SetStream(stream);
            Init(loggerFactory, cancellationToken);
        }

        protected void Init(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }

        protected void SetStream(Stream stream = null)
        {
            if (stream == null)
            {
                this.stream = tcpClient.GetStream();
            }
            else
            {
                this.stream = stream;
            }
        }
    }
}
