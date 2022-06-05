using EFT;
using Newtonsoft.Json;
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

		public static Dictionary<string, object> Players = new Dictionary<string, object>();

		public static Dictionary<string, object> Bots = new Dictionary<string, object>();

		public static ConcurrentDictionary<string, Queue<Dictionary<string, object>>> ClientQueuedActions = new ConcurrentDictionary<string, Queue<Dictionary<string, object>>>();

		public class CoopGameComponent : MonoBehaviour
		{

			List<string> MethodsToReplicateToMyPlayer = new List<string>()
			{
				"Dead",
				"Damage",
			};

			void Awake()
			{
				PatchConstants.Logger.LogInfo("CoopGameComponent:Awake");
				Players.Clear();
				Bots.Clear();
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
					PatchConstants.Logger.LogInfo($"Unable to find Profile of {accountId}");
					return null;
				}

				PatchConstants.Logger.LogInfo($"Adding Profile of {accountId} to Players list");
				Players.Add(accountId, player);
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
				//if (!base.Status.IsRunned())
				//{
				//	return null;
				//}
				int playerId = int.Parse(InvokeLocalGameInstanceMethod("method_13").ToString());
				profile.SetSpawnedInSession(false);
				return await LocalPlayer.Create(playerId
					, position
					, Quaternion.identity
					, "Player"
					, ""
					, EPointOfView.ThirdPerson
					, profile
					, false
					, EUpdateQueue.FixedUpdate //PatchConstants.GetAllPropertiesForObject(LocalGameInstance).FirstOrDefault(x=>x.Name == "UpdateQueue").GetValue(LocalGameInstance)
					, 0
					, 0
					, GClass523.Config.CharacterController.BotPlayerMode
					, () => 0.7f
					, () => 0.7f
					, 0
					, new GClass1481()
					, new GClass1205()
					, null
					, false);

				//return localPlayer;
			}




			void RunQueuedActions()
            {
				if (ClientQueuedActions == null)
					return;

				if (ClientQueuedActions.Any())
				{
					try
					{
						//foreach (KeyValuePair<string, Queue<Dictionary<string, object>>> clientQueuedAction in this.ClientQueuedMoveActions)
						//UpdateClientQueuedActions_Move();

						foreach (KeyValuePair<string, Queue<Dictionary<string, object>>> clientQueuedAction in ClientQueuedActions)
						{
							string key = clientQueuedAction.Key;
							Queue<Dictionary<string, object>> value = clientQueuedAction.Value;
							if (value == null || value.Count == 0)
							{
								continue;
							}
							//while (value.Any())
							if (value.Any())
							{
								Dictionary<string, object> dictionary = value.Dequeue();
								if (value == null)
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



								}
							}
						}
					}
					catch (Exception ex)
					{
						PatchConstants.Logger.LogInfo($"{ex}");

					}
				}
			}

			void RunParityCheck()
            {

            }


			void FixedUpdate()
			{


			}

			void Update()
			{
				RunParityCheck();
				RunQueuedActions();
				
			}

			void QuickLog(string log)
            {
				PatchConstants.Logger.LogInfo(log);
            }
			
		}
	}
}
