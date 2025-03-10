using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection.Emit;

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

    [HarmonyPatch(typeof(ProxyHeat.ProxyHeatManager), "GetTemperatureOutcomeFor", new Type[] { typeof(IntVec3), typeof(float) })]
    public static class ProxyHeatManager_GetTemperatureOutcomeFor_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            Label? targetLabel = null;
            int removeStartIndex = -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brfalse_S && codes[i].operand is Label label)
                {
                    targetLabel = label;
                }

                if (targetLabel.HasValue)
                {
                    for (int j = 0; j < codes.Count; j++)
                    {
                        if (codes[j].labels.Contains(targetLabel.Value))
                        {
                            removeStartIndex = j;
                            break;
                        }
                    }
                }

                if (targetLabel.HasValue && removeStartIndex != -1)
                {
                    if (codes[removeStartIndex].opcode == OpCodes.Newobj)
                    {
                        for (int k = 0; k < codes.Count; k++)
                        {
                            if (codes[k].opcode == OpCodes.Brfalse_S && codes[k].operand is Label checkLabel && checkLabel.GetHashCode() == targetLabel.GetHashCode())
                            {
                                codes[k].opcode = OpCodes.Nop;
                                codes[k].operand = null;

                                if (k + 1 + 4 <= codes.Count)
                                {
                                    codes.RemoveRange(k + 1, 4);
                                }
                                break;
                            }
                        }
                    }
                    targetLabel = null;
                    removeStartIndex = -1;

                }
            }
            return codes.AsEnumerable();
        }
    }

        public class ProxyHeatManager : MapComponent
    {
        public ProxyHeatManager(Map map) : base(map) { }

        public void MarkDirty(CompTemperatureSource source) { }

        public float GetTemperatureOutcomeFor(IntVec3 cell, float result) { return 0; }
    }

    public class CompTemperatureSource : ThingComp
    {
        public CompProperties_TemperatureSource Props => (CompProperties_TemperatureSource)props;
        public float TemperatureOutcome { get { return 0; } }

    }

    public class CompProperties_TemperatureSource : CompProperties
    {
        public float? minTemperature;
        public float? maxTemperature;
        public CompProperties_TemperatureSource()
        {
            compClass = typeof(CompTemperatureSource);
        }
    }
    public static class ProxyHeatMod
    {
        public static ProxyHeatSettings settings = new ProxyHeatSettings();
    }

    public class ProxyHeatSettings
    {
        public bool enableProxyHeatEffectIndoors;
    }

}
