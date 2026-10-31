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
}
