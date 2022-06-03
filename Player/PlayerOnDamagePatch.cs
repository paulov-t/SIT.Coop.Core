using SIT.Tarkov.Core;
using SIT.Z.Coop.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

            //dictionary.Add("armorDamage", damageInfo.ArmorDamage);
            //dictionary.Add("bodyPart", bodyPartType);
            //dictionary.Add("bodyPartColliderType", damageInfo.BodyPartColliderType);
            //dictionary.Add("damage", damageInfo.Damage);
            //dictionary.Add("damageType", damageInfo.DamageType);
            //dictionary.Add("deflectedBy", damageInfo.DeflectedBy);
            //dictionary.Add("didArmorDamage", damageInfo.DidArmorDamage);
            //dictionary.Add("didBodyDamage", damageInfo.DidBodyDamage);
            //dictionary.Add("direction", damageInfo.Direction);
            //dictionary.Add("heavyBleedingDelta", damageInfo.HeavyBleedingDelta);
            ////dictionary.Add("hitCollider", damageInfo.HitCollider);
            //dictionary.Add("hitNormal", damageInfo.HitNormal);
            //dictionary.Add("hitPoint", damageInfo.HitPoint);
            //dictionary.Add("lightBleedingDelta", damageInfo.LightBleedingDelta);
            //dictionary.Add("masterOrigin", damageInfo.MasterOrigin);
            //if (damageInfo.OverDamageFrom.HasValue)
            //    dictionary.Add("overDamageFrom", damageInfo.OverDamageFrom);
            //dictionary.Add("penetrationPower", damageInfo.PenetrationPower);
            //if (damageInfo.Player != null && damageInfo.Player.Profile != null)
            //    dictionary.Add("playerId", damageInfo.Player.Profile.AccountId);
            //dictionary.Add("sourceId", damageInfo.SourceId);
            //if (damageInfo.Weapon != null)
            //    dictionary.Add("weapon", damageInfo.Weapon.Id);

            //dictionary.Add("absorbed", absorbed);
            //dictionary.Add("headSegment", headSegment);

            //dictionary.Add("m", "Damage");
            //ServerCommunication.PostLocalPlayerData(__instance, dictionary);
            //Logger.LogInfo("PlayerOnDamagePatch.PatchPostfix:Sent");

        }
    }
}
