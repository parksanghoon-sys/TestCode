﻿using Protocols.Abstractions.Channels;
using Protocols.Modbus.Requests;
using Protocols.Modbus.Serialization;
using Protocols.Abstractions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Protocols.Abstractions;

namespace Protocols.Modbus.Loggging
{

    /// <summary>
    /// 통신 채널을 통해 주고 받은 Mudbus 요청 메시지에 대한 Log
    /// </summary>
    public class ModbusRequestLog : ChannelRequestLog
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="channel">통신 채널</param>
        /// <param name="request">요청 메시지 인스턴스</param>
        /// <param name="rawMessage">원본 메시지</param>
        /// <param name="serializer">Modbus Serializer</param>
        public ModbusRequestLog(IChannel channel, ModbusRequest request, byte[] rawMessage, ModbusSerializer serializer) : base(channel, request, rawMessage)
        {
            this.serializer = serializer;
            ModbusRequest = request;
        }

        private ushort? transactionID = null;
        private readonly ModbusSerializer serializer;

        /// <summary>
        /// Modbus 요청 메시지
        /// </summary>
        public ModbusRequest ModbusRequest { get; }

        /// <summary>
        /// 트랜잭션 ID (Modbus TCP에서 사용)
        /// </summary>
        public ushort? TransactionID { get => transactionID ?? ModbusRequest?.TransactionID; set => transactionID = value; }

        /// <summary>
        /// 이 인스턴스의 정규화된 형식 이름을 반환합니다.
        /// </summary>
        /// <returns>정규화된 형식 이름입니다.</returns>
        public override string ToString() => $"REQ: {RawMessage.ModbusRawMessageToString(serializer)}";
    }
}
