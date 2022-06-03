using Newtonsoft.Json;
using SIT.Tarkov.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.LocalGame
{
	public class LocalGamePatches
	{
		public static object LocalGameInstance;

		public static object MyPlayer;

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
			}

			private EFT.LocalPlayer GetPlayerByAccountId(string accountId)
			{
				if (MyPlayerProfile.AccountId == accountId)
					return MyPlayer as EFT.LocalPlayer;

				if (Players.ContainsKey(accountId))
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

			void FixedUpdate()
			{


			}

			void Update()
			{
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
							while (value.Any())
							{
								Dictionary<string, object> dictionary = value.Dequeue();
								if (value == null)
									continue;

								if (!dictionary.ContainsKey("accountId") || !dictionary.ContainsKey("m"))
									continue;

								string accountid = dictionary["accountId"].ToString();
								var player = GetPlayerByAccountId(accountid);
								if (player == null)
									continue;

								EBodyPart eBodyPart = EBodyPart.Common;
								if (dictionary.ContainsKey("bodyPart"))
									eBodyPart = (EBodyPart)Enum.Parse(typeof(EBodyPart), dictionary["bodyPart"].ToString());


								var method = dictionary["m"].ToString();
								if (accountid == MyPlayerProfile.AccountId && !MethodsToReplicateToMyPlayer.Contains(method))
									continue;


								switch (method)
								{
									case "Move":
										Vector2 zero = Vector2.zero;
										Vector2 zero2 = Vector2.zero;
										Quaternion item2 = Quaternion.identity;
										Vector3 item3 = Vector3.zero;
										if (dictionary.ContainsKey("dX") && dictionary.ContainsKey("dY"))
										{
											zero.x = float.Parse(dictionary["dX"].ToString());
											zero.y = float.Parse(dictionary["dY"].ToString());
										}
										if (dictionary.ContainsKey("nP"))
										{
											item3 = JsonConvert.DeserializeObject<Vector3>(JsonConvert.SerializeObject(dictionary["nP"]));
										}
										//player.MoveReplicated(zero, item3);
										//player.MovesReplicated(new List<Vector2>() { zero }, item3);
										break;
									case "Dead":
										PatchConstants.Logger.LogInfo("Dead");
										break;
									case "Damage":
										PatchConstants.Logger.LogInfo("Damage");
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
			
		}
	}
}
