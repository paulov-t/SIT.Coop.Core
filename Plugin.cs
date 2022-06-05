using BepInEx;
using CoopTarkovGameServer;
using SIT.Coop.Core.LocalGame;
using SIT.Coop.Core.Player;
using SIT.Coop.Core.Player.Weapon;
using SIT.Z.Coop.Core.Player;
using System.Linq;
using UnityEngine.SceneManagement;

namespace SIT.Coop.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SIT.A.Tarkov.Core.PluginInfo.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static EchoGameServer EchoGameServer { get; private set; }
        private void Awake()
        {
            Instance = this;
            Matchmaker.MatchmakerAcceptPatches.Run();

            new LocalGameStartingPatch().Enable();

            // .. This can break loot containers, be careful ..
            //new PlayerOnInteractWithDoorPatch().Enable();

            // ------ SPAWN --------------------------
            new LocalGamePlayerSpawn().Enable();

            // ------ PLAYER -------------------------
            new PlayerOnDamagePatch().Enable();
            new PlayerOnDropBackpackPatch().Enable();
            new PlayerOnGesturePatch().Enable();
            new PlayerOnHealPatch().Enable();
            new PlayerOnInventoryOpenedPatch().Enable();
            new PlayerOnMovePatch().Enable();
            new PlayerOnRotatePatch().Enable();
            new PlayerOnSayPatch().Enable();
            new PlayerOnSetItemInHandsPatch().Enable();

            // ------ WEAPON -------------------------
            new WeaponOnTriggerPressedPatch().Enable();
            new WeaponOnDropPatch().Enable();

            // ------ HIDDEN LISTEN SERVER -----------
            Logger.LogInfo("Starting Echo Server");
            EchoGameServer = new EchoGameServer();
            EchoGameServer.OnLog += EchoGameServer_OnLog;
            EchoGameServer.CreateListenersAndStart();
            Logger.LogInfo("Echo Server started");

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
