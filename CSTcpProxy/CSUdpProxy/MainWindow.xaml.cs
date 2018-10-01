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

            Task.Run(() => RunSv(50080, "10.0.0.12", 50080));
            Task.Run(() => RunSv(60080, "127.0.0.1", 60080));
        }
        async void RunSv(int localPort, string remoteIpAddr, int remotePort)
        {
            var threadId = $"{localPort}_{remoteIpAddr}_{remotePort}";
            TraceLog($"Start, {threadId}");

            var bufferSize = 64 * 1024 - 20 - 20;//とりあえずTCPウインドウサイズの一般的な最大値64KBからTCPヘッダ（UDPヘッダより大きい）とIPヘッダのサイズを引いた値。

            var udpRecieve = new UdpClient(new IPEndPoint(IPAddress.Loopback, localPort));
            udpRecieve.Client.ReceiveBufferSize = bufferSize;
            var udpSend = new UdpClient(remoteIpAddr, remotePort);
            udpSend.Client.ReceiveBufferSize = bufferSize;
            TraceLog($"Connected, {threadId}, Local={(IPEndPoint)udpSend.Client.LocalEndPoint}, Remote={(IPEndPoint)udpSend.Client.RemoteEndPoint}");


            var firstRecieveRet = await udpRecieve.ReceiveAsync();

            TraceLog($"first Receieved, {threadId}, {firstRecieveRet.RemoteEndPoint}, {firstRecieveRet.Buffer.Length}");

            var sendSizeFirst = await udpSend.SendAsync(firstRecieveRet.Buffer, firstRecieveRet.Buffer.Length);

            TraceLog($"first Send End, {threadId}, size={sendSizeFirst}");

            var taskForward = Task.Run(async () =>
            {
                TraceLog($"taskForward Start, {threadId}");
                while (true)
                {

                    udpRecieve.Client.ReceiveBufferSize = bufferSize;
                    var recieveRet = await udpRecieve.ReceiveAsync();

                    TraceLog($"taskForward, {threadId} Receieved, {recieveRet.RemoteEndPoint}, {recieveRet.Buffer.Length}");

                    var sendSize = await udpSend.SendAsync(recieveRet.Buffer, recieveRet.Buffer.Length);
                    TraceLog($"taskForward, {threadId} Send End, size={sendSize}");
                }
            });

            var taskReverse = Task.Run(async () =>
            {
                TraceLog($"taskReverse, {threadId} Start");
                while (true)
                {
                    udpSend.Client.ReceiveBufferSize = bufferSize;
                    var sendReciveRet = await udpSend.ReceiveAsync();

                    TraceLog($"taskReverse, {threadId} Receieved, {sendReciveRet.RemoteEndPoint}, {sendReciveRet.Buffer.Length}");

                    var sendSize = await udpRecieve.SendAsync(sendReciveRet.Buffer, sendReciveRet.Buffer.Length,
                        firstRecieveRet.RemoteEndPoint);
                    TraceLog($"taskReverse, {threadId} Send End, size={sendSize}");
                }
            });

            
            Task.WaitAll(taskForward, taskReverse);
        }
    }
}
