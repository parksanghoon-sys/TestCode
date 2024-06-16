using BytePacketSupport;
using BytePacketSupport.BytePacketSupport.Services.CRC;
using Xunit.Abstractions;

namespace AppendTest
{
    public class BasicUnitTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public BasicUnitTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        [Fact]
        public void Test1()
        {
            var builder1 = new PacketBuilder(new PacketBuilderConfiguration()
            {
                DefaultEndian = BytePacketSupport.Enums.EEndian.LITTLE
            })
               .AppendInt16(1)
               .AppendInt(2)
               .AppendLong(3)               
               .Build();

            _testOutputHelper.WriteLine(builder1.ToHexString());

        }
        [Fact]
        public void Test2()
        {
            var builder1 = new PacketBuilder()
                .BeginSection("packet")
                .AppendInt16(1)
                .AppendInt16(2)
                .AppendShort(4)
                .AppendULong(23231)
                .EndSection("packet")
                .Compute("packet", CRC16Type.Classic)
                .Build();
            _testOutputHelper.WriteLine(builder1.ToHexString());
        }
    }
}