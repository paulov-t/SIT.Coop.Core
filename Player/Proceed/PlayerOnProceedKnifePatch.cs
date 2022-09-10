using Comfort.Common;
using EFT.InventoryLogic;
using SIT.Coop.Core.Web;
using SIT.Tarkov.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SIT.Coop.Core.Player
{
    internal class PlayerOnProceedKnifePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var t = typeof(EFT.Player);
            if (t == null)
                Logger.LogInfo($"PlayerOnProceedKnifePatch:Type is NULL");

            var method = PatchConstants.GetAllMethodsForType(t)
                .FirstOrDefault(x => x.Name == "Proceed"
                && x.GetParameters()[0].Name == "knife"
                );

            Logger.LogInfo($"PlayerOnProceedKnifePatch:{t.Name}:{method.Name}");
            return method;
        }

        [PatchPrefix]
        public static bool PrePatch()
        {
            return Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer;
        }

        [PatchPostfix]
        public static void Patch(EFT.Player __instance, KnifeComponent knife)
        {
            if (Matchmaker.MatchmakerAcceptPatches.IsSinglePlayer)
                return;

            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("m", "Proceed");
            args.Add("item.id", knife.Item.Id);
            args.Add("item.tpl", knife.Item.TemplateId);
            args.Add("template", knife.Template.SITToJson());
            args.Add("pType", "Knife");

            ServerCommunication.PostLocalPlayerData(__instance, args);

        }

        public static void ProceedWeaponReplicated(EFT.Player player, Dictionary<string, object> packet)
        {
            if (player == null)
                return;

            var item = player.Profile.Inventory.GetAllItemByTemplate(packet["item.tpl"].ToString()).FirstOrDefault();
            if (item != null)
            {
                var kc = new KnifeComponent(item, new KnifeTemplate());
                PatchConstants.Logger.LogInfo($"PlayerOnProceedKnifePatch.ProceedWeaponReplicated: Attempting to set item of tpl {packet["item.tpl"].ToString()}");
                player.Proceed(knife: kc, callback: (Result<IKnifeHandsController> r) => { }, false);
            }
        }

        public class KnifeTemplate : IKnifeTemplate
        {
            public float KnifeHitDelay { get; set; } = 1;

            public float KnifeHitSlashRate { get; set; } = 1;

            public float KnifeHitStabRate { get; set; } = 1;

            public float KnifeHitRadius { get; set; } = 1;

            public int KnifeHitSlashDam { get; set; } = 1;

            public int KnifeHitStabDam { get; set; } = 1;

            public float PrimaryDistance { get; set; } = 1;

            public float SecondaryDistance { get; set; } = 1;

            public int StabPenetration { get; set; } = 1;

            public int SlashPenetration { get; set; } = 1;

            public float PrimaryConsumption { get; set; } = 1;

            public float SecondaryConsumption { get; set; } = 1;

            public float DeflectionConsumption { get; set; } = 1;

            public Vector2 AppliedTrunkRotation { get; set;}

            public Vector2 AppliedHeadRotation { get; set;}

            public bool DisplayOnModel { get; set;}

            public int AdditionalAnimationLayer { get; set;}

            public float StaminaBurnRate { get; set;}

            public Vector3 ColliderScaleMultiplier { get; set;}
        }
    }
}
