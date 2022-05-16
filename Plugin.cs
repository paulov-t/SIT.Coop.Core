using BepInEx;

namespace SIT.Z.Coop.Core
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(SIT.A.Tarkov.Core.PluginInfo.PLUGIN_GUID)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {

            // Turning off until I fully got it removed from Assembly-CSharp
            SIT.Coop.Core.Matchmaker.MatchmakerAcceptPatches.Run();



            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
