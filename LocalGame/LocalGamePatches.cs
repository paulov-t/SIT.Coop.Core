using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SIT.Coop.Core.Matchmaker;
using SIT.Coop.Core.Player;
using SIT.Tarkov.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.LocalGame
{
	public class LocalGamePatches
	{
		public static object LocalGameInstance { get; set; }

		public static object InvokeLocalGameInstanceMethod(string methodName, params object[] p)
        {
			var method = PatchConstants.GetAllMethodsForType(LocalGameInstance.GetType()).FirstOrDefault(x => x.Name == methodName);
			if(method == null)
				method = PatchConstants.GetAllMethodsForType(LocalGameInstance.GetType().BaseType).FirstOrDefault(x => x.Name == methodName);

			if(method != null)
            {
				method.Invoke(method.IsStatic ? null : LocalGameInstance, p);
            }


			return null;
        }

		public static object MyPlayer { get; set; }

		public static EFT.Profile MyPlayerProfile
		{
			get
			{
				if (MyPlayer == null)
					return null;

				return PatchConstants.GetPlayerProfile(MyPlayer) as EFT.Profile;
			}
		}

		public static ConcurrentDictionary<string, EFT.LocalPlayer> Players = new ConcurrentDictionary<string, EFT.LocalPlayer>();

		public static ConcurrentDictionary<string, ConcurrentQueue<Dictionary<string, object>>> ClientQueuedActions = new ConcurrentDictionary<string, ConcurrentQueue<Dictionary<string, object>>>();

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

			private EFT.LocalPlayer GetPlayerByAccountId(string accountId)
			{
				if (MyPlayerProfile != null && MyPlayerProfile.AccountId == accountId)
					return MyPlayer as EFT.LocalPlayer;

				if (Players != null && Players.ContainsKey(accountId))
					return Players[accountId] as EFT.LocalPlayer;

				var player = GameObject.FindObjectsOfType<EFT.LocalPlayer>().FirstOrDefault(x => x.Profile.AccountId == accountId);
				if (player == null)
				{
					//PatchConstants.Logger.LogInfo($"Unable to find Profile of {accountId}");
					return null;
				}

				PatchConstants.Logger.LogInfo($"Adding Profile of {accountId} to Players list");
				Players.TryAdd(accountId, player);
				return player;

			}

			private readonly ConcurrentQueue<(Profile, Vector3, bool)> BotsToSpawn = new ConcurrentQueue<(Profile, Vector3, bool)>();

			private readonly ConcurrentQueue<(Profile, Vector3, bool)> PlayersToSpawn = new ConcurrentQueue<(Profile, Vector3, bool)>();

			private readonly ConcurrentDictionary<string, string> AccountsLoading = new ConcurrentDictionary<string, string>();

			private void DataReceivedClient_PlayerBotSpawn(Dictionary<string, object> parsedDict, string accountId, string profileId, Vector3 newPosition, bool isBot)
			{
				if (MyPlayerProfile == null)
					return;

				if (
					!LocalGamePatches.Players.ContainsKey(accountId) && MyPlayerProfile.AccountId != accountId
					&& ((isBot && !this.BotsToSpawn.Any(((Profile, Vector3, bool) x) => x.Item1.AccountId == accountId))
					|| (!isBot && !this.PlayersToSpawn.Any(((Profile, Vector3, bool) x) => x.Item1.AccountId == accountId))) && !this.AccountsLoading.ContainsKey(accountId))
				{
					try
					{

						PatchConstants.Logger.LogInfo("DataReceivedClient_PlayerBotSpawn:: Adding " + accountId + " to spawner list");
						this.AccountsLoading.TryAdd(accountId, null);
						Profile profile = MyPlayerProfile.Clone();
						profile.AccountId = accountId;
						profile.Id = profileId;
						profile.Info.Nickname = "Dickhead " + Players.Count;
						profile.Info.Side = isBot ? EPlayerSide.Savage : EPlayerSide.Usec;
						if (parsedDict.ContainsKey("p.info"))
						{
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
							//profile.Customization = parsedDict["p.cust"].ToString().ParseJsonTo<GClass1437>(Array.Empty<JsonConverter>());
						}
						if (parsedDict.ContainsKey("p.equip"))
						{
							//var equipment = parsedDict["p.equip"].ToString().ParseJsonTo<GClass2058>(Array.Empty<JsonConverter>());
							//profile.Inventory.Equipment = equipment;
						}
						if (parsedDict.ContainsKey("isHost"))
						{
						}
						if (isBot)
						{
							this.BotsToSpawn.Enqueue((profile, newPosition, false));
						}
						else
						{
							this.PlayersToSpawn.Enqueue((profile, newPosition, false));
						}
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
					//if (!base.Status.IsRunned())
					//{
					//	return null;
					//}
					//int playerId = int.Parse(InvokeLocalGameInstanceMethod("method_13").ToString());
					int playerId = Players.Count + 1;
					profile.SetSpawnedInSession(true);
					var localPlayer = await LocalPlayer.Create(playerId
						, position
						, Quaternion.identity
						, "Player"
						, ""
						, EPointOfView.ThirdPerson
						, profile
						, false
						, EUpdateQueue.Update //PatchConstants.GetAllPropertiesForObject(LocalGameInstance).FirstOrDefault(x=>x.Name == "UpdateQueue").GetValue(LocalGameInstance)
						, 0
						, 0
						, GClass523.Config.CharacterController.ClientPlayerMode
						, () => 0.7f
						, () => 0.7f
						, 0
						, default(GClass1481)
						, default(GClass1205)
						, default(GClass1756)
						, false);
					return localPlayer;
				}
				catch(Exception ex)
                {
					QuickLog(ex.ToString());
                }

				//return localPlayer;
				return null;
			}




			void RunQueuedActions()
            {
				Task.Run(() =>
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

								//while (value.Any())
								if (value.Any())
								{
									bool dequeued = value.TryDequeue(out Dictionary<string, object> dictionary);
									if (!dequeued)
										continue;

									if (dictionary == null)
										continue;

									if (!dictionary.ContainsKey("accountId") || !dictionary.ContainsKey("m"))
										continue;

									if (MyPlayerProfile == null)
										continue;

									string accountid = dictionary["accountId"].ToString();
									var player = GetPlayerByAccountId(accountid);
									if (player == null)
										continue;

									EBodyPart eBodyPart = EBodyPart.Common;
									if (dictionary.ContainsKey("bodyPart"))
										eBodyPart = (EBodyPart)Enum.Parse(typeof(EBodyPart), dictionary["bodyPart"].ToString());

									var method = dictionary["m"].ToString();
									if (
										(accountid == MyPlayerProfile.AccountId || (Matchmaker.MatchmakerAcceptPatches.IsServer && player.IsAI))
										&& !MethodsToReplicateToMyPlayer.Contains(method))
										continue;


									switch (method)
									{
										case "Damage":
											//PatchConstants.Logger.LogInfo("Damage");
											PlayerOnDamagePatch.DamageReplicated(player, dictionary);
											break;
										case "Dead":
											PatchConstants.Logger.LogInfo("Dead");
											break;
										case "Door":
											PatchConstants.Logger.LogInfo("Door");
											break;
										case "Move":
											PlayerOnMovePatch.MoveReplicated(player, dictionary);
											break;
										case "Rotate":
											//PatchConstants.Logger.LogInfo("Rotate");
											PlayerOnRotatePatch.RotateReplicated(player, dictionary);
											break;
										case "PlayerSpawn":
											PatchConstants.Logger.LogInfo("PlayerSpawn");
											break;



									}
								}
							}
						}
						catch (Exception ex)
						{
							PatchConstants.Logger.LogInfo($"{ex}");

						}
					}
				});
			}

			DateTime? LastParityCheck = DateTime.Now;

			void RunParityCheck()
            {
				Task.Run(() => { 
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
							dictionary.Add("count", LocalGamePatches.Players.Count);
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
								foreach (KeyValuePair<string, EFT.LocalPlayer> player in LocalGamePatches.Players)
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
					catch (Exception ex)
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
								List<Dictionary<string, object>> list3 = dictionary["players"].ToJson().ParseJsonTo<List<Dictionary<string, object>>>(Array.Empty<JsonConverter>());
								if (list3 != null)
								{
									//this.QuickLog("ParityCheck::Data Received from Central Server::PlayerListData::Count::" + list3.Count);
									//foreach (var item in list3)
									//{
									//	QuickLog(string.Join(", ", item.Keys));
									//}
									foreach (var item in list3)
									{
										if (item == null)
											continue;

										if (item.ContainsKey("accountId"))
										{
											string accountId = item["accountId"].ToString();
											if (LocalGamePatches.Players == null || LocalGamePatches.Players.Count == 0)
												continue;

                                            Vector3 newPosition = LocalGamePatches.Players.First().Value.Position;
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
					if (!this.PlayersToSpawn.TryDequeue(out var newPlayerToSpawn) || LocalGamePatches.Players.ContainsKey(newPlayerToSpawn.Item1.AccountId))
					{
						return;
					}
					if (!newPlayerToSpawn.Item3)
					{
						this.QuickLog("Update::Loading a new Player " + newPlayerToSpawn.Item1.AccountId);
						IEnumerable<ResourceKey> allPrefabPaths = newPlayerToSpawn.Item1.GetAllPrefabPaths();
						if (allPrefabPaths.Count() > 0)
						{
							SIT.A.Tarkov.Core.Plugin.LoadBundlesAndCreatePools(allPrefabPaths.ToArray()).ContinueWith((x) =>
							{
								this.PlayersToSpawn.Enqueue((newPlayerToSpawn.Item1, newPlayerToSpawn.Item2, true));
							});
							//Singleton<GClass1481>.Instance.LoadBundlesAndCreatePools(GClass1481.PoolsCategory.Raid, GClass1481.AssemblyType.Local, allPrefabPaths.ToArray(), GClass2536.General)
							//	.ContinueWith(delegate
							//	{
							//		this.PlayersToSpawn.Enqueue((newPlayerToSpawn.Item1, newPlayerToSpawn.Item2, true));
							//	});
						}
						return;
					}
					this.QuickLog("Update::Spawning a new >> Loaded << Player " + newPlayerToSpawn.Item1.AccountId);
					newPlayerToSpawn.Item1.SetSpawnedInSession(true);
					Vector3 vector = newPlayerToSpawn.Item2;
					LocalPlayer result = this.CreatePhysicalOtherPlayerOrBot(newPlayerToSpawn.Item1, vector).Result;
					if (result != null)
					{
						if (MatchmakerAcceptPatches.IsServer)
						{
							//this.gclass1220_0.AddActivePLayer(result);
						}

						if (LocalGamePatches.Players.TryAdd(newPlayerToSpawn.Item1.AccountId, result))
						{
							QuickLog($"Added new Player {newPlayerToSpawn.Item1.AccountId} to Players");
							if (MatchmakerAcceptPatches.IsServer)
							{
								//CoopImplementation.ClientPlayers.TryAdd(newPlayerToSpawn.Item1.AccountId, result);
							}
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
						this.PlayersToSpawn.Enqueue(newPlayerToSpawn);
					}
				}
				catch (Exception ex)
                {
					this.QuickLog(ex.ToString());
                }
			}


			void FixedUpdate()
			{
				RunParityCheck();
				RunQueuedActions();

				UpdateClientSpawnPlayers();
			}

			void Update()
			{
			
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
}
