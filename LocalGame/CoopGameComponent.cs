using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.Coop.Core.Matchmaker;
using SIT.Coop.Core.Player;
using SIT.Tarkov.Core;
using SIT.Tarkov.Core.AI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.LocalGame
{

	public class CoopGameComponent : MonoBehaviour
	{
		public static Type PoolManagerType;

		List<string> MethodsToReplicateToMyPlayer = new List<string>()
			{
				"Dead",
				"Damage",
			};

		void Awake()
		{
			PatchConstants.Logger.LogInfo("CoopGameComponent:Awake");
			Players.Clear();

			if (PoolManagerType == null)
			{
				PoolManagerType = PatchConstants.EftTypes.Single(x => PatchConstants.GetAllMethodsForType(x).Any(x => x.Name == "LoadBundlesAndCreatePools"));
			}
		}

		void Start()
		{
			PatchConstants.Logger.LogInfo("CoopGameComponent:Start");
			//((MonoBehaviour)this).InvokeRepeating("RunQueuedActions", 0, 0.33f);
			//((MonoBehaviour)this).InvokeRepeating("RunParityCheck", 0, 1f);
		}

		public static EFT.LocalPlayer GetPlayerByAccountId(string accountId)
		{
			try
			{
				if (LocalGamePatches.MyPlayerProfile != null && LocalGamePatches.MyPlayerProfile.AccountId == accountId)
					return LocalGamePatches.MyPlayer as EFT.LocalPlayer;

				if (Players != null && Players.ContainsKey(accountId))
					return Players[accountId];

				//var allPlayers = FindObjectsOfType<EFT.LocalPlayer>();
				//if (allPlayers != null)
				//{
				//	var player = allPlayers.FirstOrDefault(x => x.Profile.AccountId == accountId);
				//	if (player == null)
				//	{
				//		//PatchConstants.Logger.LogInfo($"Unable to find Profile of {accountId}");
				//		return null;
				//	}

				//	PatchConstants.Logger.LogInfo($"Adding Profile of {accountId} to Players list");
				//	Players.TryAdd(accountId, player);
				//	return player;
				//}
			}
			catch (Exception)
            {
            }

			return null;

		}

		private readonly ConcurrentDictionary<string, (Profile, Vector3, ESpawnState)> PlayersToSpawn = new ConcurrentDictionary<string, (Profile, Vector3, ESpawnState)>();

		/// <summary>
		/// Player, Time spawned -> Handled by UpdateAddPlayersToAICalc
		/// </summary>
		private readonly List<(EFT.LocalPlayer, DateTime)> PlayersToAddActivePlayerToAI = new List<(EFT.LocalPlayer, DateTime)>();

		private readonly ConcurrentDictionary<string, string> AccountsLoading = new ConcurrentDictionary<string, string>();

		private void DataReceivedClient_PlayerBotSpawn(Dictionary<string, object> parsedDict, string accountId, string profileId, Vector3 newPosition, bool isBot)
		{
			if (LocalGamePatches.MyPlayerProfile == null)
				return;

			if (
				!Players.ContainsKey(accountId) && LocalGamePatches.MyPlayerProfile.AccountId != accountId
				&&
				!PlayersToSpawn.ContainsKey(accountId)
				)
			{
				try
				{

					PatchConstants.Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Adding " + accountId + " to spawner list");
					this.AccountsLoading.TryAdd(accountId, null);
					Profile profile = LocalGamePatches.MyPlayerProfile.Clone();
					profile.AccountId = accountId;
					profile.Id = profileId;
					profile.Info.Nickname = "Dickhead " + Players.Count;
					profile.Info.Side = isBot ? EPlayerSide.Savage : EPlayerSide.Usec;
					if (parsedDict.ContainsKey("p.info"))
					{
						var jobInfo = JObject.Parse(parsedDict["p.info"].ToString());
						profile.Info.Nickname = jobInfo.Property("Nickname").Value.ToString();
						profile.Info.Side = (EPlayerSide)Enum.Parse(typeof(EPlayerSide), jobInfo.Property("Side").Value.ToString());
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
						//var equipment = parsedDict["p.equip"].ToString().ParseJsonTo<GClass2058>(Array.Empty<JsonConverter>());
						//profile.Inventory.Equipment = equipment;
					}
					if (parsedDict.ContainsKey("isHost"))
					{
					}
					//if (isBot)
					//{
					//	this.BotsToSpawn.Enqueue((profile, newPosition, false));
					//}
					//else
					//{
					this.PlayersToSpawn.TryAdd(accountId, (profile, newPosition, ESpawnState.NotLoaded));
					//}
				}
				catch (Exception ex)
				{
					QuickLog($"DataReceivedClient_PlayerBotSpawn::ERROR::" + ex.Message);
					//QuickLog(ex.ToString());
				}
			}
			else
			{
				//QuickLog($"DataReceivedClient_PlayerBotSpawn::Attempting to Re-Process {accountId}, ignoring");
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
				int playerId = Players.Count + 1;
				profile.SetSpawnedInSession(true);

				/* TODO. Do this shit next private static async Task<object> CallGetByReflection(IFoo foo, IBar bar)
        {
            var method = typeof(IFoo).GetMethod(nameof(IFoo.Get));
            var generic = method.MakeGenericMethod(bar.GetType());
            var task = (Task) generic.Invoke(foo, new[] {bar});

            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty.GetValue(task);
        }
				 */

				var createMethod = typeof(LocalPlayer).GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var localPlayer = (Task<LocalPlayer>)createMethod.Invoke(
					null,
					//onstants.InvokeAsyncMethod(typeof(LocalPlayer), typeof(LocalPlayer), "Create",
					new object[] {
						playerId
						, position
						, Quaternion.identity
						, "Player"
						, ""
						, EPointOfView.ThirdPerson
						, profile
						, false
						, EUpdateQueue.Update //PatchConstants.GetAllPropertiesForObject(LocalGameInstance).FirstOrDefault(x=>x.Name == "UpdateQueue").GetValue(LocalGameInstance)
						, armsUpdateMode
						, bodyUpdateMode
						//, GClass523.Config.CharacterController.ClientPlayerMode // TODO: Make this dynamic/reflected
						, PatchConstants.CharacterControllerSettings.ClientPlayerMode
						, () => 1f
						, () => 1f
						, 0
						//, new GClass1478()
						, Activator.CreateInstance(PatchConstants.TypeDictionary["StatisticsSession"])
						, PatchConstants.GetFieldFromType(PatchConstants.TypeDictionary["FilterCustomization"], "Default").GetValue(null)
						, null
						, false
					}

					);

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
				return await localPlayer;
			}
			catch (Exception ex)
			{
				QuickLog(ex.ToString());
			}

			//return localPlayer;
			return null;
		}


		public static ConcurrentDictionary<string, EFT.LocalPlayer> OldPlayers { get; } = new ConcurrentDictionary<string, EFT.LocalPlayer>();
		public static ConcurrentDictionary<string, EFT.LocalPlayer> Players { get; } = new ConcurrentDictionary<string, EFT.LocalPlayer>();

		public static ConcurrentDictionary<string, ConcurrentQueue<Dictionary<string, object>>> ClientQueuedActions { get; } = new ConcurrentDictionary<string, ConcurrentQueue<Dictionary<string, object>>>();

		public static ConcurrentDictionary<EFT.LocalPlayer, Dictionary<string, object>> ServerReliablePackets { get; } 
			= new ConcurrentDictionary<EFT.LocalPlayer, Dictionary<string, object>>();


		void RunQueuedActions()
		{
			//Task.Run(() =>
			{
				if (ClientQueuedActions == null)
					return;

				if (ClientQueuedActions.Any())
				{
					try
					{
						//foreach (KeyValuePair<string, Queue<Dictionary<string, object>>> clientQueuedAction in this.ClientQueuedMoveActions)
						//UpdateClientQueuedActions_Move();

						foreach (KeyValuePair<string, ConcurrentQueue<Dictionary<string, object>>> clientQueuedAction in ClientQueuedActions)
						{
							string key = clientQueuedAction.Key;
							ConcurrentQueue<Dictionary<string, object>> value = clientQueuedAction.Value;
							if (value == null)
								continue;

                            while (value.Any())
                            //if (value.Any())
                            {
								bool dequeued = value.TryDequeue(out Dictionary<string, object> dictionary);
								if (!dequeued)
									continue;

								if (dictionary == null)
									continue;

								if (!dictionary.ContainsKey("accountId") || !dictionary.ContainsKey("m"))
									continue;

								if (LocalGamePatches.MyPlayerProfile == null)
									continue;

								string accountid = dictionary["accountId"].ToString();
								var player = GetPlayerByAccountId(accountid);
								if (player == null)
									continue;

								player.GetOrAddComponent<PlayerReplicatedComponent>().QueuedPackets.Enqueue(dictionary);

								EBodyPart eBodyPart = EBodyPart.Common;
								if (dictionary.ContainsKey("bodyPart"))
									eBodyPart = (EBodyPart)Enum.Parse(typeof(EBodyPart), dictionary["bodyPart"].ToString());

								var method = dictionary["m"].ToString();
								//if (
								//	(accountid == LocalGamePatches.MyPlayerProfile.AccountId || (Matchmaker.MatchmakerAcceptPatches.IsServer && player.IsAI))
								//	&& !MethodsToReplicateToMyPlayer.Contains(method))
								//	continue;


								//switch (method)
								//{
								//	case "Damage":
								//		//PatchConstants.Logger.LogInfo("Damage");
								//		PlayerOnDamagePatch.DamageReplicated(player, dictionary);
								//		break;
								//	case "Dead":
								//		PatchConstants.Logger.LogInfo("Dead");
								//		break;
								//	case "Door":
								//		PatchConstants.Logger.LogInfo("Door");
								//		break;
								//	//case "Move":
        // //                               PlayerOnMovePatch.MoveReplicated(player, dictionary);
        // //                               break;
								//	case "Rotate":
        //                                //PatchConstants.Logger.LogInfo("Rotate");
        //                                PlayerOnRotatePatch.RotateReplicated(player, dictionary);
        //                                break;
								//	case "Say":
								//		break;
								//	case "SetTriggerPressed":
								//		break;
								//	case "SetItemsInHands":
								//		break;
								//	case "InventoryOpened":
								//		PlayerOnInventoryOpenedPatch.InventoryOpenedReplicated(player, dictionary);
								//		break;

								//}
							}
						}
					}
					catch (Exception ex)
					{
						PatchConstants.Logger.LogInfo($"{ex}");

					}
				}
				//});
			}
		}

		DateTime? LastParityCheck = DateTime.Now;

		async void UpdateParityCheck()
		{
			await Task.Run(() => {
				try
				{
					//if (Status != GameStatus.Stopped && this.LastParityCheck < DateTime.Now.AddSeconds(-5.0))
					if (this.LastParityCheck < DateTime.Now.AddSeconds(-5.0))
					{
						this.LastParityCheck = DateTime.Now;
						Dictionary<string, object> dictionary = new Dictionary<string, object>();
						dictionary.Add("m", "clientRequestForParity");
						//QuickLog("RunParityCheck:" + MatchmakerAcceptPatches.GetGroupId());
						dictionary.Add("groupId", MatchmakerAcceptPatches.GetGroupId());
						//dictionary.Add("count", (this.BotDictionary.Count + this.Players.Count).ToString());
						dictionary.Add("count", Players.Count);
						//if (MatchmakerAcceptPatches.IsClient)
						//{
						//	dictionary.Add("botKeys", JsonConvert.SerializeObject(this.BotDictionary.Keys));
						//	dictionary.Add("playerKeys", JsonConvert.SerializeObject(this.Players.Keys));
						//}
						if (MatchmakerAcceptPatches.IsServer)
						{
							//dictionary.Add("gameTime", this.dateTime_0.ToString());
							//List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
							//dictionary.Add("botData", list);
							//foreach (KeyValuePair<string, Player> item in this.BotDictionary)
							//{
							//	Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
							//	dictionary2.Add("accountId", item.Key);
							//	dictionary2.Add("profileId", item.Key);
							//	dictionary2.Add("sPx", item.Value.Position.x);
							//	dictionary2.Add("sPy", item.Value.Position.y);
							//	dictionary2.Add("sPz", item.Value.Position.z);
							//	dictionary2.Add("p.info", item.Value.Profile.Info);
							//	dictionary2.Add("p.cust", item.Value.Profile.Customization);
							//	dictionary2.Add("p.equip", item.Value.Profile.Inventory.Equipment);
							//	list.Add(dictionary2);
							//}
							//dictionary["botData"] = list;
							List<Dictionary<string, object>> list2 = new List<Dictionary<string, object>>();
							dictionary.Add("playerData", list2);
							foreach (KeyValuePair<string, EFT.LocalPlayer> player in Players)
							{

								Dictionary<string, object> playerData = new Dictionary<string, object>();
								playerData.Add("accountId", player.Key);
								playerData.Add("profileId", player.Key);
								var p = GetPlayerByAccountId(player.Key);
								if (p != null)
								{
									var pp = PatchConstants.GetPlayerProfile(p);
									//if(pp != null)
									//                              {
									//	var ppInfo = PatchConstants.GetFieldOrPropertyFromInstance<object>(pp, "Info");
									//	if (ppInfo != null)
									//                                  {
									//		playerData.Add("p.info", ppInfo);
									//	}

									//	var ppCust = PatchConstants.GetFieldOrPropertyFromInstance<object>(pp, "Customization");
									//	if (ppCust != null)
									//	{
									//		playerData.Add("p.cust", ppCust);
									//	}
									//}
								}
								playerData.Add("sPx", player.Value.Position.x);
								playerData.Add("sPy", player.Value.Position.y);
								playerData.Add("sPz", player.Value.Position.z);
								playerData.Add("p.info", player.Value.Profile.Info);
								playerData.Add("p.cust", player.Value.Profile.Customization);
								playerData.Add("p.equip", player.Value.Profile.Inventory.Equipment);
								list2.Add(playerData);
							}
							dictionary["playerData"] = list2;
						}

						//MatchMakerAcceptScreen.ServerCommunicationCoopImplementation.SendDataDownWebSocket(dictionary);

						string text = new Request().PostJson("/client/match/group/server/parity", dictionary.SITToJson());
						if (!string.IsNullOrEmpty(text))
						{
							//QuickLog(text);
							ParityCheckHandleReturn(text);
						}

						//string text2 = new Request().PostJson("/client/match/group/server/parity/players", dictionary.ToJson());
						//ParityCheckHandleReturn(text2);

						//string text3 = new Request().PostJson("/client/match/group/server/parity/bots", dictionary.ToJson());
						//ParityCheckHandleReturn(text3);

						//string text4 = new Request().PostJson("/client/match/group/server/parity/dead/get",
						//	data: MatchmakerAcceptPatches.GetGroupId().ToJson());
						//if (text != "ERROR")
						//{
						//	QuickLog(text4);
						//}
					}

				}
				catch (Exception)
				{
					//QuickLog("ParityCheck::ERROR::" + ex.ToString());
				}
			});
		}

		private void ParityCheckHandleReturn(string text)
		{
			try
			{
				if (!string.IsNullOrEmpty(text) && !text.StartsWith("OK") && !text.Contains("ERROR"))
				{
					//var dictionary = text.ParseJsonTo<Dictionary<string, object>>(Array.Empty<JsonConverter>());
					var dictionary = text.SITParseJson<Dictionary<string, object>>();
					if (dictionary == null)
						return;

					if (dictionary.Count > 0)
					{
						//this.QuickLog("ParityCheck::Data Received from Central Server::" + text.Length);
						if (dictionary.ContainsKey("players"))
						{
							List<Dictionary<string, object>> returnedPlayers = dictionary["players"].ToJson().ParseJsonTo<List<Dictionary<string, object>>>(Array.Empty<JsonConverter>());
							if (returnedPlayers != null)
							{
								//this.QuickLog("ParityCheck::Data Received from Central Server::PlayerListData::Count::" + list3.Count);
								//foreach (var item in list3)
								//{
								//	QuickLog(string.Join(", ", item.Keys));
								//}
								foreach (var item in returnedPlayers)
								{
									if (item == null)
										continue;

									if (item.ContainsKey("accountId"))
									{
										string accountId = item["accountId"].ToString();
										if (Players == null || Players.Count == 0)
											continue;

										if (OldPlayers.ContainsKey(accountId))
											continue;

										Vector3 newPosition = Players.First().Value.Position;
										if (item.ContainsKey("sPx") && item.ContainsKey("sPy") && item.ContainsKey("sPz"))
										{
											string npxString = item["sPx"].ToString();
											newPosition.x = float.Parse(npxString);
											string npyString = item["sPy"].ToString();
											newPosition.y = float.Parse(npyString);
											string npzString = item["sPz"].ToString();
											newPosition.z = float.Parse(npzString);

											//QuickLog("New Position found for Spawning Player");
										}
										this.DataReceivedClient_PlayerBotSpawn(item, accountId, item["profileId"].ToString(), newPosition, false);
									}
									else
									{
										this.QuickLog("couldn't process parity data, no accountId");
									}
								}
							}
							else
							{
								this.QuickLog("ParityCheck::Data Received from Central Server::Unable to convert playersListData");
							}
						}

						if (dictionary.ContainsKey("dead"))
						{
							List<Dictionary<string, object>> deads = dictionary["dead"].ToJson().ParseJsonTo<List<Dictionary<string, object>>>(Array.Empty<JsonConverter>());
							QuickLog($"Received: {deads.Count} dead people");
						}
					}
				}
			}
			catch (Exception ex)
			{
				QuickLog("ParityCheckHandleReturn ERROR");
				QuickLog(ex.ToString());
			}
		}

		private void UpdateClientSpawnPlayers()
		{
			try
			{
				if (!PlayersToSpawn.Any())
					return;

				var accountId = PlayersToSpawn.Keys.First();
				var newPlayerToSpawn = PlayersToSpawn[accountId];

				switch (newPlayerToSpawn.Item3)
				{
					case ESpawnState.NotLoaded:
						newPlayerToSpawn.Item3 = ESpawnState.Loading;
						this.QuickLog("Update::Loading a new Player " + newPlayerToSpawn.Item1.AccountId);
						IEnumerable<ResourceKey> allPrefabPaths = newPlayerToSpawn.Item1.GetAllPrefabPaths();
						if (allPrefabPaths.Count() > 0)
						{
							var loadTask = SIT.A.Tarkov.Core.Plugin.LoadBundlesAndCreatePools(allPrefabPaths.ToArray());
							if (loadTask != null)
							{
								loadTask.ConfigureAwait(false);
                                // TODO: Get this working
                                loadTask.ContinueWith(delegate
                                {
                                    //	newPlayerToSpawn.Item3 = ESpawnState.Loaded;
                                });
                                //loadTask.GetAwaiter().OnCompleted(() => {
                                //	newPlayerToSpawn.Item3 = ESpawnState.Loaded;
                                //});
                                newPlayerToSpawn.Item3 = ESpawnState.Loaded;

                            }

							//Singleton<GClass1481>.Instance.LoadBundlesAndCreatePools(GClass1481.PoolsCategory.Raid, GClass1481.AssemblyType.Local, allPrefabPaths.ToArray(), GClass2536.General)
							//	.ContinueWith(delegate
							//	{
							//		this.PlayersToSpawn.Enqueue((newPlayerToSpawn.Item1, newPlayerToSpawn.Item2, true));
							//	});
						}
						else
						{
							QuickLog("Update::New Player has no PrefabPaths. Deleting.");
							PlayersToSpawn.TryRemove(accountId, out _);
						}
						break;
					case ESpawnState.Loaded:
						newPlayerToSpawn.Item3 = ESpawnState.Spawning;

						this.QuickLog("Update::Spawning a new >> Loaded << Player " + newPlayerToSpawn.Item1.AccountId);
						newPlayerToSpawn.Item1.SetSpawnedInSession(true);
						Vector3 vector = newPlayerToSpawn.Item2;
						try
						{
							this.CreatePhysicalOtherPlayerOrBot(newPlayerToSpawn.Item1, vector).ContinueWith((x) =>
							{
								var result = x.Result;
								if (result != null)
								{
									newPlayerToSpawn.Item3 = ESpawnState.Spawned;

									if (Players.TryAdd(newPlayerToSpawn.Item1.AccountId, result))
									{
										QuickLog($"Added new Player {newPlayerToSpawn.Item1.AccountId} to Players");
										var prc = result.GetOrAddComponent<PlayerReplicatedComponent>();
										prc.player = result;
										PlayersToAddActivePlayerToAI.Add((result, DateTime.Now));
										
										//this.SetWeaponInHandsOfNewPlayer(result);
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
							});
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


		void FixedUpdate()
		{

		}

		bool doingclientwork = false;

		void Update()
		{
			if (!doingclientwork)
			{
				doingclientwork = true;
				UpdateParityCheck();
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
			PatchConstants.Logger.LogInfo(log);
		}

	}
}
