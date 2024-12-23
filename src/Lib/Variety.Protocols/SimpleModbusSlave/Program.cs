using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using Protocols.Channels;
using Protocols.Modbus;
using Protocols.Modbus.Serialization;

internal class Program
{
    private static void Main(string[] args)
    {
        var logger = new ConsoleChannelLogger();

        IChannel channel = new TcpChannelProvider(532) { Logger = logger };

        var modbusSlaveService = new ModbusSlaveService(channel)
        {
            //Serializer = new ModbusRtuSerializer(),
            Serializer = new ModbusTcpSerializer(),
            //Serializer = new ModbusAsciiSerializer(),
        };

        var modbuseSlave1 = modbusSlaveService[1] = new ModbusSlave();
        var float100 = 1.23f;
        var float102 = 4.56f;
        int boolIndex = 0;
        (channel as ChannelProvider)?.Start();

        Task.Run(() =>
        {
            while(true)
            {
                float100 += 0.01f;
                float102 += 0.01f;

                modbuseSlave1.InputRegisters.SetValue(100, float100);
                modbuseSlave1.InputRegisters.SetValue(102, float102);

                for(ushort i = 0; i < 10 ; i++)
                {
                    modbuseSlave1.DiscreteInputs[i] = i == boolIndex;
                    modbuseSlave1.Coils[i] = i == boolIndex;
                }
                boolIndex = (boolIndex + 1) % 10;

                Thread.Sleep(1000);
            }
        });

        Console.ReadKey();
        modbusSlaveService.Dispose();
    }
}