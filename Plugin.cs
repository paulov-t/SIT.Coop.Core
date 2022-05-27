using BepInEx;
using SIT.Coop.Core.LocalGame;
using SIT.Coop.Core.Player;
using SIT.Z.Coop.Core.Player;

namespace SIT.Coop.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SIT.A.Tarkov.Core.PluginInfo.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
            // Turning off until I fully got it removed from Assembly-CSharp
            SIT.Coop.Core.Matchmaker.MatchmakerAcceptPatches.Run();

            new LocalGameStartingPatch().Enable();
            new LocalGamePlayerSpawn().Enable();
            //new PlayerOnInteractWithDoorPatch().Enable();
            new PlayerOnGesturePatch().Enable();
            new PlayerOnSayPatch().Enable();
            new PlayerOnDropBackpackPatch().Enable();
            new PlayerOnInventoryOpenedPatch().Enable();
            new PlayerOnDamagePatch().Enable();
            new PlayerOnMovePatch().Enable();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {

        }
    }
}
