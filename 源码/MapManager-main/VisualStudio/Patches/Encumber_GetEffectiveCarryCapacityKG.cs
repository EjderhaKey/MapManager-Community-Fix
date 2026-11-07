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
        private static void Postfix(ref float __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return;

            // Infinite carry must not add any extra hunger drain based on carried weight.
            __result = 0f;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.CalculateModifiedCalorieBurnRate), new Type[] { typeof(float) })]
    internal static class PlayerManager_CalculateModifiedCalorieBurnRate
    {
        private static bool _loggedActive;

        private static void Postfix(PlayerManager __instance, float baseBurnRate, ref float __result)
        {
            if (!Settings.Instance.EnableCustomCarryWeight)
                return;

            if (!_loggedActive)
            {
                MelonLogger.Msg("[MapManager] Infinite carry calorie patch active.");
                _loggedActive = true;
            }

            // Rebuild the game's normal calorie rate without its carried-weight surcharge.
            float rate = baseBurnRate;
            if (GameManager.GetFreezingComponent().IsFreezing())
                rate *= GameManager.GetFreezingComponent().m_CalorieBurnMultiplier;

            if (__instance.PlayerIsSprinting() || __instance.PlayerIsWalking())
            {
                float verticalVelocity = GameManager.GetVpFPSPlayer().Controller.Velocity.normalized.y;
                if (verticalVelocity > 0.1f)
                {
                    float uphill = Mathf.Clamp((verticalVelocity - 0.1f) / 0.5f, 0f, 1f);
                    rate *= Mathf.Lerp(1f, 1.5f, uphill);
                }
            }

            rate *= GameManager.GetExperienceModeManagerComponent().GetCalorieBurnScale();
            __result = rate * GameManager.GetFeatEfficientMachine().ReduceCaloriesScale();
        }
    }
}
