using Newtonsoft.Json;
using SIT.Coop.Core;
using SIT.Tarkov.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoopTarkovGameServer
{
    public static class ShimExtensionsForNET472
    {
        public static void Clear<T>(this ConcurrentQueue<T> q)
        {
            foreach(T item in q)
            {
                q.Enqueue(item);
            }
        }

        public static void Clear<T>(this ConcurrentBag<T> q)
        {
            q = new ConcurrentBag<T>();
        }
    }

    public class EchoGameServer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public object UnityPlugin { get; set; }

        public delegate void ConnectionReceivedHandler(IPEndPoint endPoint);
        public event ConnectionReceivedHandler OnConnectionReceived;

        public delegate void ResetServerHandler();
        public event ResetServerHandler OnResetServer;

        public delegate void LogHandler(string text);
        public event LogHandler OnLog;

        //public delegate void MethodCallHandler(ConcurrentDictionary<string, int> actionCounts);
        //public event MethodCallHandler OnMethodCall;

        public static EchoGameServer Instance { get { return Instances.Last(); } }
        public static List<EchoGameServer> Instances = new List<EchoGameServer>();

        public static int HighestAcceptablePing { get { return 999; } }
        public TcpListener tcpServer { get; set; }
        public List<UdpClient> udpReceivers;
        public int CurrentReceiverIndex = 0;
        //public int NumberOfReceivers = 2; // Two Channels. Reliable and Unreliable
        public int NumberOfReceivers = 1;
        public int udpReceiverPort { get { return Plugin.UDPPort; } }
        public DateTime StartupTime = DateTime.Now;
        public bool quit;
        public int NumberOfConnections = 0;
        public (IPEndPoint, string)? HostConnection;
        public readonly ConcurrentDictionary<IPEndPoint, string> ConnectedClients = new ConcurrentDictionary<IPEndPoint, string>();
        public readonly ConcurrentDictionary<IPEndPoint, (UdpClient, int)> ConnectedClientToReceiverPort = new ConcurrentDictionary<IPEndPoint, (UdpClient, int)>();
        public readonly ConcurrentDictionary<IPEndPoint, DateTime> ConnectedClientsLastTimeDataReceiver = new ConcurrentDictionary<IPEndPoint, DateTime>();
        public readonly ConcurrentDictionary<string, IPEndPoint> PlayersToConnectedClients = new ConcurrentDictionary<string, IPEndPoint>();
        public readonly ConcurrentDictionary<string, int> MethodCallCounts = new ConcurrentDictionary<string, int>();
        public int TotalNumberOfBytesSentLastSecond = 0;
        public int TotalNumberOfBytesProcessedLastSecond = 0;
        public int TotalNumberOfBytesProcessed = 0;
        public readonly TimeSpan ConnectionTimeout = new TimeSpan(0, 2, 0);

        public ConcurrentQueue<(IPEndPoint, byte[], string)> EnqueuedDataToSend = new ConcurrentQueue<(IPEndPoint, byte[], string)>();


        public ConcurrentQueue<Dictionary<string,object>> DataProcessInsurance = new ConcurrentQueue<Dictionary<string, object>>();

        public ConcurrentBag<Dictionary<string, object>> BotSpawnData = new ConcurrentBag<Dictionary<string, object>>();
        public ConcurrentBag<Dictionary<string, object>> PlayerSpawnData = new ConcurrentBag<Dictionary<string, object>>();

        public ConcurrentQueue<string> Log = new ConcurrentQueue<string>();

        public readonly ConcurrentDictionary<IPEndPoint, DateTime> PingTimes = new ConcurrentDictionary<IPEndPoint, DateTime>();
        public readonly ConcurrentDictionary<IPEndPoint, DateTime> PongTimes = new ConcurrentDictionary<IPEndPoint, DateTime>();

        public readonly ConcurrentBag<(string, string, long)> ProcessedEvents = new ConcurrentBag<(string, string, long)>();

        public DateTime? GameServerClock { get; private set; }

        public Guid InstanceId  { get; private set; }
        public readonly List<TcpClient> ConnectedClientsTcp = new List<TcpClient>();

        public EchoGameServer()
        {
            InstanceId = Guid.NewGuid();
            //if (startingUdpPort.HasValue) udpReceiverPort = startingUdpPort.Value;
            // Only handle 1 instance for now
            Instances.Clear();
            Instances.Add(this);
        }

        public void CreateListenersAndStart()
        {
            var internalIPString = new Request().PostJson("/ServerInternalIPAddress", null);
            var externalIPString = new Request().PostJson("/ServerExternalIPAddress", null);
            if (string.IsNullOrEmpty(internalIPString))
                internalIPString = "127.0.0.1";

            AddToLog($"{this.GetType()}:CreateListenersAndStart:{internalIPString}");


            //tcpReceiver = new TcpClient();
            udpReceivers = new List<UdpClient>();
            for (var i = 0; i < NumberOfReceivers; i++)
            {
                var newPort = udpReceiverPort + i;

                //if (UPnP.NAT.Discover())
                //{
                //    UPnP.NAT.ForwardPort(newPort, ProtocolType.Udp, "SIT-Tarkov-" + i);
                //}

                //var udpReceiver = new UdpClient(newPort);
                
                //var newIpEndPoint = new IPEndPoint(IPAddress.Any, newPort);
                var newIpEndPoint = new IPEndPoint(IPAddress.Parse(internalIPString), newPort);
                var udpReceiver = new UdpClient(newIpEndPoint);
                AddToLog(this.GetType() + ": Started udp receiver " + newIpEndPoint.ToString());
                //udpReceiver.AllowNatTraversal(true);
                //udpReceiver.DontFragment = true;
                //udpReceiver.DontFragment = false;
                udpReceiver.Client.SendTimeout = 500; // defaulted min
                udpReceiver.Client.ReceiveTimeout = 500; // defaulted min
                const int SIO_UDP_CONNRESET = -1744830452;
                udpReceiver.Client.IOControl(
                    (IOControlCode)SIO_UDP_CONNRESET,
                    new byte[] { 0, 0, 0, 0 },
                    null
                );
                //udpReceiver.Client.ReceiveBufferSize = 50;
                //udpReceiver.Client.SendBufferSize = 300;
                udpReceiver.Client.ReceiveBufferSize = 16384;
                udpReceiver.Client.SendBufferSize = 16384;
              
                udpReceiver.BeginReceive(UdpReceive, udpReceiver);
         
                udpReceivers.Add(udpReceiver);
            }

            //    tcpServer = new TcpListener(new IPEndPoint(IPAddress.Any, 7076));
            //    tcpServer.Server.ReceiveBufferSize = 2048;
            //    tcpServer.Server.ReceiveTimeout = HighestAcceptablePing;
            //    tcpServer.Server.SendTimeout = HighestAcceptablePing;
            //StartTcpServerAndAccept();    

     
                    UpdatePings();
            ServerSendOutEnqueuedData();
        }

        //public void StartTcpServerAndAccept()
        //{
        //    tcpServer.Start();
        //    AddToLog(this.GetType() + ": Started tcp receiver on " + tcpServer.LocalEndpoint);
        //    tcpServer.BeginAcceptTcpClient((IAsyncResult result) =>
        //    {
        //        StartTcpServerAndAccept();
        //        TcpClient client = tcpServer.EndAcceptTcpClient(result);  //creates the TcpClient
        //        NetworkStream ns = client.GetStream();
        //        ConnectedClientsTcp.Add(client);
        //        while (client.Connected)  //while the client is connected, we look for incoming messages
        //        {
        //            try
        //            {
        //                byte[] msg = new byte[2048];     //the messages arrive as byte array
        //                ns.Read(msg, 0, msg.Length);   //the same networkstream reads the message sent by the client
        //                Debug.WriteLine("TCP:" + Encoding.Default.GetString(msg));
        //                AddToLog("TCP:" + Encoding.Default.GetString(msg));
        //                foreach(var cc in ConnectedClientsTcp)
        //                {
        //                    NetworkStream ccns = cc.GetStream();
        //                    if(ccns != null && ccns.CanWrite)
        //                    {
        //                        ccns.Write(msg, 0, msg.Length);
        //                    }

        //                }
        //                client.Close();
        //                //ns.Write(msg, 0, msg.Length);
        //            }
        //            catch (Exception)
        //            {
        //            }
        //        }
        //        ConnectedClientsTcp.Remove(client);


        //    }, tcpServer);  //this is called asynchronously and will run in a different thread
        //}

        public void UdpReceive(IAsyncResult ar)
        {
            var udpClient = ar.AsyncState as UdpClient;
            IPEndPoint endPoint = null;
            var data = udpClient.EndReceive(ar, ref endPoint);

            ServerHandleReceivedData(data, endPoint);

            udpClient.BeginReceive(UdpReceive, udpClient);
        }

        //public void TcpHandler(IAsyncResult ar)
        //{
        //    var tcpListener = ar.AsyncState as TcpListener;
        //    var socket = tcpListener.EndAcceptSocket(ar);
        //    socket.()
        //    using (Stream myStream = new NetworkStream(socket))
        //    {
        //        StreamReader reader = new StreamReader(myStream);
        //        StreamWriter writer = new StreamWriter(myStream)
        //        { AutoFlush = true };
        //        ServerHandleReceivedDataTcp(ref reader, ref writer, (IPEndPoint)socket.RemoteEndPoint);
        //    }
            
        //    TcpHandler(ar);
        //}


        public void ResetServer()
        {
            StartupTime = DateTime.Now;

            ConnectedClientToReceiverPort.Clear();
            ConnectedClients.Clear();
            ConnectedClientsLastTimeDataReceiver.Clear();
            PlayersToConnectedClients.Clear();
            MethodCallCounts.Clear();
            DataProcessInsurance.Clear();
            EnqueuedDataToSend.Clear();
            BotSpawnData.Clear();
            PlayerSpawnData.Clear();

            Log = new ConcurrentQueue<string>();

            TotalNumberOfBytesSentLastSecond = 0;
            TotalNumberOfBytesProcessedLastSecond = 0;
            TotalNumberOfBytesProcessed = 0;

            if(OnResetServer != null)
            {
                OnResetServer();
            }
        }

        public async void UpdatePings()
        {
            await Task.Delay(50);
            var array = ASCIIEncoding.ASCII.GetBytes("Ping");
            foreach (IPEndPoint item in ConnectedClients.Keys)
            {
                PingTimes.TryRemove(item, out _);
                udpReceivers[0].Send(array, array.Length, item);
                //EnqueuedDataToSend.Enqueue((item, array, null));
                PingTimes.TryAdd(item, DateTime.Now);
            }
            UpdatePings();
        }


        private async void ServerSendOutEnqueuedData()
        {
            try
            {
                //if (DataProcessInsurance.Any())
                //{
                //    var bytes = ASCIIEncoding.ASCII.GetBytes(JsonConvert.SerializeObject(DataProcessInsurance));
                //    EnqueuedDataToSend.Enqueue((null, bytes, null));
                //}
                //while (EnqueuedDataToSend.Any())
                if (EnqueuedDataToSend.Any())
                {
                    if(EnqueuedDataToSend.TryDequeue(out (IPEndPoint, byte[], string) result))
                    {
                        foreach (IPEndPoint item in ConnectedClients.Keys)
                        {
                            //await udpReceivers[0].SendAsync(queuedData, queuedData.Length, item);
                            udpReceivers[0].Send(result.Item2, result.Item2.Length, item);
                        }
                    }
                    //var queuedData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(EnqueuedDataToSend.Select(x => Encoding.UTF8.GetString(x.Item2))));
                    //foreach (IPEndPoint item in ConnectedClients.Keys)
                    //{
                    //    //await udpReceivers[0].SendAsync(queuedData, queuedData.Length, item);
                    //    udpReceivers[0].Send(queuedData, queuedData.Length, item);
                    //}
                    //EnqueuedDataToSend.Clear();
                }
            }
            catch (Exception ex)
            {
                AddToLog(this.GetType() + ex.ToString());
            }
            await Task.Delay(1);
            ServerSendOutEnqueuedData();
        }

        private void ServerHandleReceivedDataTcp(ref StreamReader reader, ref StreamWriter writer, IPEndPoint receivedIpEndPoint)
        {
            if (reader == null)
                return;

            try
            {
                var allText = reader.ReadToEnd();
                if (string.IsNullOrEmpty(allText))
                    return;

                if (allText.Length == 4 && allText == "Pong")
                {
                    PongTimes.TryRemove(receivedIpEndPoint, out _);
                    PongTimes.TryAdd(receivedIpEndPoint, DateTime.Now);
                    writer.Write("Ping");
                    writer.Flush();
                    return;
                }

                if (allText == "GET_PLAYERS")
                {
                    if (DataProcessInsurance.Any())
                    {
                        var queuedData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(EnqueuedDataToSend.Select(x => Encoding.UTF8.GetString(x.Item2))));
                    }
                }

                if (allText == "CHECK_DEAD")
                {
                    writer.Write("A COUPLE OF GUYS ARE DEAD LIKE");

                    if (DataProcessInsurance.Any())
                    {
                        var queuedData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(DataProcessInsurance.Select(x=>x)));
                        writer.Write(queuedData);
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }

        private void ServerHandleReceivedData(byte[] array, IPEndPoint receivedIpEndPoint)
        {
            string @string = Encoding.ASCII.GetString(array);
            if (@string.Length > 0)
            {
                try
                {
                    if (@string.Length == 4 && @string == "Pong")
                    {
                        PongTimes.TryRemove(receivedIpEndPoint, out _);
                        PongTimes.TryAdd(receivedIpEndPoint, DateTime.Now);
                        EnqueuedDataToSend.Enqueue((receivedIpEndPoint, Encoding.ASCII.GetBytes("Ping"), null));
                        //udpClient.BeginReceive(DataReceivedServer, udpClient);
                        return;
                    }

                    // If the "Server" player is saying "Start" then its a new game and clean up!
                    if (@string.StartsWith("Start="))
                    {
                        var accountId = @string.Split('=')[1];
                        PongTimes.TryRemove(receivedIpEndPoint, out _);
                        PongTimes.TryAdd(receivedIpEndPoint, DateTime.Now);
                        PingTimes.TryRemove(receivedIpEndPoint, out _);
                        PingTimes.TryAdd(receivedIpEndPoint, DateTime.Now);
                        ResetServer();
                        AddNewConnection(receivedIpEndPoint, accountId);
                        //udpClient.BeginReceive(DataReceivedServer, udpClient);
                        return;
                    }

                    if (@string.StartsWith("Connect="))
                    {
                        var accountId = @string.Split('=')[1];

                        PongTimes.TryRemove(receivedIpEndPoint, out _);
                        PongTimes.TryAdd(receivedIpEndPoint, DateTime.Now);
                        PingTimes.TryRemove(receivedIpEndPoint, out _);
                        PingTimes.TryAdd(receivedIpEndPoint, DateTime.Now);
                        AddNewConnection(receivedIpEndPoint, accountId);

                        //udpClient.BeginReceive(DataReceivedServer, udpClient);
                        return;
                    }

                    if (udpReceivers.Count == 0)
                    {
                        AddToLog("There are no udpReceivers!!!");
                        return;
                    }

                    AddNewConnection(receivedIpEndPoint, null);
                    if (ConnectedClients.Count == 0)
                    {
                        AddToLog("There are no ConnectedClients!!!");
                        return;
                    }

                    foreach (var client in ConnectedClients.Keys)
                    {
                        
                        foreach (var udpServer in udpReceivers)
                        {
                            //_ = udpServer.SendAsync(array, array.Length, client);
                            udpServer.Send(array, array.Length, client);
                            //udp.BeginSend(array, array.Length, (IAsyncResult r) => { }, client);
                        }
                    }

                    //EnqueuedDataToSend.Enqueue((receivedIpEndPoint, array, null));
                    /*
                    var dictData = JsonConvert.DeserializeObject<Dictionary<string, object>>(@string);
                    if (dictData != null)
                    {

                        //if (!dictData.ContainsKey("accountId"))
                        //{
                        //    //AddToLog("Thrown unusable Dictionary: Missing accountId!");
                        //    return;
                        //}

                        if (dictData.ContainsKey("method") || dictData.ContainsKey("m"))
                        {


                            if (!dictData.ContainsKey("method"))
                                dictData.Add("method", dictData["m"]);

                            var method = dictData["method"].ToString();
                            if (!string.IsNullOrEmpty(method))
                            {
                                if (!MethodCallCounts.ContainsKey(method))
                                {
                                    MethodCallCounts.TryAdd(method, 0);
                                }

                                if (MethodCallCounts.TryGetValue(method, out int callCount))
                                {
                                    callCount++;
                                    MethodCallCounts[method] = callCount;
                                }

                                if (OnMethodCall != null)
                                {
                                    OnMethodCall(MethodCallCounts);
                                }

                                //if (method == "Damage"
                                //    || method == "Dead"
                                //    //|| method == "Move"
                                //    )
                                //{
                                //    foreach (var client in ConnectedClients.Keys)
                                //    {
                                //        udpReceivers[0].Send(array, array.Length, client);
                                //        udpReceivers[1].Send(array, array.Length, client);
                                //    }
                                //    AddToLog($"Received {method} to {dictData["accountId"]}");



                                //    return;
                                //}

                                // Always push Dead calls !
                                //if (method == "Dead" || method == "Damage")
                                //{
                                //    DataProcessInsurance.Enqueue(dictData);
                                //}
                            }

                            //string accountId = dictData["accountId"].ToString();

                            //if (dictData.ContainsKey("tick"))
                            //{
                            //    long ticks = long.Parse(dictData["tick"].ToString());
                            //    if (!ProcessedEvents.Any(x => x.Item1 == method && x.Item2 == accountId && x.Item3 == ticks))
                            //        EnqueuedDataToSend.Enqueue((receivedIpEndPoint, array, accountId));

                            //    ProcessedEvents.Add((method, accountId, ticks));
                            //}
                            //else
                            //{
                            //}
                            //foreach (var client in ConnectedClients.Keys)
                            //{
                            //    udpReceivers[0].Send(array, array.Length, client);
                            //    udpReceivers[1].Send(array, array.Length, client);
                            //}


                        }
                    }
                    */

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Send parity data to ALL clients
        /// </summary>
        //private async void SendParityData()
        //{
        //    AddToLog($"Sending out Spawn Data, {PlayerSpawnData.Count} Players, {BotSpawnData.Count} Bots");
        //    foreach (var bpd in BotSpawnData)
        //    {
        //        EnqueuedDataToSend.Enqueue((null, Encoding.Default.GetBytes(JsonConvert.SerializeObject(bpd))));
        //        await Task.Delay(1000); // Delay by 1 second to ensure data is actually sent
        //    }
        //    foreach (var bpd in PlayerSpawnData)
        //    {
        //        EnqueuedDataToSend.Enqueue((null, Encoding.Default.GetBytes(JsonConvert.SerializeObject(bpd))));
        //        await Task.Delay(1000); // Delay by 1 second to ensure data is actually sent
        //    }
        //}

        private void AddNewConnection(IPEndPoint receivedIpEndPoint, string playerId, bool isHost = false)
        {
            if (receivedIpEndPoint == null)
            {
                AddToLog(this.GetType() + ": DataReceivedServer::[ERROR] IP End Point is NULL! WTF!");
                return;
            }

            if (udpReceivers.Count == 0)
            {
                AddToLog(this.GetType() + ": DataReceivedServer::[ERROR] AddNewConnection attempting to add new connection when there are no receivers!");
                return;
            }

            if (!ConnectedClients.Keys.Any((IPEndPoint x) => x.ToString() == receivedIpEndPoint.ToString()))
            {
                if (ConnectedClients.TryAdd(receivedIpEndPoint, playerId))
                {
                    ConnectedClientToReceiverPort.TryAdd(receivedIpEndPoint, (udpReceivers[0], udpReceiverPort + 0));
                    NumberOfConnections++;

                    if (isHost)
                        HostConnection = (receivedIpEndPoint, playerId);

                    //Debug.WriteLine(this.GetType() + ": DataReceivedServer::New Connection from " + receivedIpEndPoint.ToString());
                    //Console.WriteLine(this.GetType() + ": DataReceivedServer::New Connection from " + receivedIpEndPoint.ToString());
                    if (OnConnectionReceived != null)
                    {
                        OnConnectionReceived(receivedIpEndPoint);
                    }
                    AddToLog(this.GetType() + ": DataReceivedServer::Connection [NEW] from " + receivedIpEndPoint.ToString());
                }
                else
                {
                    AddToLog(this.GetType() + ": DataReceivedServer::Connection [RESTORE] from " + receivedIpEndPoint.ToString());
                }
            }
        }

        public void AddToLog(string text)
        {
            //Debug.WriteLine(text);
            if(OnLog != null)
            {
                OnLog(text);
            }
        }

        //~EchoGameServer()
        //{

        //}

        public void Dispose()
        {
            //    foreach (var c in udpReceivers)
            //    {
            //        c.Close();
            //        c.Dispose();
            //    }
            //    udpReceivers.Clear();
            //    Instances.Clear();
            //    ResetServer();
        }
    }

}
