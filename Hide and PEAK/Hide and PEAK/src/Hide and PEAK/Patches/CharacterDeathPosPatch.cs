using HarmonyLib;
using UnityEngine;

namespace Hide_and_PEAK.Patches;

public class CharacterDeathPosPatch
{
    [HarmonyPatch(typeof(Character), "DeathPos")]
    [HarmonyPrefix]
    static bool Prefix(Character __instance, ref Vector3 __result)
    {
        Plugin.Log.LogInfo($"RunManager: Death pos Patch!");

        __instance.view.Controller.CustomProperties.TryGetValue("Team", out var team);
        
        if ((Team)team == Team.Hider)
        {
            __result = __instance.Center + Vector3.up;
            Plugin.Log.LogInfo($"RunManager: Death pos Patch! Hider death position set to {__result}");
            return false;
        }

        return true;
    }
}