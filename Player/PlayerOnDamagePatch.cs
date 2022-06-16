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

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = SIT.Tarkov.Core.PatchConstants.EftTypes.FirstOrDefault(x => x.FullName == "EFT.Player");
            if (t == null)
                Logger.LogInfo($"PlayerOnDamagePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "ApplyDamageInfo");

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
            , object damageInfo
            , object bodyPartType
            , float absorbed
            , object headSegment)
        {
            //Logger.LogInfo("PlayerOnDamagePatch.PatchPostfix");
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            //DamageInfo damageI = JsonConvert.DeserializeObject<DamageInfo>(JsonConvert.SerializeObject(damageInfo, settings: new JsonSerializerSettings() { MaxDepth = 0, ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
            dictionary.Add("armorDamage", PatchConstants.GetFieldFromType(damageInfo.GetType(), "ArmorDamage").GetValue(damageInfo));
            dictionary.Add("bodyPart", bodyPartType);
            //dictionary.Add("bodyPartColliderType", damageI.BodyPartColliderType);
            dictionary.Add("damage", PatchConstants.GetFieldFromType(damageInfo.GetType(), "Damage").GetValue(damageInfo));
            dictionary.Add("damageType", PatchConstants.GetFieldFromType(damageInfo.GetType(), "DamageType").GetValue(damageInfo));
            //dictionary.Add("damageType", damageI.DamageType);
            //dictionary.Add("deflectedBy", damageI.DeflectedBy);
            //dictionary.Add("didArmorDamage", damageI.DidArmorDamage);
            //dictionary.Add("didBodyDamage", damageI.DidBodyDamage);
            //dictionary.Add("direction", damageI.Direction);
            //dictionary.Add("heavyBleedingDelta", damageI.HeavyBleedingDelta);
            ////dictionary.Add("hitCollider", damageInfo.HitCollider);
            //dictionary.Add("hitNormal", damageI.HitNormal);
            //dictionary.Add("hitPoint", damageI.HitPoint);
            //dictionary.Add("lightBleedingDelta", damageI.LightBleedingDelta);
            //dictionary.Add("masterOrigin", damageI.MasterOrigin);
            //if (damageI.OverDamageFrom.HasValue)
            //    dictionary.Add("overDamageFrom", damageI.OverDamageFrom);
            //dictionary.Add("penetrationPower", damageI.PenetrationPower);
            ////if (damageI.Player != null && damageI.Player.Profile != null)
            ////    dictionary.Add("playerId", damageI.Player.Profile.AccountId);
            ////dictionary.Add("sourceId", damageI.SourceId);
            ////if (damageI.Weapon != null)
            ////    dictionary.Add("weapon", damageI.Weapon.Id);

            //dictionary.Add("absorbed", absorbed);
            //dictionary.Add("headSegment", headSegment);

            dictionary.Add("m", "Damage");
            ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //Logger.LogInfo("PlayerOnDamagePatch.PatchPostfix:Sent");

        }

        public static void DamageReplicated(EFT.Player player, Dictionary<string, object> dict)
        {
            if(player == null)
            {
                Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - ERROR, no player instance");
                return;
            }
            object ActiveHealthController = player.ActiveHealthController;
            if (ActiveHealthController == null)
            {
                Logger.LogInfo("PlayerOnDamagePatch.DamageReplicated() - ERROR, no ActiveHealthController instance");
                return;
            }

            bool isAlive = PatchConstants.GetFieldOrPropertyFromInstance<bool>(ActiveHealthController, "IsAlive", false);
            //DamageInfo damageInfo = new DamageInfo();
            var dmI = HealthControllerHelpers.CreateDamageInfoTypeFromDict(dict);
            var damage = PatchConstants.GetFieldOrPropertyFromInstance<float>(dmI, "Damage");
            //damageInfo.Damage = float.Parse(dict["damage"].ToString());
            Enum.TryParse<EBodyPart>(dict["bodyPart"].ToString(), out EBodyPart bodyPart);

            bool autoKillThisCunt = (dict.ContainsKey("killThisCunt") ? bool.Parse(dict["killThisCunt"].ToString()) : false);

            //EDamageType damageType = damageInfo.DamageType;
            EDamageType damageType = PatchConstants.GetFieldOrPropertyFromInstance<EDamageType>(dmI, "DamageType");
            if (Matchmaker.MatchmakerAcceptPatches.IsClient && (damageType == EDamageType.Undefined || damageType == EDamageType.Fall))
                return;

            if (ActiveHealthController == null)
            {
                Logger.LogInfo($"ClientApplyDamageInfo::Attempting to Apply Damage a person with no Health Controller");
                return;
            }

            if (!isAlive)
            {
                Logger.LogInfo($"ClientApplyDamageInfo::Attempting to Apply Damage to a Dead Guy");
                return;
            }

            if (autoKillThisCunt)
            {
                //ActiveHealthController.Kill(damageType);
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
                    //ActiveHealthController.ChangeHealth(bodyPartType, -damageInfo.Damage, damageInfo);

                    //if (Singleton<GClass558>.Instantiated)
                    //{
                    //    Singleton<GClass558>.Instance.BeingHitAction(damageInfo, this);
                    //}
                    //ActiveHealthController.TryApplySideEffects(damageInfo, bodyPartType, out var sideEffectComponent);
                }
            }
            catch 
            {
            }

            //// get the health again
            currentBodyPartHealth = HealthControllerHelpers.GetBodyPartHealth(ActiveHealthController, bodyPart).Current;
            //currentBodyPartHealth = ActiveHealthController.GetBodyPartHealth(bodyPartType).Current;
            Logger.LogInfo($"ClientApplyDamageInfo::{bodyPart} current health [after] = {currentBodyPartHealth}");
            //UnityEngine.Debug.LogError($"ClientApplyDamageInfo::{bodyPartType} current health [after] = {currentBodyPartHealth}");

            //if (currentBodyPartHealth == 0)
            //{
            //    if (!damageType.IsBleeding() && (bodyPartType == EBodyPart.Head || bodyPartType == EBodyPart.Chest))
            //    {
            //        UnityEngine.Debug.LogError($"ClientApplyDamageInfo::No BodyPart Health on Head/Chest, killing");

            //        ActiveHealthController.Kill(damageType);
            //    }
            //}

            var currentOVRHealth = HealthControllerHelpers.GetBodyPartHealth(ActiveHealthController, EBodyPart.Common).Current;
            if (currentOVRHealth == 0)
            {
                UnityEngine.Debug.LogError($"ClientApplyDamageInfo::Common Health, killing");

                PatchConstants.GetAllMethodsForObject(ActiveHealthController).Single(x => x.Name == "Kill").Invoke(ActiveHealthController, new object[] { damageType });
            }

            if (!isAlive)
                return;

            //ActiveHealthController.DoWoundRelapse(damageInfo.Damage, bodyPartType);
        }
    }
}
