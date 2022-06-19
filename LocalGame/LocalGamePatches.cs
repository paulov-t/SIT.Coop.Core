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
	public enum ESpawnState
	{
		NotLoaded = 0,
		Loading = 1,
		Loaded = 2,
		Spawning = 3,
		Spawned = 4,
	}

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

		public static Type StatisticsManagerType;
		private static object StatisticsManager;

		public static object GetStatisticsManager()
        {
			if(StatisticsManagerType == null || StatisticsManager == null)
            {
				StatisticsManagerType = PatchConstants.EftTypes.First(
					x =>
					PatchConstants.GetAllMethodsForType(x).Any(m => m.Name == "AddDoorExperience")
					&& PatchConstants.GetAllMethodsForType(x).Any(m => m.Name == "BeginStatisticsSession")
					&& PatchConstants.GetAllMethodsForType(x).Any(m => m.Name == "EndStatisticsSession")
					&& PatchConstants.GetAllMethodsForType(x).Any(m => m.Name == "OnEnemyDamage")
					&& PatchConstants.GetAllMethodsForType(x).Any(m => m.Name == "OnEnemyKill")
					);
				StatisticsManager = Activator.CreateInstance(StatisticsManagerType);
			}
			return StatisticsManager;	
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

	}
}
