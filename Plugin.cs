using BepInEx;
using CoopTarkovGameServer;
using SIT.Coop.Core.LocalGame;
using SIT.Coop.Core.Player;
using SIT.Coop.Core.Player.Weapon;
using SIT.Coop.Core.Player;
using System.Linq;
using UnityEngine.SceneManagement;

namespace SIT.Coop.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SIT.A.Tarkov.Core.PluginInfo.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static int UDPPort { get; private set; } = 7070;
        public static Plugin Instance { get; private set; }
        public static EchoGameServer EchoGameServer { get; private set; }
        private void Awake()
        {
            Instance = this;
            Matchmaker.MatchmakerAcceptPatches.Run();

            UDPPort = Config.Bind<int>("Server", "Port", 7070).Value;

            new LocalGameStartingPatch(Config).Enable();


            // ------ SPAWN --------------------------
            new LocalGamePlayerSpawn().Enable();

            // ------ PLAYER -------------------------
            new PlayerOnDamagePatch().Enable();
            new PlayerOnDeadPatch(Config).Enable();
            new PlayerOnDropBackpackPatch().Enable();
            new PlayerOnGesturePatch().Enable();
            new PlayerOnHealPatch().Enable();
            new PlayerOnInventoryOpenedPatch().Enable();
            new PlayerOnMovePatch().Enable();
            new PlayerOnSayPatch().Enable();
            new PlayerOnSetItemInHandsPatch().Enable();
            // testing
            new PlayerOnInteractWithDoorPatch().Enable();
            new PlayerOnProceedWeaponPatch().Enable();
            new PlayerOnSetInHandsWeaponPatch().Enable();

            // ------ WEAPON -------------------------
            new WeaponOnTriggerPressedPatch().Enable();
            new WeaponOnDropPatch().Enable();


            

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            //if (LocalGamePatches.LocalGameInstance == null)
            //    new LocalGameStartBotSystemPatch().Disable();
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            //if(LocalGamePatches.LocalGameInstance != null)
        }

        private void EchoGameServer_OnLog(string text)
        {
            Logger.LogInfo(text);
        }

        void FixedUpdate()
        {

        }

        void Update()
        {
            //while(LocalGamePatches.ClientQueuedActions.Any())
            //{
            //    Logger.LogInfo("LocalGamePatches.ClientQueuedActions has stuff!");
            //}
        }
    }
}
