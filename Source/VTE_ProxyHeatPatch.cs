using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using ProxyHeat;

namespace VTE_ProxyHeatPatch
{
    [StaticConstructorOnStartup]
    public static class VTE_ProxyHeatPatch
    {
        static VTE_ProxyHeatPatch()
        {
            Harmony harmony = new Harmony("ewolfy.vtePatch");
            harmony.PatchAll();

        }
    }

    [HarmonyPatch(typeof(ProxyHeatManager), "GetTemperatureOutcomeFor")]
    public static class ProxyHeatManager_GetTemperatureOutcomeFor_Patch
    {
        public static bool Prefix(IntVec3 cell, float result, ref float __result, Map ___map, ProxyHeatManager __instance)
        {
            if (cell.Impassable(___map))
            {
                __result = result;
                return false;
            }

            if (__instance.temperatureSources.TryGetValue(cell, out List<CompTemperatureSource> tempSources))
            {
                if (!cell.UsesOutdoorTemperature(___map))
                {
                    foreach (var comp in tempSources)
                    {
                        __instance.MarkDirty(comp);
                    }
                    __result = result;
                    return false;
                }
                
                var tempResults = new List<float>();
                foreach (var tempSourceCandidate in tempSources)
                {
                    var tempResult = result;
                    var props = tempSourceCandidate.Props;
                    var tempOutcome = tempSourceCandidate.TemperatureOutcome;
                    if (tempOutcome != 0)
                    {
                        if (!(props.maxTemperature.HasValue && result >= props.maxTemperature.Value && tempOutcome > 0) &&
                            !(props.minTemperature.HasValue && props.minTemperature.Value >= result && tempOutcome < 0))
                        {
                            tempResult += tempOutcome;

                            if (props.maxTemperature.HasValue)
                            {
                                tempResult = System.Math.Min(tempResult, props.maxTemperature.Value);
                            }

                            if (props.minTemperature.HasValue)
                            {
                                tempResult = System.Math.Max(tempResult, props.minTemperature.Value);
                            }
                        }
                    }
                    tempResults.Add(tempResult);
                }
                if (tempResults.Count > 1)
                {
                    result = tempResults.Average();
                }
                else if (tempResults.Count == 1)
                {
                    result = tempResults[0];
                }
                __result = result;
            }
            return false;
        }
    }

}
