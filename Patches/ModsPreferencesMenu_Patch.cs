using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Menus;
using PreferenceSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch]
    static class ModsPreferencesMenu_Patch
    {
        enum ElementMethod
        {
            AddSpacer,
            AddLabel,
            AddButton,
            AddActionButton,
            AddSubmenuButton
        }

        static Dictionary<ElementMethod, MethodInfo> _methodInfos;

        [HarmonyPatch(typeof(ModsPreferencesMenu<PauseMenuAction>), nameof(ModsPreferencesMenu<PauseMenuAction>.Setup))]
        [HarmonyPostfix]
        static void Setup_Postfix(ModsPreferencesMenu<PauseMenuAction> __instance)
        {
            string prefSetName = PreferenceSystemRegistry.LastLoadedPreferenceSetName;
            if (!PreferenceSystemRegistry.LastLoadedPreferenceSetName.IsNullOrEmpty())
            {
                if (_methodInfos == null)
                {
                    Type instanceType = __instance.GetType();
                    _methodInfos = new Dictionary<ElementMethod, MethodInfo>()
                    {
                        { ElementMethod.AddSpacer, instanceType.GetMethod("New", BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(typeof(SpacerElement)) },
                        { ElementMethod.AddLabel, instanceType.GetMethod("AddLabel", BindingFlags.NonPublic | BindingFlags.Instance) },
                        { ElementMethod.AddButton, instanceType.GetMethod("AddButton", BindingFlags.NonPublic | BindingFlags.Instance) },
                        { ElementMethod.AddActionButton, instanceType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)?.Single((MethodInfo m) => m.Name == "AddActionButton" && m.GetParameters().Length == 3) },
                        { ElementMethod.AddSubmenuButton, instanceType.GetMethod("AddSubmenuButton", BindingFlags.NonPublic | BindingFlags.Instance) }
                    };
                }

                try
                {
                    object obj = _methodInfos[ElementMethod.AddButton]?.Invoke(__instance, new object[] { $"{(PreferenceSystemRegistry.IsPreferencesTampered? "<color=#f55>" : string.Empty)}" +
                        $"{PreferenceSystemRegistry.LastLoadedPreferenceSetName} Loaded", delegate (int _) { }, 0, 0.75f, 0.2f });

                    if (obj != null && obj is ButtonElement preferenceSetButton)
                    {
                        preferenceSetButton.SetSelectable(false);
                    }
                }
                catch (Exception ex)
                {
                    Main.LogError($"{ex.Message}\n{ex.StackTrace})");
                }
            }
        }
    }
}
