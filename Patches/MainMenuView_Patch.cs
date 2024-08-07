using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Event;
using PreferenceSystem.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch(typeof(MainMenuView), "SetupMenus")]
    static class MainMenuViewPatch
    {
        private static void Postfix(MainMenuView __instance, ref Dictionary<Type, Menu<MenuAction>> ___Menus)
        {
            FieldInfo field2 = ReflectionUtils.GetField<LocalMenuView<MenuAction>>("ModuleList");
            ModuleList module_list = (ModuleList)field2.GetValue(__instance);
            MethodInfo method = ReflectionUtils.GetMethod<LocalMenuView<MenuAction>>("AddMenu");
            MainMenuView_SetupMenusArgs args = new MainMenuView_SetupMenusArgs(__instance, method, module_list);
            EventUtils.InvokeEvent("MainMenuView_SetupMenusEvent", Events.MainMenuView_SetupMenusEvent?.GetInvocationList(), null, args);

            foreach (KeyValuePair<Type, Menu<MenuAction>> menu in ___Menus)
            {
                menu.Value.Style = ElementStyle.MainMenu;
            }
        }
    }
}
