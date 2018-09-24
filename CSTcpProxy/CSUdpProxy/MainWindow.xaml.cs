using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CSUdpProxy
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void TraceLog(string msg)
        {
            System.Diagnostics.Trace.WriteLine("CSUDPProxy:" + msg);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSv());
        }
        async void RunSv()
        {
            TraceLog("Start");

            var udpRecieve = new UdpClient(new IPEndPoint(IPAddress.Loopback, 50080));
            var udpSend = new UdpClient("10.0.0.12", 50080);
        
            var firstRecieveRet = await udpRecieve.ReceiveAsync();

            await udpSend.SendAsync(firstRecieveRet.Buffer, firstRecieveRet.Buffer.Length);

            var taskForward = Task.Run(async () =>
            {
                while (true)
                {
                    var recieveRet = await udpRecieve.ReceiveAsync();

                    await udpSend.SendAsync(recieveRet.Buffer, recieveRet.Buffer.Length);
                }
            });

            var taskReverse = Task.Run(async () =>
            {
                while (true)
                {
                    var sendReciveRet = await udpSend.ReceiveAsync();

                    await udpRecieve.SendAsync(sendReciveRet.Buffer, sendReciveRet.Buffer.Length,
                        firstRecieveRet.RemoteEndPoint);
                }
            });

            
            Task.WaitAll(taskForward, taskReverse);
        }
    }
}
