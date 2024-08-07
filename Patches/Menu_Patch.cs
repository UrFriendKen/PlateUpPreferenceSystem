using HarmonyLib;
using Kitchen;
using KitchenData;
using PreferenceSystem.Menus;
using System;
using System.Collections.Generic;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch]
    static class Menu_Patch
    {
        internal static Dictionary<string, (Type type, bool skipStack)> InterceptedRootButtons = new Dictionary<string, (Type type, bool skipStack)>();

        static HashSet<string> _ignoredLabels => new HashSet<string>()
        {
            "Mods",
            "Mod Preferences",
            GameData.Main.GlobalLocalisation["MENU_DISCONNECT"],
            GameData.Main.GlobalLocalisation["MENU_LEAVE_TUTORIAL"],
            GameData.Main.GlobalLocalisation["MENU_RECIPES"],
            GameData.Main.GlobalLocalisation["MENU_ACTIVE_CARDS"],
            GameData.Main.GlobalLocalisation["MENU_PRACTICE_MODE"],
            GameData.Main.GlobalLocalisation["MENU_OPTIONS"],
            GameData.Main.GlobalLocalisation["MAIN_MENU_ONLINE_PLAY"],
            GameData.Main.GlobalLocalisation["MENU_REMOVE_INPUT"],
            GameData.Main.GlobalLocalisation["MENU_MULTIPLAYER"],
            GameData.Main.GlobalLocalisation["MENU_ABANDON"],
            GameData.Main.GlobalLocalisation["MENU_QUIT_TO_LOBBY"],
            GameData.Main.GlobalLocalisation["MENU_CONTINUE"],
            GameData.Main.GlobalLocalisation["MENU_QUIT"],
            "Rehearsal Time",
            "Restart Day",
            Main.CONSOLIDATION_WARNING_TEXT
        };

        [HarmonyPatch(typeof(Menu<MenuAction>), "AddSubmenuButton")]
        [HarmonyPrefix]
        static bool AddSubmenuButton_Patch(Menu<MenuAction> __instance, string label, Type menu, bool skip_stack = false)
        {
            if ((Main.PrefManager?.Get<bool>(Main.CONSOLIDATE_MODDED_ROOT_BUTTONS_ID) ?? false) &&
                __instance.GetType() == typeof(MainMenu) && !_ignoredLabels.Contains(label))
            {
                if (!InterceptedRootButtons.ContainsKey(label))
                {
                    PreferenceSystemMenu<MenuAction>.RegisterMenu(label, menu, typeof(MenuAction));
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Menu<MenuAction>), "AddButton")]
        [HarmonyPrefix]
        static bool AddButton_Patch(Menu<MenuAction> __instance, string label, Action<int> on_activate)
        {
            if ((Main.PrefManager?.Get<bool>(Main.CONSOLIDATE_MODDED_ROOT_BUTTONS_ID) ?? false) &&
                __instance.GetType() == typeof(MainMenu) && !_ignoredLabels.Contains(label))
            {
                if (!InterceptedRootButtons.ContainsKey(label))
                {
                    PreferenceSystemMenu<MenuAction>.RegisterButton(label, on_activate, typeof(MenuAction));
                }
                return false;
            }
            return true;
        }
    }
}
