using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Protocols.Channels
{
    /// <summary>
    /// TCP 기반 통신 채널 공급자
    /// </summary>
    public class TcpChannelProvider : ChannelProvider
    {
        private readonly TcpListener _tcpListener;
        private readonly Dictionary<Guid, WeakReference<TcpChannel>> _channels = new Dictionary<Guid, WeakReference<TcpChannel>>();
        private CancellationTokenSource _cts;
        private readonly object _lock = new();
        /// <summary>
        /// 로컬 IP 주소
        /// </summary>
        public IPAddress IPAddress { get; }
        /// <summary>
        /// TCP Port
        /// </summary>
        public int Port { get; }

        public TcpChannelProvider()
            :this(502)
        {
            
        }
        public TcpChannelProvider(int port)
            :this(IPAddress.Any, port) 
        {
            
        }
        public TcpChannelProvider(IPAddress iPAddress, int port)            
        {
            IPAddress = iPAddress;
            Port = port;
            _tcpListener = new TcpListener(IPAddress, Port);
        }
        public override IReadOnlyList<Channel> Channels
        {
            get
            {
                IReadOnlyList<Channel> result = null;
                lock (_channels)
                {
                    result = _channels.Values.Select(w => w.TryGetTarget(out var channel) ? channel : null).Where(c => c != null).ToList();
                }
                return result;
            }
        }

        public override string Description => _tcpListener?.LocalEndpoint?.ToString();

        public override void Dispose()
        {
            lock(_lock)
            {
                if(!IsDisposed)
                {
                    IsDisposed = true;
                    Stop();
                }
            }
        }

        public override void Start()
        {
            lock (_lock)
            {
                if(IsDisposed)
                    throw new ObjectDisposedException(nameof(TcpChannelProvider));

                _cts = new CancellationTokenSource();
                _tcpListener.Start();

                Task.Run(() =>
                {
                    while (_cts.Token.IsCancellationRequested == false)
                    {
                        try
                        {
                            var tcpClient = _tcpListener.AcceptTcpClient();
                            lock(_channels)
                            {
                                var channel = new TcpChannel(this, tcpClient)
                                {
                                    Logger = Logger
                                };
                                Logger?.Log(new ChannelOpenEventLog(channel));
                                _channels[channel.Guid] = new WeakReference<TcpChannel>(channel);
                                RaiseCreatedEvent(new ChannelCreatedEventArgs(channel));

                                foreach(var disposed in _channels.Where(c => c.Value == null || c.Value.TryGetTarget(out var target)).Select(c => c.Key).ToArray())
                                {
                                    _channels.Remove(disposed);
                                }
                            }
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                },_cts.Token);
            }
        }

        public override void Stop()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _tcpListener?.Stop();

                lock (_channels)
                {
                    foreach (var reference in _channels.Values.ToArray())
                    {
                        if(reference.TryGetTarget(out var target))
                        {
                            target.Dispose();
                        }
                    }
                    _channels.Clear();
                }
            }
        }
        internal void RemoveChannel(Guid guid)
        {
            lock (_channels)
                _channels?.Remove(guid);
        }
    }
}
