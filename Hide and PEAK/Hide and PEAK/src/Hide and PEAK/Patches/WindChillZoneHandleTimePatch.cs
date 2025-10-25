using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Hide_and_PEAK.Patches;

public class WindChillZoneHandleTimePatch
{
    [HarmonyPatch(typeof(WindChillZone), "HandleTime")]
    [HarmonyPrefix]
    private static bool Prefix(WindChillZone __instance)
    {
        if (!Plugin.ConfigurationHandler.ConfigStormsEnabled.Value && !__instance.windActive)
        {
            __instance.untilSwitch -= Time.deltaTime;
            if (__instance.untilSwitch >= 0.0 || !PhotonNetwork.IsMasterClient)
                return false;
            double nextWindTime = __instance.GetNextWindTime(__instance.windActive);
            __instance.untilSwitch = __instance.GetNextWindTime(true);
            Plugin.Log.LogInfo($"WindChillZone: Storms disabled, skipping... Next Wind Time: {nextWindTime}");
            return false;
        }
        return true;
    }
}