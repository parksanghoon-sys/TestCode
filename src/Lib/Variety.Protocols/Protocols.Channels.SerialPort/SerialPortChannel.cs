using System;
using System.Collections.Generic;
#if NETSTANDARD2_0
using RJCP.IO.Ports;
#else
using System.IO.Ports;
#endif
using System.Threading;
using System.Threading.Tasks;
using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;

namespace Protocols.Channels
{
    public class SerialPortChannel : Channel
    {
        /// <summary>
        /// 포트 이름
        /// </summary>
        public string PortName { get => SerialPort.PortName; }

        /// <summary>
        /// Baud Rate
        /// </summary>
        public int BaudRate { get => SerialPort.BaudRate; }

        /// <summary>
        /// Data Bits
        /// </summary>
        public int DataBits { get => SerialPort.DataBits; }

        /// <summary>
        /// Stop Bits
        /// </summary>
        public StopBits StopBits { get => SerialPort.StopBits; }

        /// <summary>
        /// Parity
        /// </summary>
        public Parity Parity { get => SerialPort.Parity; }

        /// <summary>
        /// Handshake
        /// </summary>
        public Handshake Handshake { get => SerialPort.Handshake; }

        /// <summary>
        /// DTR 활성화 여부
        /// </summary>
        public bool DtrEnable { get => SerialPort.DtrEnable; set => SerialPort.DtrEnable = value; }

        /// <summary>
        /// RTS 활성화 여부
        /// </summary>
        public bool RtsEnable { get => SerialPort.RtsEnable; set => SerialPort.RtsEnable = value; }

        /// <summary>
        /// 채널 설명
        /// </summary>
        public override string Description { get => description; }
        /// <summary>
        /// Serial 포트
        /// </summary>
#if NETSTANDARD2_0
        public SerialPortStream SerialPort { get; }
#else
        public SerialPort SerialPort { get; }
#endif
        private readonly object openLock = new object();
        private readonly object writeLock = new object();
        private readonly object readLock = new object();
        private readonly Queue<byte> readBuffer = new Queue<byte>();
        private readonly EventWaitHandle readEventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly string description;
        private bool isRunningReceive = false;
        public SerialPortChannel(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, Handshake handshake)
        {
            description = portName;
            SerialPort =
#if NETSTANDARD2_0
                new SerialPortStream(portName, baudRate, dataBits, parity, stopBits);
#else
            new SerialPort(portName, baudRate, parity, dataBits, stopBits); 
#endif
            SerialPort.Handshake = handshake;
        }
        ~SerialPortChannel()
        {
            Dispose();
        }
        public override void Dispose()
        {
            if(IsDisposed == false)
            {
                IsDisposed = true;
                Close();
            }
        }
        public override void Write(byte[] bytes)
        {
            CheckPort(true);

            lock(writeLock)
            {
                try
                {
                    if(SerialPort.IsOpen)
                    {
                        SerialPort.Write(bytes, 0, bytes.Length);
#if NETSTANDARD2_0
                        SerialPort.Flush();
#endif
                    }
                }
                catch (Exception ex)
                {
                    Close();
                    throw ex;
                }
            }
        }
        public override byte Read(int timeout)
        {
            lock(readLock)
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
        public override IEnumerable<byte> ReadAllRemain()
        {
            lock(readLock)
            {
                while(readBuffer.Count < 0)
                {
                    yield return readBuffer.Dequeue();
                }
                if (SerialPort.IsOpen == false)
                    yield break;
                try
                {
                    SerialPort.DiscardInBuffer();
                }
                catch { }
            }
        }
        public override uint BytesToRead
        {
            get
            {
                uint available = 0;
                try
                {
                    available = (uint)SerialPort.BytesToRead;
                }
                catch {}
                return (uint)readBuffer.Count + available;
            }
        }
        private void Close()
        {
            lock(openLock)
            {
                if (SerialPort.IsOpen)
                {
                    Logger?.Log(new ChannelCloseEventLog(this));
                    SerialPort?.Close();
                }
            }
        }
        private byte? GetByte(int timeout)
        {
            lock (readBuffer)
            {
                if (readBuffer.Count == 0)
                {
                    readEventWaitHandle.Reset();
                    Task.Factory.StartNew(() =>
                    {
                        if (isRunningReceive == false)
                        {
                            isRunningReceive = true;
                            try
                            {
                                CheckPort(false);
                                if (SerialPort.IsOpen)
                                {
                                    byte[] buffer = new byte[8192];
                                    while (true)
                                    {
                                        if (SerialPort.BytesToRead > 0)
                                        {
                                            int receive = SerialPort.Read(buffer, 0, buffer.Length);
                                            lock (readBuffer)
                                            {
                                                for (int i = 0; i < receive; i++)
                                                    readBuffer.Enqueue(buffer[i]);
                                                readEventWaitHandle.Set();
                                            }
                                        }
#if NETSTANDARD2_0
                                        else Thread.Sleep(1);
#endif
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                Close();
                            }
                            readEventWaitHandle.Set();
                            isRunningReceive = false;
                        }
                    }, TaskCreationOptions.LongRunning);
                }
                else return readBuffer.Dequeue();
            }
            if (timeout == 0 ? readEventWaitHandle.WaitOne() : readEventWaitHandle.WaitOne(timeout))
                return readBuffer.Count > 0 ? readBuffer.Dequeue() : (byte?)default;
            else
                return null;
        }
        private void CheckPort(bool isWriting)
        {
            lock (openLock)
            {
                if (IsDisposed == false)
                {
                    try
                    {
                        if (SerialPort.IsOpen == false)
                        {
                            SerialPort.Open();
                            ReadAllRemain();
                            Logger?.Log(new ChannelOpenEventLog(this));
                        }
                    }
                    catch (Exception ex)
                    {
                        if(isWriting == false)
                        {
                            Logger?.Log(new ChannelErrorLog(this, ex));
                        }
                        throw ex;
                    }
                }
            }
        }
    }
}


