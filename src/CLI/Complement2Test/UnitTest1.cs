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

            Assert.AreEqual(test,-22);
            Assert.AreEqual(test2,21);
            Assert.AreEqual(test3, -59);
            Assert.AreEqual(test4, -84);
            Assert.AreEqual(test5, -19);
         
        }
    }
}