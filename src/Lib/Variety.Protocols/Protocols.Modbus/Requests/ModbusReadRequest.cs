namespace Protocols.Modbus.Requests
{
    /// <summary>
    /// Modbus 읽기 요청
    /// </summary>
    public class ModbusReadRequest : ModbusRequest
    {
        public override ushort Length { get; }
        public override ModbusObjectType ObjectType { get => (ModbusObjectType)Function; }
        public ModbusReadRequest(byte slaveAddress, ModbusObjectType objectType, ushort address, ushort length)
            : base(slaveAddress, (ModbusFunction)objectType, address)
        {
            Length = length;
        }
        public override IEnumerable<byte> Serialize()
        {
            yield return SlaveAddress;
            yield return (byte)Function;
            yield return (byte)(Address >> 8 & 0xFF);
            yield return (byte)(Address & 0xFF);
            yield return (byte)(Length >> 8 & 0xFF);
            yield return (byte)(Length & 0xFF);
        }
        public override ModbusMessageCategory MessageCategory
        {
            get
            {
                switch (ObjectType)
                {
                    case ModbusObjectType.Coil:
                        return ModbusMessageCategory.RequestReadCoil;
                    case ModbusObjectType.DiscreteInput:
                        return ModbusMessageCategory.RequestReadDiscreteInput;
                    case ModbusObjectType.HoldingRegister:
                        return ModbusMessageCategory.RequestReadHoldingRegister;
                    case ModbusObjectType.InputRegister:
                        return ModbusMessageCategory.RequestReadInputRegister;
                    default:
                        return ModbusMessageCategory.None;
                }
            }
        }
    }
}
