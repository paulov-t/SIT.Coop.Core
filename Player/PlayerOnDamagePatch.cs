using EFT;
using Newtonsoft.Json;
using SIT.Coop.Core.HelpfulStructs;
using SIT.Tarkov.Core;
using SIT.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SIT.Tarkov.Core.PlayerPatches.Health;
using EFT.InventoryLogic;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Networking;
using Comfort.Common;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnDamagePatch : ModulePatch
    {
        private static Type BeingHitType;

        public PlayerOnDamagePatch()
        {
            BeingHitType = PatchConstants.EftTypes.Single(x => PatchConstants.GetMethodForType(x, "BeingHitAction") != null);
        }

        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnDamagePatch:Type is NULL");

            var method = PatchConstants.GetMethodForType(t, "ApplyDamageInfo");

            Logger.LogInfo($"PlayerOnDamagePatch:{t.Name}:{method.Name}");
            return method;
        }

        //[PatchPrefix]
        //public static void PatchPrefix(object gesture)
        //{
        //    Logger.LogInfo("OnGesturePatch.PatchPrefix");
        //}

        [PatchPostfix]
        public static void PatchPostfix(
            EFT.Player __instance
            , DamageInfo damageInfo
            , EBodyPart bodyPartType
            , float absorbed
            , object headSegment)
        {
            //Logger.LogInfo("PlayerOnDamagePatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //DamageInfo damageI = JsonConvert.DeserializeObject<DamageInfo>(JsonConvert.SerializeObject(damageInfo, settings: new JsonSerializerSettings() { MaxDepth = 0, ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
            //dictionary.Add("armorDamage", PatchConstants.GetFieldFromType(damageInfo.GetType(), "ArmorDamage").GetValue(damageInfo));
            dictionary.Add("armorDamage", damageInfo.ArmorDamage);
            dictionary.Add("bodyPart", bodyPartType);
            ////dictionary.Add("bodyPartColliderType", damageI.BodyPartColliderType);
            ////dictionary.Add("damage", PatchConstants.GetFieldFromType(damageInfo.GetType(), "Damage").GetValue(damageInfo));
            dictionary.Add("damage", damageInfo.Damage);
            ////dictionary.Add("damageType", PatchConstants.GetFieldFromType(damageInfo.GetType(), "DamageType").GetValue(damageInfo));
            dictionary.Add("damageType", damageInfo.DamageType);
            ////dictionary.Add("damageType", damageI.DamageType);
            //dictionary.Add("deflectedBy", damageInfo.DeflectedBy);
            dictionary.Add("didArmorDamage", damageInfo.DidArmorDamage);
            dictionary.Add("didBodyDamage", damageInfo.DidBodyDamage);
            //dictionary.Add("direction", damageInfo.Direction);
            //dictionary.Add("hitNormal", damageInfo.HitNormal);
            //dictionary.Add("hitPoint", damageInfo.HitPoint);
            //dictionary.Add("lightBleedingDelta", damageInfo.LightBleedingDelta);
            //dictionary.Add("masterOrigin", damageInfo.MasterOrigin);
            ////dictionary.Add("didArmorDamage", damageI.DidArmorDamage);
            ////dictionary.Add("didBodyDamage", damageI.DidBodyDamage);
            ////dictionary.Add("direction", damageI.Direction);
            ////dictionary.Add("heavyBleedingDelta", damageI.HeavyBleedingDelta);
            //////dictionary.Add("hitCollider", damageInfo.HitCollider);
            ////dictionary.Add("hitNormal", damageI.HitNormal);
            ////dictionary.Add("hitPoint", damageI.HitPoint);
            ////dictionary.Add("lightBleedingDelta", damageI.LightBleedingDelta);
            ////dictionary.Add("masterOrigin", damageI.MasterOrigin);
            //if (damageInfo.OverDamageFrom.HasValue)
            //    dictionary.Add("overDamageFrom", damageInfo.OverDamageFrom);
            //dictionary.Add("penetrationPower", damageI.PenetrationPower);
            ////if (damageI.Player != null && damageI.Player.Profile != null)
            ////    dictionary.Add("playerId", damageI.Player.Profile.AccountId);
            ////dictionary.Add("sourceId", damageI.SourceId);
            ////if (damageI.Weapon != null)
            ////    dictionary.Add("weapon", damageI.Weapon.Id);

            //dictionary.Add("absorbed", absorbed);
            //dictionary.Add("headSegment", headSegment);
            //foreach(var r in PatchConstants.GetAllPropertiesForObject(damageInfo))
            //{
            //    dictionary.Add(r.Name, r.GetValue(damageInfo));
            //}
            //foreach (var r in PatchConstants.GetAllFieldsForObject(damageInfo))
            //{
            //    dictionary.Add(r.Name, r.GetValue(damageInfo));
            //}
            dictionary.Add("m", "Damage");
            var generatedDict = ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            if(generatedDict != null && generatedDict.ContainsKey("t"))
            {
                if (!ProcessedDamages.Contains(generatedDict["t"]))
                    ProcessedDamages.Add(generatedDict["t"].ToString());
            }

            //Logger.LogInfo("PlayerOnDamagePatch.PatchPostfix:Sent");

        }

        private static ConcurrentBag<string> ProcessedDamages = new ConcurrentBag<string>();

        public static void DamageReplicated(EFT.Player player, Dictionary<string, object> dict)
        {

            if(player == null)
            {
                Logger.LogError("PlayerOnDamagePatch.DamageReplicated() - ERROR, no player instance");
                return;
            }
            GHealthController ActiveHealthController = player.ActiveHealthController;
            if (ActiveHealthController == null)
            {
                Logger.LogError("PlayerOnDamagePatch.DamageReplicated() - ERROR, no ActiveHealthController instance");
                return;
            }

            //Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Get IsAlive ");

            bool isAlive = PatchConstants.GetFieldOrPropertyFromInstance<bool>(ActiveHealthController, "IsAlive", false);
            //DamageInfo damageInfo = new DamageInfo();
            //Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Get DamageInfo ");

            //DamageInfo dmI = Json.Deserialize<DamageInfo>(dict["data"].ToString()); // HealthControllerHelpers.CreateDamageInfoTypeFromDict(dict);
            DamageInfo dmI = new DamageInfo();
            if (!dict.ContainsKey("damage"))
                throw new ArgumentNullException("Damage", $"Damage has not been provided!");
            dmI.Damage = float.Parse(dict["damage"].ToString());
            if (dmI.Damage <= 0)
                throw new ArgumentOutOfRangeException("Damage", $"Damage needs to be over 0! Value provided: {dmI.Damage}");
            //foreach (var r in PatchConstants.GetAllPropertiesForObject(dmI))
            //{
            //    if (dict.ContainsKey(r.Name))
            //    {
            //        Logger.LogInfo($"PlayerOnDamagePatch.DamageReplicated() - Set DamageInfo.{r.Name}={dict[r.Name]}");
            //        r.SetValue(dmI, dict[r.Name]);
            //    }
            //}
            //foreach (var r in PatchConstants.GetAllFieldsForObject(dmI))
            //{
            //    if (dict.ContainsKey(r.Name))
            //    {
            //        Logger.LogInfo($"PlayerOnDamagePatch.DamageReplicated() - Set DamageInfo.{r.Name}={dict[r.Name]}");
            //        r.SetValue(dmI, dict[r.Name]);
            //    }
            //}

            //Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Get DamageInfo Damage ");

            var damage = dmI.Damage;
            //damageInfo.Damage = float.Parse(dict["damage"].ToString());

            //Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Get bodyPart ");

            Enum.TryParse<EBodyPart>(dict["bodyPart"].ToString(), out EBodyPart bodyPart);

            bool autoKillThisCunt = (dict.ContainsKey("killThisCunt") ? bool.Parse(dict["killThisCunt"].ToString()) : false);


            Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Check processed damages ");

            if (dict.ContainsKey("t"))
            {
                if (!ProcessedDamages.Contains(dict["t"]))
                    ProcessedDamages.Add(dict["t"].ToString());
                else
                {
                    Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Ignoring already processed damage ");
                    return;
                }
            }

            //EDamageType damageType = damageInfo.DamageType;
            EDamageType damageType = dmI.DamageType;
            if (Matchmaker.MatchmakerAcceptPatches.IsClient && (damageType == EDamageType.Undefined || damageType == EDamageType.Fall))
            {
                Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - Ignoring undefined or fall damage ");

                return;
            }

            if (ActiveHealthController == null)
            {
                Logger.LogInfo($"PlayerOnDamagePatch.DamageReplicated() - Attempting to Apply Damage a person with no Health Controller");
                return;
            }

            if (!isAlive)
            {
                Logger.LogInfo($"PlayerOnDamagePatch.DamageReplicated() - Attempting to Apply Damage to a Dead Guy");
                return;
            }

            if (autoKillThisCunt)
            {
                Kill(ActiveHealthController, damageType);
                return;
            }

            //if (ClientHandledDamages.ContainsKey(timeStamp))
            //    return;
            //ClientHandledDamages.Add(timeStamp, damageInfo);

            float currentBodyPartHealth = HealthControllerHelpers.GetBodyPartHealth(ActiveHealthController, bodyPart).Current;

            Logger.LogInfo($"ClientApplyDamageInfo::Damage = {damage}");
            Logger.LogInfo($"ClientApplyDamageInfo::{bodyPart} current health [before] = {currentBodyPartHealth}");

            try
            {
                if (damage > 0f)
                {
                    HealthControllerHelpers.ChangeHealth(ActiveHealthController, bodyPart, -damage, dmI);

                    //if (Singleton<PlayerGameAction>.Instantiated)
                    //{
                    //    //Singleton<PlayerGameAction>.Instance.BeingHitAction(dmI, player);
                    //}
                    //if (Singleton<GClass558>.Instantiated)
                    //{
                    //    Singleton<GClass558>.Instance.BeingHitAction(damageInfo, this);
                    //}
                    //ActiveHealthController.TryApplySideEffects(dmI, bodyPart, out var sideEffectComponent);
                    if (ActiveHealthController is PlayerHealthController)
                    {
                        Logger.LogInfo("Attempting to Kill!");
                        ((PlayerHealthController)ActiveHealthController).TryApplySideEffects(dmI, bodyPart, out _);
                    }
                }
            }
            catch 
            {
            }

            //// get the health again
            currentBodyPartHealth = HealthControllerHelpers.GetBodyPartHealth(ActiveHealthController, bodyPart).Current;
            //currentBodyPartHealth = ActiveHealthController.GetBodyPartHealth(bodyPartType).Current;
            //Logger.LogInfo($"ClientApplyDamageInfo::{bodyPart} current health [after] = {currentBodyPartHealth}");
            //UnityEngine.Debug.LogError($"ClientApplyDamageInfo::{bodyPartType} current health [after] = {currentBodyPartHealth}");

            if (currentBodyPartHealth == 0)
            {
                if (!damageType.IsBleeding() && (bodyPart == EBodyPart.Head || bodyPart == EBodyPart.Chest))
                {
                    UnityEngine.Debug.LogError($"ClientApplyDamageInfo::No BodyPart Health on Head/Chest, killing");

                    Kill(ActiveHealthController, damageType);
                }
            }

            var currentOVRHealth = HealthControllerHelpers.GetBodyPartHealth(ActiveHealthController, EBodyPart.Common).Current;
            if (currentOVRHealth == 0)
            {
                UnityEngine.Debug.LogError($"ClientApplyDamageInfo::Common Health, killing");

                Kill(ActiveHealthController, damageType);
            }

            if (!isAlive)
                return;

            //ActiveHealthController.DoWoundRelapse(damage, bodyPart);
        }

        private static void Kill(GHealthController activeHealthController, EDamageType damageType)
        {
            if (activeHealthController == null)
                return;

            
            if(activeHealthController is PlayerHealthController)
            {
                Logger.LogInfo("Attempting to Kill!");
                ((PlayerHealthController)activeHealthController).Kill(damageType);
            }

            //activeHealthController.Kill(damageType);
            //PatchConstants.GetMethodForType(activeHealthController.GetType(), "Kill").Invoke(activeHealthController, new object[] { damageType });
        }

        private static void BeingHitAction(object damageInfo, EFT.Player player)
        {
            var singletonBeingHit = PatchConstants.GetSingletonInstance(BeingHitType);
            if (singletonBeingHit == null)
                return;

            PatchConstants.GetMethodForType(singletonBeingHit.GetType(), "BeingHitAction")
                .Invoke(singletonBeingHit, new object[]
                {
                    damageInfo
                    , player
                });

        }

        private static void TryApplySideEffects(object activeHealthController, EDamageType damageType, EBodyPart bodyPart, out SideEffectComponent sideEffectComponent)
        {
            sideEffectComponent = null;
            if (activeHealthController == null)
                return;

            PatchConstants.GetMethodForType(activeHealthController.GetType(), "TryApplySideEffects")
                .Invoke(activeHealthController, new object[] 
                { 
                    damageType
                    , bodyPart
                    , sideEffectComponent
                });

        }
    }
}
