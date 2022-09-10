using Comfort.Common;
using Diz.Jobs;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.Coop.Core.Matchmaker;
using SIT.Coop.Core.Player;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using SIT.Tarkov.Core.AI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

namespace SIT.Coop.Core.LocalGame
{
	struct Continuation
	{
		Profile Profile;
		TaskScheduler TaskScheduler { get; }


		public Continuation(TaskScheduler taskScheduler)
		{
			Profile = null;
			TaskScheduler = taskScheduler;
		}
		public Task<Profile> LoadBundle(Profile p)
        {
			var loadTask = SIT.A.Tarkov.Core.Plugin.LoadBundlesAndCreatePools(Profile.GetAllPrefabPaths(false).ToArray());

			return loadTask.ContinueWith((t) => { return p; }, TaskScheduler);
		}

		public Task<Profile> LoadBundles(Task<Profile> task)
		{
			Profile = task.Result;

			var loadTask = SIT.A.Tarkov.Core.Plugin.LoadBundlesAndCreatePools(Profile.GetAllPrefabPaths(false).ToArray());

			return loadTask.ContinueWith(GetProfile, TaskScheduler);
		}

		private Profile GetProfile(Task task)
		{
			//Logger.LogInfo("LoadBotTemplatesPatch+Continuation.GetProfile");
			return Profile;
		}
	}
#pragma warning disable CS0618 // Type or member is obsolete
    public class CoopGameComponent : NetworkBehaviour
    {
		BepInEx.Logging.ManualLogSource Logger;

		public static List<string> PeopleParityRequestedAccounts = new List<string>();

		public static Vector3? ClientSpawnLocation;

		private readonly ConcurrentDictionary<string, (Profile, Vector3, ESpawnState)> PlayersToSpawn = new ConcurrentDictionary<string, (Profile, Vector3, ESpawnState)>();

		/// <summary>
		/// Player, Time spawned -> Handled by UpdateAddPlayersToAICalc
		/// </summary>
		private readonly List<(EFT.LocalPlayer, DateTime)> PlayersToAddActivePlayerToAI = new List<(EFT.LocalPlayer, DateTime)>();

		private readonly ConcurrentDictionary<string, string> AccountsLoading = new ConcurrentDictionary<string, string>();

		bool doingclientwork = false;

		bool doingClientSpawnPlayersWork = false;

		private SITNetworkManager HostNetworkManager { get; set; }
		private NetworkClient HostNetworkClient { get; set; }

		public static ConnectionConfig GetConnectionConfig()
        {
			var cc = new ConnectionConfig() { };
			return cc;
		}

		//public static MatchInfo GetMatchInfo()
		//{
		//	MatchInfo matchInfo = new MatchInfo();
		//	matchInfo.address = "127.0.0.1";
		//	matchInfo.port = 5555;
		//	matchInfo.usingRelay = false;
		//	return matchInfo;
		//}

		#region Unity Component Methods

		void Awake()
		{
			// ----------------------------------------------------
			// Always clear "Players" when creating a new CoopGameComponent
			Players.Clear();

			// ----------------------------------------------------
			// Create a BepInEx Logger for CoopGameComponent
			Logger = BepInEx.Logging.Logger.CreateLogSource("CoopGameComponent");

			//HostNetworkManager = this.GetOrAddComponent<SITNetworkManager>();
			//if (NetworkManager.singleton == null)
			//	NetworkManager.singleton = HostNetworkManager;

			//if (HostNetworkManager != null && MatchmakerAcceptPatches.IsServer && HostNetworkClient == null)
			//{
			//	Logger.LogInfo("HostNetworkClient: Start Host");
			//	HostNetworkManager.offlineScene = Plugin.CurrentScene.name;
			//	HostNetworkManager.onlineScene = Plugin.CurrentScene.name;
			//	HostNetworkClient = HostNetworkManager.StartHost(GetMatchInfo());
			//	HostNetworkClient.RegisterHandler(0, (NetworkMessage message) => {

			//		Logger.LogInfo("Host Client Received 0 Message");

			//	});
			//	HostNetworkClient.RegisterHandler(999, (NetworkMessage message) => {

			//		Logger.LogInfo("Host Client Received 999 Message"); 
					
			//	});
			//}
		}

		void Start()
		{
			//Logger.LogInfo("CoopGameComponent:Start");


			// ----------------------------------------------------
			// Consume Data Received from ServerCommunication class
            ServerCommunication.OnDataReceived += ServerCommunication_OnDataReceived;
            ServerCommunication.OnDataStringReceived += ServerCommunication_OnDataStringReceived;
            ServerCommunication.OnDataArrayReceived += ServerCommunication_OnDataArrayReceived;
			// ----------------------------------------------------
		}

		void FixedUpdate()
		{
			//if (!doingClientSpawnPlayersWork)
			{
				doingClientSpawnPlayersWork = true;
				UpdateClientSpawnPlayers();
				doingClientSpawnPlayersWork = false;
			}
		}

	
		void Update()
		{
			//if (!doingclientwork)
			{
				doingclientwork = true;
				//UpdateParityCheck();
				//UpdateAddPlayersToAICalc();
				RunQueuedActions();

				

				doingclientwork = false;
			}
		}

        #endregion

        private void ServerCommunication_OnDataArrayReceived(string[] array)
        {
			try
			{
				foreach (var item in array)
				{
					if (item.Length == 4)
					{
						return;
					}
					else
					{
						Task.Run(() =>
						{
							//Logger.LogInfo("received array: item: " + item);
							var parsedItem = Json.Deserialize<Dictionary<string, object>>(item);
							if (parsedItem != null)
							{
								QueuedPackets.Enqueue(parsedItem);
							}
						});
					}
				}
			}
			catch (Exception)
            {

            }
		}

        private void ServerCommunication_OnDataStringReceived(string @string)
        {

        }

        private void ServerCommunication_OnDataReceived(byte[] buffer)
        {
			if (buffer.Length == 0)
				return;

			try
			{
				//string @string = streamReader.ReadToEnd();
				string @string = UTF8Encoding.UTF8.GetString(buffer);

				if (@string.Length == 4)
				{
					return;
				}
				else
				{
					Task.Run(() =>
					{
                        //Logger.LogInfo($"CoopGameComponent:OnDataReceived:{buffer.Length}");
                        //Logger.LogInfo($"CoopGameComponent:OnDataReceived:{@string}");

						if (@string.Length > 0 && @string[0] == '{' && @string[@string.Length - 1] == '}')
						{
							var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(@string);

							if (dictionary != null && dictionary.Count > 0)
							{
								if (dictionary.ContainsKey("SERVER"))
								{
									//Logger.LogInfo($"LocalGameStartingPatch:OnDataReceived:SERVER:{buffer.Length}");
									QueuedPackets.Enqueue(dictionary);
								}
								else if(dictionary.ContainsKey("m"))
                                {
									if(dictionary["m"].ToString() == "HostDied")
                                    {
										Logger.LogInfo("Host Died");
										if (MatchmakerAcceptPatches.IsClient)
											LocalGameEndingPatch.EndSession(LocalGamePatches.LocalGameInstance, LocalGamePatches.MyPlayerProfile.Id, EFT.ExitStatus.Survived, "", 0);
									}
                                }
							}
						}
					});
				}
			}
			catch (Exception ex2)
			{
				return;
			}
		}

		private static DateTime LastPeopleParityCheck = DateTime.Now;


		public static EFT.LocalPlayer GetPlayerByAccountId(string accountId)
		{
			try
			{
				if (LocalGamePatches.MyPlayerProfile != null && LocalGamePatches.MyPlayerProfile.AccountId == accountId)
					return LocalGamePatches.MyPlayer as EFT.LocalPlayer;

				if (Players != null && Players.ContainsKey(accountId))
					return Players[accountId];

			}
			catch (Exception)
            {
            }


			//PatchConstants.Logger.LogInfo($"Failed to find Profile of {accountId} in Players list");
			// -----------------------------------------------------------
			// Unable to find profile. Ask Server to resend the data
			//if (LastPeopleParityCheck < DateTime.Now.AddSeconds(-30))
			//{
			//if (!PeopleParityRequestedAccounts.Contains(accountId))
			//{
			//	PeopleParityRequestedAccounts.Add(accountId);
			//	_ = ServerCommunication.SendDataDownWebSocket("PeopleParityRequest=" + accountId + "=" + LocalGamePatches.MyPlayerProfile.AccountId);
			//}
			//	LastPeopleParityCheck = DateTime.Now;
			//}
			return null;

		}

	

		private void DataReceivedClient_PlayerBotSpawn(Dictionary<string, object> parsedDict, string accountId, string profileId, Vector3 newPosition, bool isBot)
		{
			//Logger.LogInfo("DataReceivedClient_PlayerBotSpawn");

			if (LocalGamePatches.MyPlayerProfile == null)
			{
				Logger.LogInfo("LocalGamePatches.MyPlayerProfile is NULL");
				return;
			}

			if (
				!Players.ContainsKey(accountId) && LocalGamePatches.MyPlayerProfile.AccountId != accountId
				&&
				!PlayersToSpawn.ContainsKey(accountId)
				)
			{
				try
				{

					Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Adding " + accountId + " to spawner list");
					this.AccountsLoading.TryAdd(accountId, null);
					Profile profile = LocalGamePatches.MyPlayerProfile.Clone();
					profile.AccountId = accountId;
					profile.Id = accountId;
					profile.Info.Nickname = "Dickhead " + Players.Count;
					profile.Info.Side = isBot ? EPlayerSide.Savage : EPlayerSide.Usec;
					if (parsedDict.ContainsKey("p.info"))
					{
						Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Converting Profile data");
						//profile.Info = parsedDict["p.info"].ToString().ParseJsonTo<ProfileData>(Array.Empty<JsonConverter>());
						profile.Info = JsonConvert.DeserializeObject<ProfileInfo>(parsedDict["p.info"].ToString());// PatchConstants.SITParseJson<ProfileInfo>(parsedDict["p.info"].ToString());//.ParseJsonTo<ProfileData>(Array.Empty<JsonConverter>());
						Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Converted Profile data:: Hello " + profile.Info.Nickname);
					}
					if (parsedDict.ContainsKey("p.cust"))
					{
						var parsedCust = parsedDict["p.cust"].ToString().ParseJsonTo<Dictionary<EBodyModelPart, string>>(Array.Empty<JsonConverter>());
						if (parsedCust != null && parsedCust.Any())
						{
							PatchConstants.SetFieldOrPropertyFromInstance(
								profile
								, "Customization"
								, Activator.CreateInstance(PatchConstants.TypeDictionary["Profile.Customization"], parsedCust)
								);
							Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Set Profile Customization for " + profile.Info.Nickname);

						}
					}
					if (parsedDict.ContainsKey("p.equip"))
					{
						var pEquip = parsedDict["p.equip"].ToString();
						//var equipment = parsedDict["p.equip"].ToString().ParseJsonTo<Equipment>(Array.Empty<JsonConverter>());
						var equipment = PatchConstants.SITParseJson<Equipment>(parsedDict["p.equip"].ToString());//.ParseJsonTo<Equipment>(Array.Empty<JsonConverter>());
						profile.Inventory.Equipment = equipment;
						Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Set Equipment for " + profile.Info.Nickname);

					}
					if (parsedDict.ContainsKey("isHost"))
					{
					}
					
					this.PlayersToSpawn.TryAdd(accountId, (profile, newPosition, ESpawnState.NotLoaded));
				}
				catch (Exception ex)
				{
					QuickLog($"DataReceivedClient_PlayerBotSpawn::ERROR::" + ex.Message);
				}
			}
			
		}



		private async Task<LocalPlayer> CreatePhysicalOtherPlayerOrBot(Profile profile, Vector3 position)
		{
			try
			{
				EUpdateMode armsUpdateMode = EUpdateMode.Auto;
				EUpdateMode bodyUpdateMode = EUpdateMode.Auto;
				//var updateQueue = PatchConstants.GetFieldOrPropertyFromInstance<EUpdateQueue>(LocalGamePatches.LocalGameInstance, "UpdateQueue", false);

				//if (!base.Status.IsRunned())
				//{
				//	return null;
				//}
				//int playerId = int.Parse(InvokeLocalGameInstanceMethod("method_13").ToString());
				if(Players == null)
				{
					QuickLog("Players is NULL wtf!");
					return null;
				}

				if (Players.ContainsKey(profile.AccountId))
				{
					QuickLog("Profile already exists. ignoring.");
					var newPlayerToSpawn = PlayersToSpawn[profile.AccountId];
					newPlayerToSpawn.Item3 = ESpawnState.Spawned;
					PlayersToSpawn[profile.AccountId] = newPlayerToSpawn;
					return null;
				}

				int playerId = Players.Count + 1;
				if(profile == null)
                {
					QuickLog("CreatePhysicalOtherPlayerOrBot profile is NULL wtf!");
					return null;
                }

				if(PatchConstants.TypeDictionary["StatisticsSession"] == null)
                {
					QuickLog("StatisticsSession is NULL wtf!");
					return null;
				}

				if (PatchConstants.CharacterControllerSettings.ClientPlayerMode == null)
				{
					QuickLog("PatchConstants.CharacterControllerSettings.ClientPlayerMode is NULL wtf!");
					return null;
				}

				profile.SetSpawnedInSession(true);

				QuickLog("CreatePhysicalOtherPlayerOrBot: Attempting to Create Player " + profile.Info.Nickname);

				var localPlayer = await EFT.LocalPlayer.Create(
						playerId
						, position
						, Quaternion.identity
						, "Player"
						, ""
						, EPointOfView.ThirdPerson
						, profile
                        , false
                        , EUpdateQueue.Update
						, armsUpdateMode
						, bodyUpdateMode
						, PatchConstants.CharacterControllerSettings.ClientPlayerMode
						, () => 1f
						, () => 1f
						, (IGStatisticsSession2)Activator.CreateInstance(PatchConstants.TypeDictionary["StatisticsSession"])
						, GFilterCustomization1.Default
						, null
						, false
					);
				localPlayer.Transform.position = position;
				
				return localPlayer;
			}
			catch (Exception ex)
			{
				QuickLog(ex.ToString());
			}

			return null;
		}


		public static ConcurrentDictionary<string, EFT.LocalPlayer> Players { get; } = new ConcurrentDictionary<string, EFT.LocalPlayer>();

		public static ConcurrentQueue<Dictionary<string, object>> QueuedPackets { get; } = new ConcurrentQueue<Dictionary<string, object>>();

		void RunQueuedActions()
		{
			if (QueuedPackets.Any())
            {
				if(QueuedPackets.TryDequeue(out var queuedPacket))
                {
					if(queuedPacket != null)
                    {
						if(queuedPacket.ContainsKey("m"))
                        {
							var method = queuedPacket["m"];
                            //PatchConstants.Logger.LogInfo("CoopGameComponent.RunQueuedActions:method:" + method);
                            switch (method)
                            {
								case "PlayerSpawn":
									string accountId = queuedPacket["accountId"].ToString();
                                    if (Players != null && !Players.ContainsKey(accountId))
                                    {

                                        Vector3 newPosition = Players.First().Value.Position;
										if (queuedPacket.ContainsKey("sPx") 
											&& queuedPacket.ContainsKey("sPy") 
											&& queuedPacket.ContainsKey("sPz"))
										{
											string npxString = queuedPacket["sPx"].ToString();
											newPosition.x = float.Parse(npxString);
											string npyString = queuedPacket["sPy"].ToString();
											newPosition.y = float.Parse(npyString);
											string npzString = queuedPacket["sPz"].ToString();
											newPosition.z = float.Parse(npzString) + 0.5f;

											//QuickLog("New Position found for Spawning Player");
										}
										this.DataReceivedClient_PlayerBotSpawn(queuedPacket, accountId, queuedPacket["profileId"].ToString(), newPosition, false);
                                    }
                                    else
                                    {
                                        Logger.LogInfo($"Ignoring call to Spawn player {accountId}. The player already exists in the game.");
                                    }
                                    break;
								case "HostDied":
									{
										Logger.LogInfo("Host Died");
										if(MatchmakerAcceptPatches.IsClient)
											LocalGameEndingPatch.EndSession(LocalGamePatches.LocalGameInstance, LocalGamePatches.MyPlayerProfile.Id, EFT.ExitStatus.Survived, "", 0);
									}
									break;
							}
						}
                    }
                }
            }
		}

		private async void UpdateClientSpawnPlayers()
		{
			try
			{
				if (!PlayersToSpawn.Any())
					return;


				var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

				var accountId = PlayersToSpawn.Keys.First();
				if (Players.ContainsKey(accountId))
					// if this keeps occuring then something is stuck underneath!
					return; 

				var newPlayerToSpawn = PlayersToSpawn[accountId];

				switch (newPlayerToSpawn.Item3)
				{
					case ESpawnState.NotLoaded:
						newPlayerToSpawn.Item3 = ESpawnState.Loading;
						PlayersToSpawn[accountId] = newPlayerToSpawn;

						this.QuickLog("Update::Loading a new Player " + newPlayerToSpawn.Item1.Nickname);
                        IEnumerable<ResourceKey> allPrefabPaths = newPlayerToSpawn.Item1.GetAllPrefabPaths(false);
						if (allPrefabPaths.Count() > 0)
                        {
                            Singleton<JobScheduler>.Instance.SetForceMode(enable: true);
							Task loadBundle = Singleton<PoolManager>
								.Instance
								.LoadBundlesAndCreatePools(
								PoolManager.PoolsCategory.Raid
								, PoolManager.AssemblyType.Local
								, allPrefabPaths.ToArray()
								, GJobYield.General
								, new GProgress<SProgress>(delegate (SProgress p)
							{
								//this.QuickLog($"Update::Loading a new Player {newPlayerToSpawn.Item1.Nickname}:{p.Stage}:{p.Progress}");
							}));
							await loadBundle;
							newPlayerToSpawn.Item3 = ESpawnState.Loaded;
							//.ContinueWith((Task t) =>
							//{
							//	newPlayerToSpawn.Item3 = ESpawnState.Loaded;
							//});
						}
						else
						{
							QuickLog("Update::New Player has no PrefabPaths. Deleting.");
							PlayersToSpawn.TryRemove(accountId, out _);
						}
						break;
					case ESpawnState.Loaded:
						newPlayerToSpawn.Item3 = ESpawnState.Spawning;
						PlayersToSpawn[accountId] = newPlayerToSpawn;

						this.QuickLog("Update::Spawning a new >> Loaded << Player " + newPlayerToSpawn.Item1.AccountId);
						newPlayerToSpawn.Item1.SetSpawnedInSession(true);
						Vector3 spawnPosition = newPlayerToSpawn.Item2;
						try
						{
							var result = this.CreatePhysicalOtherPlayerOrBot(newPlayerToSpawn.Item1, spawnPosition).Result;
							if (result != null)
							{
								newPlayerToSpawn.Item3 = ESpawnState.Spawned;
								//this.SetWeaponInHandsOfNewPlayer(result);

								if (Players.TryAdd(newPlayerToSpawn.Item1.AccountId, result))
								{
									QuickLog($"Added new Player {newPlayerToSpawn.Item1.AccountId} to Players");
									var prc = result.GetOrAddComponent<PlayerReplicatedComponent>();
									prc.player = result;
									PlayersToAddActivePlayerToAI.Add((result, DateTime.Now));
									PlayersToSpawn.TryRemove(accountId, out _);
									result.Teleport(spawnPosition, true);

								}
								else
								{
									this.QuickLog("Unable to Add new Player to Players Collection");
								}
							}
							else
							{
								this.QuickLog("Failed to create & spawn the player " + newPlayerToSpawn.Item1.AccountId);
								newPlayerToSpawn.Item3 = ESpawnState.Loaded;
							}
						}
						catch
                        {
							newPlayerToSpawn.Item3 = ESpawnState.Loaded;
						}
						break;
				}

				PlayersToSpawn[accountId] = newPlayerToSpawn;


			}
			catch (Exception ex)
			{
				this.QuickLog(ex.ToString());
			}
		}

		private void SetWeaponInHandsOfNewPlayer(LocalPlayer person)
		{
			Equipment equipment = person.Profile.Inventory.Equipment;
			if (equipment == null)
			{
				return;
			}
			Item item = equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem;
			if (item == null)
			{
				this.QuickLog("SetWeaponInHandsOfNewPlayer:FirstPrimaryWeapon is NULL");
				item = equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem;
			}
			if (item == null)
			{
				this.QuickLog("SetWeaponInHandsOfNewPlayer:SecondPrimaryWeapon is NULL");
				item = equipment.GetSlot(EquipmentSlot.Holster).ContainedItem;
			}
			if (item == null)
			{
				this.QuickLog("SetWeaponInHandsOfNewPlayer:Holster is NULL");
				item = equipment.GetSlot(EquipmentSlot.Scabbard).ContainedItem;
			}
			if (item == null)
			{
				return;
			}
			//person.SetItemInHands(item, null);

			SetItemInHandsOfPlayer(person, item);
		}

		public static void SetItemInHandsOfPlayer(LocalPlayer person, Item item)
		{
			person.SetItemInHands(item, null);
		}


		

		void UpdateAddPlayersToAICalc()
        {
            if (PlayersToAddActivePlayerToAI.Any())
            {
				List<string> playersRemoved = new List<string>();
				foreach (var p in PlayersToAddActivePlayerToAI)
                {
					if(p.Item2 < DateTime.Now.AddSeconds(-10))
                    {
						BotSystemHelpers.AddActivePlayer(p.Item1);
						playersRemoved.Add(p.Item1.Profile.AccountId);
					}
                }
				PlayersToAddActivePlayerToAI.RemoveAll(x => playersRemoved.Contains(x.Item1.Profile.AccountId));
			}
        }


		private BepInEx.Logging.ManualLogSource logSource;

		void QuickLog(string log)
		{
			//if(logSource == null)
			//	logSource = new BepInEx.Logging.ManualLogSource("CoopGameComponent");

			//logSource.LogInfo(log);
			Logger.LogInfo(log);
		}

	}

	
}
