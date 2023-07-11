using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PreferenceSystem.Patches
{
    //[HarmonyPatch]
    //static class MainMenu_Patch
    //{
    //    static MethodInfo m_AddInfo;
    //    [HarmonyPatch(typeof(MainMenu), "Setup")]
    //    [HarmonyPostfix]
    //    static void Setup_Postfix(MainMenu __instance)
    //    {
    //        if (m_AddInfo == null)
    //            m_AddInfo = __instance.GetType().GetMethod("AddInfo", BindingFlags.NonPublic | BindingFlags.Instance);
    //        if (Main.PrefManager.Get<bool>(Main.CONSOLIDATE_MODDED_ROOT_BUTTONS_ID))
    //            m_AddInfo?.Invoke(__instance, new object[] { "Some buttons moved to Options - PreferenceSystem" });
    //    }
    //}

    [HarmonyPatch]
    static class MainMenu_Patch
    {
        enum ElementMethod
        {
            AddSpacer,
            AddLabel,
            AddButton,
            AddActionButton,
            AddSubmenuButton,
            AddInfo
        }

        static Dictionary<ElementMethod, MethodInfo> _methodInfos;

        [HarmonyPatch(typeof(MainMenu), "Setup")]
        [HarmonyPostfix]
        static void Setup_Postfix(MainMenu __instance)
        {
            if (_methodInfos == null)
            {
                Type instanceType = __instance.GetType();
                _methodInfos = new Dictionary<ElementMethod, MethodInfo>()
                    {
                        { ElementMethod.AddInfo, instanceType.GetMethod("AddInfo", BindingFlags.NonPublic | BindingFlags.Instance) },
                        { ElementMethod.AddSpacer, instanceType.GetMethod("New", BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(typeof(SpacerElement)) },
                        { ElementMethod.AddLabel, instanceType.GetMethod("AddLabel", BindingFlags.NonPublic | BindingFlags.Instance) },
                        { ElementMethod.AddButton, instanceType.GetMethod("AddButton", BindingFlags.NonPublic | BindingFlags.Instance) },
                        { ElementMethod.AddActionButton, instanceType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)?.Single((MethodInfo m) => m.Name == "AddActionButton" && m.GetParameters().Length == 3) },
                        { ElementMethod.AddSubmenuButton, instanceType.GetMethod("AddSubmenuButton", BindingFlags.NonPublic | BindingFlags.Instance) }
                    };
            }

            //string prefSetName = PreferenceSystemRegistry.LastLoadedPreferenceSetName;
            //if (!PreferenceSystemRegistry.LastLoadedPreferenceSetName.IsNullOrEmpty())
            //{
            //    try
            //    {
            //        object obj = _methodInfos[ElementMethod.AddButton]?.Invoke(__instance, new object[] { $"{(PreferenceSystemRegistry.IsPreferencesTampered? "<color=#f55>" : string.Empty)}" +
            //            $"{PreferenceSystemRegistry.LastLoadedPreferenceSetName} Loaded", delegate (int _) { }, 0, 0.75f, 0.2f });

            //        if (obj != null && obj is ButtonElement preferenceSetButton)
            //        {
            //            preferenceSetButton.SetSelectable(false);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Main.LogError($"{ex.Message}\n{ex.StackTrace})");
            //    }
            //}

            if (Main.PrefManager.Get<bool>(Main.CONSOLIDATE_MODDED_ROOT_BUTTONS_ID))
            {
                try
                {
                    object obj = _methodInfos[ElementMethod.AddButton]?.Invoke(__instance, new object[] { "Some buttons moved to Options => PreferenceSystem", delegate (int _) { }, 0, 0.75f, 0.2f });

                    if (obj != null && obj is ButtonElement buttonsMovedLabel)
                    {
                        buttonsMovedLabel.SetSelectable(false);
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
