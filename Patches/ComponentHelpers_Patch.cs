using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenMods;
using Unity.Entities;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch]
    static class EffectHelpers_Patch
    {
        [HarmonyPatch(typeof(EffectHelpers), nameof(EffectHelpers.AddApplianceEffectComponents))]
        [HarmonyPrefix]
        [HarmonyPriority(int.MinValue)]
        static bool AddApplianceEffectComponents_Prefix(bool __runOriginal, EntityCommandBuffer ecb, Entity e, IEffectPropertySource prop)
        {
            if (!__runOriginal ||
                prop.EffectRange == null ||
                prop.EffectType == null)
                return true;

            if (!(prop.EffectCondition is IModComponent) &&
                !(prop.EffectRange is IModComponent) &&
                !(prop.EffectType is IModComponent))
                return true;

            ecb.AddComponent(e, default(CAppliesEffect));

            if (prop.EffectCondition == null)
                ecb.AddComponent(e, default(CEffectAlways));
            else
                ecb.AddComponent(e, (dynamic)prop.EffectCondition);

            ecb.AddComponent(e, (dynamic)prop.EffectRange);
            ecb.AddComponent(e, (dynamic)prop.EffectType);

            return false;
        }
    }
}
