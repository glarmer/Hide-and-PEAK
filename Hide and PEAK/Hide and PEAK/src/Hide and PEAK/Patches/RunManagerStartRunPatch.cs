using HarmonyLib;

namespace Hide_and_PEAK.Patches;

public class RunManagerStartRunPatch
{
    [HarmonyPatch(typeof(RunManager), "StartRun")]
    [HarmonyPostfix]
    static void Postfix(RunManager __instance)
    {
        Plugin.Log.LogInfo($"RunManager: StartRun Patch!");
        
        if (!Plugin.Instance._isInitialised && GameHandler.GetService<RichPresenceService>()?.m_currentState == RichPresenceState.Status_Shore)
        {
            Plugin.Log.LogInfo($"RunManager: Initialising mod...");
            Plugin.Instance.InitialiseMod();
        }
    }
}