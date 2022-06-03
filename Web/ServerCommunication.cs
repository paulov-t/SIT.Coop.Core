using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SIT.Coop.Core.Matchmaker;

namespace SIT.Z.Coop.Core.Web
{

	public static class ServerCommunication
	{

		/// <summary>
		/// The User Profile of the Local Player. I usually set this as soon as we load MatchMakerAcceptScreen (probably could be done a lot sooner and globally)
		/// </summary>
		public static object UserProfile { get; set; }

		private static List<UdpClient> udpClients = new List<UdpClient>();

		//private static int udpClientsCount = 1;

		private static int udpServerPort = 7070;

		private static string backendUrlIp { get; set; }
        public static bool CommunicationError { get; private set; }

        //private static string backendUrlPort { get; set; }

        public delegate void OnDataReceivedHandler(byte[] buffer);
		public static event OnDataReceivedHandler OnDataReceived;

		public static void CloseAllUdpClients()
		{
			foreach (var udpClient in udpClients)
			{
				udpClient.Close();
			}

			udpClients.Clear();
		}

		public static UdpClient GetUdpClient(bool reliable = false)
		{
			if (ServerCommunication.CommunicationError || MatchmakerAcceptPatches.IsSinglePlayer)
				return null;

			if (!reliable && udpClients.Any())
				return udpClients[0];

			if (reliable && udpClients.Count > 1)
				return udpClients[1];

			// TODO: Get the game server ip properly!
			Dictionary<string, string> dataDict = new Dictionary<string, string>();
			dataDict.Add("groupId", MatchmakerAcceptPatches.GetGroupId());
			if (string.IsNullOrEmpty(backendUrlIp))
			{
				var backendUrl = PatchConstants.GetBackendUrl();
				if (backendUrl.Contains("localhost"))
					backendUrlIp = "127.0.0.1";
				else
					backendUrlIp = backendUrl.Split(':')[1].Replace("//", "");
				//backendUrlPort = array[2];
				PatchConstants.Logger.LogInfo("Setting ServerCommunicationCoopImplementation backendurlip::" + backendUrlIp);
			}
			var returnedIp = new Request().PostJson("/client/match/group/server/getGameServerIp", data: dataDict.ToJson());
			PatchConstants.Logger.LogInfo("GetUdpClient::Game Server IP is " + returnedIp);
			if (!string.IsNullOrEmpty(returnedIp))
			{
				if (IPAddress.TryParse(returnedIp, out _))
				{
					backendUrlIp = returnedIp;
				}
			}

			UdpClient udpClient = new UdpClient(backendUrlIp, (reliable ? udpServerPort + 1 : udpServerPort));
			udpClient.Client.SendBufferSize = 50;
			udpClient.Client.ReceiveBufferSize = 50;
			udpClient.Client.ReceiveTimeout = 50;
			udpClient.Client.SendTimeout = 50;
			udpClient.Send(Encoding.UTF8.GetBytes("Connect="), Encoding.UTF8.GetBytes("Connect=").Length);
			//udpClient.BeginSend(Encoding.UTF8.GetBytes("Connect="), Encoding.UTF8.GetBytes("Connect=").Length, (IAsyncResult ar) => { }, null);
			udpClient.BeginReceive(ReceiveUdp, udpClient);

			if (!udpClients.Contains(udpClient))
				udpClients.Add(udpClient);

			return udpClient;
		}

		public static void ReceiveUdp(IAsyncResult ar)
		{
			var udpClient = ar.AsyncState as UdpClient;
			IPEndPoint endPoint = null;
			var data = udpClient.EndReceive(ar, ref endPoint);
			if (OnDataReceived != null)
			{
				OnDataReceived(data);
			}
			//ServerHandleReceivedData(data, endPoint);

			udpClient.BeginReceive(ReceiveUdp, udpClient);
		}


		public delegate void PostLocalPlayerDataHandler(object player, Dictionary<string, object> data);
		public static event PostLocalPlayerDataHandler OnPostLocalPlayerData;

		/// <summary>
		/// Posts the data to the Udp Socket and returns the changed Dictionary for any extra use
		/// </summary>
		/// <param name="player"></param>
		/// <param name="data"></param>
		/// <param name="useReliable"></param>
		/// <returns></returns>
		public static Dictionary<string, object> PostLocalPlayerData(object player, Dictionary<string, object> data, bool useReliable = false)
		{
			var profile = PatchConstants.GetPlayerProfile(player);
			if (!data.ContainsKey("groupId"))
			{
				data.Add("groupId", MatchmakerAcceptPatches.GetGroupId());
			}
			//if (!data.ContainsKey("profileId"))
			//{
			//	data.Add("profileId", player.Profile.Id);
			//}
			if (!data.ContainsKey("accountId"))
			{
				data.Add("accountId", PatchConstants.GetPlayerProfileAccountId(profile));
			}
			_ = SendDataDownWebSocket(data, useReliable);

			if (OnPostLocalPlayerData != null)
			{
				OnPostLocalPlayerData(player, data);
			}
			return data;
		}

		/// <summary>
		/// Posts the data to the Udp Socket and returns the changed Dictionary for any extra use
		/// </summary>
		/// <param name="player"></param>
		/// <param name="data"></param>
		/// <param name="useReliable"></param>
		/// <returns></returns>
		public static async Task<Dictionary<string, object>> PostLocalPlayerDataAsync(object player, Dictionary<string, object> data, bool useReliable = false)
		{
			return await Task.Run(() => { return PostLocalPlayerData(player, data, useReliable); });
		}
		static bool lockedWS = false;
		static object lockedWSObject = new object();

		public static async Task SendDataDownWebSocket(object data, bool reliable = false)
		{
			if (MatchmakerAcceptPatches.IsSinglePlayer || CommunicationError)
				return;

			int attemptCountdown = 30;
			while (lockedWS && attemptCountdown-- > 0)
			{
				await Task.Delay(2);
			}
			if (attemptCountdown == 0)
				return;

			lockedWS = true;

			string text = string.Empty;
			try
			{
				text = ((data is string) ? ((string)data) : data.SITToJson());
			}
			catch (Exception ex)
			{
				LoggingCoopImplementation.QuickLog("SendDataDownWebSocket::" + ex.ToString());
			}
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			try
			{
				byte[] bytes = Encoding.ASCII.GetBytes(text);
				if (GetUdpClient(reliable).Send(bytes, bytes.Length) == 0)
				{
					LoggingCoopImplementation.QuickLog("SendDataDownWebSocket::socketClient::Sent no bytes!");
				}
				await Task.Delay(1);

			}
			catch (Exception ex2)
			{
				LoggingCoopImplementation.QuickLog("SendDataDownWebSocket::Socket::" + ex2.ToString());
				ServerCommunication.CommunicationError = true;
			}
			lockedWS = false;
		}


		public static string GetMyExternalAddress()
		{
			try
			{
				return new StreamReader(WebRequest.Create("http://checkip.dyndns.org").GetResponse().GetResponseStream()).ReadToEnd().Trim().Split(':')[1].Substring(1).Split('<')[0];
			}
			catch (Exception ex)
			{
				LoggingCoopImplementation.QuickLog(ex.ToString());
			}
			return "";
		}

	}

	public static class LoggingCoopImplementation
	{
		public static void QuickLog(string logString)
		{
			if (!string.IsNullOrEmpty(logString))
			{
				string text = (MatchmakerAcceptPatches.IsServer ? "SERVER-> " : "CLIENT-> ");
				if (MatchmakerAcceptPatches.IsSinglePlayer)
				{
					text = "SP-> ";
				}
				text += logString;
				UnityEngine.Debug.LogError(text);
				PatchConstants.Logger.LogInfo(text);
			}
		}
	}
}
