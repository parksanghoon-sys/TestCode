using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Protocols.Channels
{
    public class TcpChannel : Channel
    {
        internal Guid Guid { get; }
        private readonly TcpChannelProvider provider;
        private readonly byte[] buffer = new byte[8192];

        private TcpClient tcpClient = null;
        private Stream stream = null;
        private readonly object connectLock = new object();
        private readonly object writeLock = new object();
        private readonly object readLock = new object();
        private readonly Queue<byte> readBuffer = new Queue<byte>();
        private string description;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// 채널 설명
        /// </summary>
        public override string Description { get => description; }

        public string Host { get; }
        public int Port { get; }
        public int ConnectTimeout { get; }
        public bool IsConnected
        {
            get
            {
                lock (connectLock)
                {
                    return tcpClient != null && tcpClient.Connected;
                }
            }
        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="host">호스트</param>
        /// <param name="port">포트</param>
        public TcpChannel(string host, int port) : this(host, port, 10000) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="host">호스트</param>
        /// <param name="port">포트</param>
        /// <param name="connectTimeout">연결 제한시간(밀리초)</param>
        public TcpChannel(string host, int port, int connectTimeout)
        {
            Host = host;
            Port = port;
            ConnectTimeout = connectTimeout;
            description = $"{host}:{port}";
        }
        public TcpChannel(TcpChannelProvider provider, TcpClient tcpClient)
        {
            Guid = Guid.NewGuid();

            this.provider = provider;
            this.tcpClient = tcpClient;
            stream = tcpClient.GetStream();
            description = tcpClient.Client.RemoteEndPoint.ToString();
        }
        ~TcpChannel()
        {
            Dispose();
        }
        public override void Dispose()
        {
            if (!IsDisposed)
            {
                provider?.RemoveChannel(Guid);
                IsDisposed = true;
                Close();
            }
        }
        private void Close()
        {
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (Exception)
            {

            }
            finally
            {
                cancellationTokenSource = new CancellationTokenSource();
                lock (connectLock)
                {
                    if (tcpClient != null)
                    {
                        Logger?.Log(new ChannelCloseEventLog(this));
                        tcpClient.Close();
                        tcpClient = null;
                    }
                }
            }
        }
        private void CheckConnection(bool isWriting)
        {
            if (provider != null) return;

            lock (connectLock)
            {
                if (IsDisposed == false && tcpClient == null)
                {
                    tcpClient = new TcpClient();
                    try
                    {
                        Task task = tcpClient.ConnectAsync(Host ?? string.Empty, Port);
                        if (task.Wait(ConnectTimeout, cancellationTokenSource.Token) == false)                        
                            throw new SocketException(10060);

                        stream = tcpClient.GetStream();
                        description = tcpClient!.Client!.RemoteEndPoint!.ToString()!;
                        Logger?.Log(new ChannelOpenEventLog(this));
                    }
                    catch (Exception ex)
                    {
                        tcpClient?.Client?.Dispose();
                        tcpClient = null;
                        if(isWriting == false)
                            Logger?.Log(new ChannelErrorLog(this, ex));
                        throw ex.InnerException ?? ex;
                    }
                }
            }
        }
        private byte? GetByte(int timeout)
        {
            lock (readLock)
            {
                if (readBuffer.Count == 0)
                {
                    try
                    {
                        CheckConnection(false);
                        if (tcpClient != null)
                        {
                            int received = 0;
                            if (timeout == 0)
                                received = stream.Read(buffer, 0, buffer.Length);
                            else
                            {
                                var task = stream.ReadAsync(buffer, 0, buffer.Length);
                                if (task.Wait(timeout))
                                    received = task.Result;
                            }

                            for (int i = 1; i < received; i++)
                                readBuffer.Enqueue(buffer[i]);

                            if (received == 0)
                            {
                                var socket = tcpClient?.Client;
                                if (socket == null || socket.Available == 0 && socket.Poll(1000, SelectMode.SelectRead))
                                {
                                    throw new Exception();
                                }
                            }
                            else return buffer[0];
                        }
                    }
                    catch (Exception)
                    {
                        Close();
                    }
                    return null;
                }
                else
                    return readBuffer.Dequeue();
            }
        }
        public override void Write(byte[] bytes)
        {
            CheckConnection(true);
            lock(writeLock)
            {
                try
                {
                    if (tcpClient?.Client?.Connected == true)
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush();
                    }
                }
                catch (Exception ex )
                {
                    Close();
                    throw ex.InnerException ?? ex;
                }
            }
        }
        /// <summary>
        /// 1 바이트 읽기
        /// </summary>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트</returns>
        public override byte Read(int timeout)
        {
            lock (readLock)
            {
                return GetByte(timeout) ?? throw new TimeoutException();
            }
        }
        /// <summary>
        /// 여러 개의 바이트 읽기
        /// </summary>
        /// <param name="count">읽을 개수</param>
        /// <param name="timeout">제한시간(밀리초)</param>
        /// <returns>읽은 바이트 열거</returns>
        public override IEnumerable<byte> Read(uint count, int timeout)
        {
            lock (readLock)
            {
                for (int i = 0; i < count; i++)
                {
                    yield return GetByte(timeout) ?? throw new TimeoutException();
                }
            }
        }
        /// <summary>
        /// 채널에 남아있는 모든 바이트 읽기
        /// </summary>
        /// <returns>읽은 바이트 열거</returns>
        public override IEnumerable<byte> ReadAllRemain()
        {
            lock (readLock)
            {
                while (readBuffer.Count > 0)
                    yield return readBuffer.Dequeue();

                if (tcpClient == null)
                    yield break;

                byte[] receivedBuffer = new byte[4096];
                int available = 0;

                try
                {
                    available = tcpClient.Client.Available;
                }
                catch { }

                while (available > 0)
                {
                    int received = 0;
                    try
                    {
                        received = stream.Read(receivedBuffer, 0, receivedBuffer.Length);
                    }
                    catch { }
                    for (int i = 0; i < received; i++)
                        yield return receivedBuffer[i];

                    try
                    {
                        available = tcpClient.Client.Available;
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 수신 버퍼에 있는 데이터의 바이트 수입니다.
        /// </summary>
        public override uint BytesToRead
        {
            get
            {
                uint available = 0;

                try
                {
                    available = (uint)tcpClient.Client.Available;
                }
                catch { }
                return (uint)readBuffer.Count + available;
            }
        }
    }
}
