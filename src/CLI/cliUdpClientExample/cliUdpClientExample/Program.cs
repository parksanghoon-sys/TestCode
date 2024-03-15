using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Console;

namespace UdpCli
{
    class Program
    {
        static void Main(string[] args)
        {
            // (1) UdpClient 객체 성성
            UdpClient cli = new UdpClient();

            string msg = "안녕하세요";
            byte[] datagram = Encoding.UTF8.GetBytes(msg);

            // (2) 데이타 송신
            cli.Send(datagram, datagram.Length, "127.0.0.1", 514);
            WriteLine("[Send] 127.0.0.1:7777 로 {0} 바이트 전송", datagram.Length);

            // (3) 데이타 수신
            IPEndPoint epRemote = new IPEndPoint(IPAddress.Any, 999);
            byte[] bytes = cli.Receive(ref epRemote);
            WriteLine("[Receive] {0} 로부터 {1} 바이트 수신", epRemote.ToString(), bytes.Length);

            // (4) UdpClient 객체 닫기
            cli.Close();
        }
    }
}