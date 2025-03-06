using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using ProxyHeat;
using System;
using System.Reflection;

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
        public static void Postfix(IntVec3 cell, ref float __result, ProxyHeatManager __instance)
        {
            if (__instance.temperatureSources.TryGetValue(cell, out List<CompTemperatureSource> tempSources))
            {
                var tempResults = new List<float>();
                foreach (var tempSourceCandidate in tempSources)
                {
                    var tempResult = __result;
                    var props = tempSourceCandidate.Props;
                    var tempOutcome = tempSourceCandidate.TemperatureOutcome;
                    if (tempOutcome != 0)
                    {
                        if (!(props.maxTemperature.HasValue && __result >= props.maxTemperature.Value && tempOutcome > 0) &&
                            !(props.minTemperature.HasValue && props.minTemperature.Value >= __result && tempOutcome < 0))
                        {
                            tempResult += tempOutcome;

                            if (props.maxTemperature.HasValue)
                            {
                                tempResult = Math.Min(tempResult, props.maxTemperature.Value);
                            }

                            if (props.minTemperature.HasValue)
                            {
                                tempResult = Math.Max(tempResult, props.minTemperature.Value);
                            }
                        }
                    }
                    tempResults.Add(tempResult);
                }
                if (tempResults.Count > 1)
                {
                    __result = tempResults.Average();
                }
                else if (tempResults.Count == 1)
                {
                    __result = tempResults[0];
                }
            }
        }
    }

    [HarmonyPatch(typeof(CompTemperatureSource), "RecalculateAffectedCells")]
    public static class CompTemperatureSource_RecalculateAffectedCells_Patch
    {
        private static readonly FieldInfo affectedCellsField = typeof(CompTemperatureSource).GetField("affectedCells", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo affectedCellsListField = typeof(CompTemperatureSource).GetField("affectedCellsList", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo mapField = typeof(CompTemperatureSource).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Postfix(CompTemperatureSource __instance)
        {
            HashSet<IntVec3> affectedCells = (HashSet<IntVec3>)affectedCellsField.GetValue(__instance);
            List<IntVec3> affectedCellsList = (List<IntVec3>)affectedCellsListField.GetValue(__instance);
            Map map = (Map)mapField.GetValue(__instance);

            affectedCells.RemoveWhere(cell => cell.GetEdifice(map) != null);
            affectedCellsList.RemoveAll(cell => cell.GetEdifice(map) != null);

            affectedCellsList.Clear();
            affectedCellsList.AddRange(affectedCells);
        }
    }

}
