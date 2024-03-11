using cliompensatioinOfNegative2;

namespace Complement2Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            BitCalculation bitCalculation = new BitCalculation();

            var test = bitCalculation.CalculateComplement(106,7);
            var test2 = bitCalculation.CalculateComplement(21, 7);
            var test3 = bitCalculation.CalculateComplement(197, 8);
            var test4 = bitCalculation.CalculateComplement(172, 8);
            var test5 = bitCalculation.CalculateComplement(237, 8);

            var a1 = bitCalculation.GetOriginalFromTwoComplement(-22, 7);
            var a2 = bitCalculation.GetOriginalFromTwoComplement(21, 7);
            var a3 = bitCalculation.GetOriginalFromTwoComplement(-84, 8);
            var a4 = bitCalculation.GetOriginalFromTwoComplement(-19, 8);

            Assert.AreEqual(test,-22);
            Assert.AreEqual(test2,21);
            Assert.AreEqual(test3, -59);
            Assert.AreEqual(test4, -84);
            Assert.AreEqual(test5, -19);

            Assert.AreEqual(a1, 106);
            Assert.AreEqual(a2, 21);
            Assert.AreEqual(a3, 172);
            Assert.AreEqual(a4, 237);

        }
    }
}