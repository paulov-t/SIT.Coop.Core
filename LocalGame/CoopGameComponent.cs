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
	public class CoopGameComponent : MonoBehaviour
	{
		BepInEx.Logging.ManualLogSource Logger;

		void Awake()
		{
			Players.Clear();

			Logger = BepInEx.Logging.Logger.CreateLogSource("CoopGameComponent");
			//Logger.LogInfo("CoopGameComponent:Awake");
		}

		void Start()
		{
			//Logger.LogInfo("CoopGameComponent:Start");
			//((MonoBehaviour)this).InvokeRepeating("RunQueuedActions", 0, 0.33f);
			//((MonoBehaviour)this).InvokeRepeating("RunParityCheck", 0, 1f);
            ServerCommunication.OnDataReceived += ServerCommunication_OnDataReceived;
            ServerCommunication.OnDataStringReceived += ServerCommunication_OnDataStringReceived;
            ServerCommunication.OnDataArrayReceived += ServerCommunication_OnDataArrayReceived;
		}

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

					//var parsedItem = Json.Deserialize<Dictionary<string, object>>(item);
					//if (parsedItem != null)
					//            {
					//	QueuedPackets.Enqueue(parsedItem);

					//	//if (parsedItem.ContainsKey("m") && parsedItem["m"].ToString() == "PlayerSpawn")
					// //               {

					//	//}
					//}
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
			//	_ = ServerCommunication.SendDataDownWebSocket("PeopleParity");
			//	LastPeopleParityCheck = DateTime.Now;
			//}
			return null;

		}

		public static Vector3? ClientSpawnLocation;

		private readonly ConcurrentDictionary<string, (Profile, Vector3, ESpawnState)> PlayersToSpawn = new ConcurrentDictionary<string, (Profile, Vector3, ESpawnState)>();

		/// <summary>
		/// Player, Time spawned -> Handled by UpdateAddPlayersToAICalc
		/// </summary>
		private readonly List<(EFT.LocalPlayer, DateTime)> PlayersToAddActivePlayerToAI = new List<(EFT.LocalPlayer, DateTime)>();

		private readonly ConcurrentDictionary<string, string> AccountsLoading = new ConcurrentDictionary<string, string>();

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
					profile.Id = profileId;
					profile.Info.Nickname = "Dickhead " + Players.Count;
					profile.Info.Side = isBot ? EPlayerSide.Savage : EPlayerSide.Usec;
					if (parsedDict.ContainsKey("p.info"))
					{
						var profileInfo = JObject.Parse(parsedDict["p.info"].ToString());
						profile.Info.Nickname = profileInfo.Property("Nickname").Value.ToString();
						profile.Info.Side = (EPlayerSide)Enum.Parse(typeof(EPlayerSide), profileInfo.Property("Side").Value.ToString());
						if(profileInfo.ContainsKey("Voice"))
                        {
							profile.Info.Voice = profileInfo["Voice"].ToString();
						}
						//var info = parsedDict["p.info"].ToString().ParseJsonTo<GClass1443>(Array.Empty<JsonConverter>());
						//if (info != null)
						//{
						//	profile.Info.Nickname = info.Nickname;
						//	profile.Info.Side = info.Side;
						//	profile.Info.Voice = info.Voice;
						//}
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
						}
					}
					if (parsedDict.ContainsKey("p.equip"))
					{
						var pEquip = parsedDict["p.equip"].ToString();
						var equipment = parsedDict["p.equip"].ToString().ParseJsonTo<GClass2140>(Array.Empty<JsonConverter>());
						profile.Inventory.Equipment = equipment;
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
				EFT.Player.EUpdateMode armsUpdateMode = EFT.Player.EUpdateMode.Auto;
				EFT.Player.EUpdateMode bodyUpdateMode = EFT.Player.EUpdateMode.Auto;
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

				var localPlayer = await EFT.LocalPlayer.Create(
						playerId
						, position
						, Quaternion.identity
						, "Player"
						, ""
						, EPointOfView.ThirdPerson
						, profile
                        , false
                        //, true
                        , EUpdateQueue.Update
						, armsUpdateMode
						, bodyUpdateMode
						, PatchConstants.CharacterControllerSettings.ClientPlayerMode
						, () => 1f
						, () => 1f
						, (GInterface118)Activator.CreateInstance(PatchConstants.TypeDictionary["StatisticsSession"])
						//, (GInterface74)PatchConstants.GetFieldFromType(PatchConstants.TypeDictionary["FilterCustomization"], "Default").GetValue(null)
						,GClass1271.Default
						, null
						, false
					);
				localPlayer.Transform.position = position;
				//var createMethod = typeof(LocalPlayer).GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				//var localPlayer = (Task<LocalPlayer>)createMethod.Invoke(
				//	null,
				//	//onstants.InvokeAsyncMethod(typeof(LocalPlayer), typeof(LocalPlayer), "Create",
				//	new object[] {
				//		playerId
				//		, position
				//		, Quaternion.identity
				//		, "Player"
				//		, ""
				//		, EPointOfView.ThirdPerson
				//		, profile
				//		, false
				//		, EUpdateQueue.Update //PatchConstants.GetAllPropertiesForObject(LocalGameInstance).FirstOrDefault(x=>x.Name == "UpdateQueue").GetValue(LocalGameInstance)
				//		, armsUpdateMode
				//		, bodyUpdateMode
				//		//, GClass523.Config.CharacterController.ClientPlayerMode // TODO: Make this dynamic/reflected
				//		, PatchConstants.CharacterControllerSettings.ClientPlayerMode
				//		, () => 1f
				//		, () => 1f
				//		, 0
				//		//, new GClass1478()
				//		, Activator.CreateInstance(PatchConstants.TypeDictionary["StatisticsSession"])
				//		, PatchConstants.GetFieldFromType(PatchConstants.TypeDictionary["FilterCustomization"], "Default").GetValue(null)
				//		, null
				//		, false
				//	}

				//	);

				//typeof(LocalPlayer).GetMethod("Create").inv

				//var localPlayer = await LocalPlayer.Create(playerId
				//	, position
				//	, Quaternion.identity
				//	, "Player"
				//	, ""
				//	, EPointOfView.ThirdPerson
				//	, profile
				//	, false
				//	, EUpdateQueue.Update //PatchConstants.GetAllPropertiesForObject(LocalGameInstance).FirstOrDefault(x=>x.Name == "UpdateQueue").GetValue(LocalGameInstance)
				//	, armsUpdateMode
				//	, bodyUpdateMode
				//                //, GClass523.Config.CharacterController.ClientPlayerMode // TODO: Make this dynamic/reflected
				//                , PatchConstants.CharacterControllerSettings.ClientPlayerMode // TODO: Make this dynamic/reflected
				//															//, CharacterControllerMode
				//	, () => 1f
				//	, () => 1f
				//	, 0
				//	//, new GClass1478()
				//	, Activator.CreateInstance(PatchConstants.TypeDictionary["StatisticsSession"])
				//	, GClass1206.Default
				//	, null
				//	, false);
				//return localPlayer;
				return localPlayer;
			}
			catch (Exception ex)
			{
				QuickLog(ex.ToString());
			}

			//return localPlayer;
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
						//this.QuickLog("Update::Loading a new Player " + newPlayerToSpawn.Item1.AccountId);
						IEnumerable<ResourceKey> allPrefabPaths = newPlayerToSpawn.Item1.GetAllPrefabPaths();
						if (allPrefabPaths.Count() > 0)
                        {
							Singleton<JobScheduler>.Instance.SetForceMode(enable: true);
							await Singleton<GClass1560>
								.Instance
								.LoadBundlesAndCreatePools(GClass1560.PoolsCategory.Raid, GClass1560.AssemblyType.Local, allPrefabPaths.ToArray(), GClass2637.General, new GClass2558<GStruct94>(delegate (GStruct94 p)
							{
								//this.QuickLog($"Update::Loading a new Player {newPlayerToSpawn.Item1.AccountId}:{p.Stage}:{p.Progress}");
							})).ContinueWith((Task t) => {
								newPlayerToSpawn.Item3 = ESpawnState.Loaded;
							});
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
							var result = await this.CreatePhysicalOtherPlayerOrBot(newPlayerToSpawn.Item1, spawnPosition);
							if (result != null)
							{
								newPlayerToSpawn.Item3 = ESpawnState.Spawned;
								this.SetWeaponInHandsOfNewPlayer(result);

								if (Players.TryAdd(newPlayerToSpawn.Item1.AccountId, result))
								{
									QuickLog($"Added new Player {newPlayerToSpawn.Item1.AccountId} to Players");
									var prc = result.GetOrAddComponent<PlayerReplicatedComponent>();
									prc.player = result;
									PlayersToAddActivePlayerToAI.Add((result, DateTime.Now));
									PlayersToSpawn.TryRemove(accountId, out _);
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
			GClass2140 equipment = person.Profile.Inventory.Equipment;
			if (equipment == null)
			{
				return;
			}
			Item item = equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem
				?? equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem
				?? equipment.GetSlot(EquipmentSlot.Holster).ContainedItem
				?? equipment.GetSlot(EquipmentSlot.Scabbard).ContainedItem;
			if (item == null)
			{
				return;
			}
			//person.SetItemInHands(item, null);

			SetItemInHandsOfPlayer(person, item);
		}

		private void SetItemInHandsOfPlayer(LocalPlayer person, Item item)
		{
			person.SetItemInHands(item, null);
		}


		void FixedUpdate()
		{

		}

		bool doingclientwork = false;

		void Update()
		{
			if (!doingclientwork)
			{
				doingclientwork = true;
				//UpdateParityCheck();
                UpdateClientSpawnPlayers();
				UpdateAddPlayersToAICalc();
				RunQueuedActions();

                doingclientwork = false;
			}
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
