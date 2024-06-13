
using System.Buffers.Binary;

short aa = 253;
var bb = BitConverter.IsLittleEndian == true ? (UInt16)BinaryPrimitives.ReverseEndianness(aa) : (UInt16)aa;



var case1 = ("4,4,1,0,10");
var case2 = ("1,4,1,0,110");
var case3 = ("2,4,1,0,1111");

var cases = new List<string> { case1, case2, case3 };

foreach (var item in cases)
{
	var test = item.Substring(0, item.LastIndexOf(','));
	if(test != null)
	{
		//break;
	}

}

byte[] data1 = new byte[2] {0x39,0x99};
byte[] data2 = new byte[2] {0x01,0x75};
byte[] dataArray = new byte[4];
Buffer.BlockCopy(data1, 0, dataArray, 0, 2);
Buffer.BlockCopy(data2, 0, dataArray, 2, 2);

Console.WriteLine("Jes");

char a = 'A';
char b = 'B';
var t = string.Format("{0}{1}",a,b);
Console.WriteLine(t);

string presetFreq = "123.123";
var ttt = presetFreq.ToCharArray()[0] = 'A';

Console.WriteLine(presetFreq);