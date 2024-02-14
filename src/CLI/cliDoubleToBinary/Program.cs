// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

int doubleData = 448;
double maxData = 0;
string binary = Convert.ToString(doubleData,2).PadLeft(10,'0');
var binarychars = binary.ToCharArray();
Console.WriteLine(binary);

for (int index = 1; index < 9; index++)
{
    if (binarychars[index] == '1')
    {
        maxData += (180 / Math.Pow(2,index));
    }
}
long bitCount = sizeof(int) * 4;
char[] result = new char[bitCount];

byte[] bytes = BitConverter.GetBytes(doubleData);
foreach (byte b in bytes )
{
    Console.WriteLine(Convert.ToString(b, 2).PadLeft(8, '0'));
}

