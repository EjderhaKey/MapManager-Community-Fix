using Il2CppTLD.IntBackedUnit;

namespace MapManager
{
    [HarmonyPatch(typeof(Encumber), nameof(Encumber.GetEffectiveCarryCapacityKG))]
    internal static class Encumber_GetEffectiveCarryCapacityKG
    {
        private static void Postfix(ref ItemWeight __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return;

            __result = ItemWeight.FromKilograms(Settings.Instance.CustomCarryWeightKG);
        }
    }

    [HarmonyPatch]
    internal static class Encumber_GetCustomCarryThresholds
    {
        private static readonly string[] ThresholdMethods =
        {
            nameof(Encumber.GetNoSprintCarryCapacityKG),
            nameof(Encumber.GetEncumberLowThresholdKG),
            nameof(Encumber.GetEncumberMedThresholdKG),
            nameof(Encumber.GetEncumberHighThresholdKG)
        };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string methodName in ThresholdMethods)
                yield return AccessTools.Method(typeof(Encumber), methodName);
        }

        private static void Postfix(Encumber __instance, ref ItemWeight __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return;

            ItemWeight vanillaCapacity = __instance.GetMaxCarryCapacityKG();
            float vanillaCapacityKG = vanillaCapacity.ToQuantity(1f);
            if (vanillaCapacityKG <= 0f)
                return;

            float scale = Settings.Instance.CustomCarryWeightKG / vanillaCapacityKG;
            __result = __result * scale;
        }
    }

    [HarmonyPatch(typeof(Encumber), nameof(Encumber.GetNoWalkCarryCapacityKG))]
    internal static class Encumber_GetCustomNoWalkCapacity
    {
        private static void Postfix(ref ItemWeight __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return;

            ItemWeight minimumWalkCapacity = ItemWeight.FromKilograms(
                Settings.Instance.CustomCarryWeightKG + 100f);
            __result = ItemWeight.Max(__result, minimumWalkCapacity);
        }
    }

    [HarmonyPatch(typeof(Encumber), nameof(Encumber.GetEncumbranceSlowdownMultiplier))]
    internal static class Encumber_GetCustomCarrySlowdown
    {
        private static bool Prefix(Encumber __instance, ref float __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return true;

            ItemWeight customCapacity = ItemWeight.FromKilograms(Settings.Instance.CustomCarryWeightKG);
            if (__instance.GetGearWeightKG() > customCapacity)
                return true;

            __result = 1f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Encumber), nameof(Encumber.GetHourlyCalorieBurnFromWeight))]
    internal static class Encumber_LimitWeightCalorieBurn
    {
        // Keep the weight-related hunger cost at no more than the normal 30 kg load.
        private const float NormalCarryWeightKG = 30f;
        private static bool loggedFirstCall;
        private static bool loggedFirstLimit;

        private static void Postfix(Encumber __instance, ref float __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return;

            float maximumWeightBurn = Mathf.Max(0f, __instance.m_HourlyCalorieBurnPerKG * NormalCarryWeightKG);
            if (!loggedFirstCall)
            {
                MelonLogger.Msg($"CarryDiag weight hunger: initial={__result:F2}/h, cap={maximumWeightBurn:F2}/h");
                loggedFirstCall = true;
            }

            if (__result > maximumWeightBurn)
            {
                if (!loggedFirstLimit)
                {
                    MelonLogger.Msg($"CarryDiag weight hunger capped: {__result:F2}/h -> {maximumWeightBurn:F2}/h");
                    loggedFirstLimit = true;
                }
                __result = maximumWeightBurn;
            }
        }
    }
}
