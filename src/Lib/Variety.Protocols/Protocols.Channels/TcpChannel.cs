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
        ~TcpChannel()
        {
            Dispose();
        }
        public override void Dispose()
        {
            if(!IsDisposed)
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
}
