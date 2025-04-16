using System.Configuration;
using System.Data;
using System.Net;
using System.Windows;

namespace wpfUDP_PacketTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress addr in addresses)
            {
                Console.WriteLine(addr.ToString());
            }
        }
    }

}
