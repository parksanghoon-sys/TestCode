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