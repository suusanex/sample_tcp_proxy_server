using System;
using System.Collections.Generic;
using System.Configuration;
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

namespace CSTcpProxy
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
            System.Diagnostics.Trace.WriteLine("CSTCPProxy:" + msg);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TraceLog("MainWindow_OnLoaded");

            Task.Run(() => RunSv());
        }
        async void RunSv()
        {
            TraceLog("Start");

            var listener = new TcpListener(IPAddress.Loopback, 10001);
            listener.Start();

            while (true)
            {
                TraceLog("accept Start");
                var accept = await listener.AcceptTcpClientAsync();

                var task = Task.Run(async () =>
                {
                    TraceLog("accept");

                    var client = new TcpClient();
                    await client.ConnectAsync("10.0.0.12", 80);

                    TraceLog("connect");

                    using (client)
                    using (accept)
                    {

                        var acceptRead = Task.Run(async () =>
                        {
                            using (var acceptStream = accept.GetStream())
                            using (var connectStream = client.GetStream())
                            {
                                while (true)
                                {

                                    TraceLog("acceptSide Read Start");
                                    var buf = new byte[64 * 1024];
                                    var retByte = await acceptStream.ReadAsync(buf, 0, 64 * 1024);
                                    TraceLog($"acceptSide Read End, Size={retByte}");
                                    await connectStream.WriteAsync(buf, 0, retByte);
                                    TraceLog($"acceptSide Write End, Size={retByte}");

                                }
                            }
                        });
                        var connectRead = Task.Run(async () =>
                        {
                            using (var acceptStream = accept.GetStream())
                            using (var connectStream = client.GetStream())
                            {
                                while (true)
                                {
                                    TraceLog("connectSide Read Start");
                                    var buf = new byte[64 * 1024];
                                    var retByte = await connectStream.ReadAsync(buf, 0, 64 * 1024);
                                    TraceLog($"connectSide Read End, Size={retByte}");

                                    await acceptStream.WriteAsync(buf, 0, retByte);
                                    TraceLog($"connectSide Write End, Size={retByte}");

                                }
                            }
                        });

                        Task.WaitAll(acceptRead, connectRead);
                    }
                });
            }
        }
    }
}
