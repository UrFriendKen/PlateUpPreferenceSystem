using HarmonyLib;
using Kitchen;
using KitchenMods;
using PreferenceSystem.Event;
using PreferenceSystem.Menus;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PreferenceSystem
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.PreferenceSystem";
        public const string MOD_NAME = "PreferenceSystem";
        public const string MOD_VERSION = "1.0.3";

        internal static PreferenceSystemManager PrefManager;

        private Harmony _harmonyInstance;
        private static List<Assembly> PatchedAssemblies = new List<Assembly>();

        internal const string PREFERENCE_SYSTEM_BUTTON_LOCATION_ID = "preferenceSystemButtonLocation";
        internal const string CONSOLIDATE_MODDED_ROOT_BUTTONS_ID = "consolidateModdedRootButtons";

        internal const string CONSOLIDATION_WARNING_TEXT = "Some buttons moved to Options => PreferenceSystem";

        public Main()
        {
            if (_harmonyInstance == null)
            {
                _harmonyInstance = new Harmony(MOD_GUID);
            }
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (!PatchedAssemblies.Contains(assembly) && assembly != null)
            {
                _harmonyInstance.PatchAll(assembly);
                PatchedAssemblies.Add(assembly);
            }
        }

        public void PostActivate(Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");

            if (!Directory.Exists($"{PreferenceSystemRegistry.PREFERENCE_SETS_FOLDER_PATH}"))
                Directory.CreateDirectory($"{PreferenceSystemRegistry.PREFERENCE_SETS_FOLDER_PATH}");

            SetupMenus();

            PrefManager = new PreferenceSystemManager(MOD_GUID, $"{MOD_NAME} Settings");
            PrefManager
                .AddLabel("Modded Root Buttons")
                .AddInfo("Consolidating moves all modded buttons into PreferenceSystem menu.")
                .AddOption<bool>(
                    CONSOLIDATE_MODDED_ROOT_BUTTONS_ID,
                    false,
                    new bool[] { false, true },
                    new string[] { "Ignore", "Consolidate" })
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }

        private void SetupMenus()
        {
            Events.StartOptionsMenu_SetupEvent = delegate (object s, StartMainMenu_SetupArgs args)
            {
                args.addSubmenuButton.Invoke(args.instance, new object[3]
                {
                    "PreferenceSystem",
                    typeof(PreferenceSystemMenu<MainMenuAction>),
                    false
                });
            };
            Events.MainMenuView_SetupMenusEvent = delegate (object s, MainMenuView_SetupMenusArgs args)
            {
                args.addMenu.Invoke(args.instance, new object[2]
                {
                    typeof(PreferenceSystemMenu<MainMenuAction>),
                    new PreferenceSystemMenu<MainMenuAction>(args.instance.ButtonContainer, args.module_list)
                });
            };
            Events.OptionsMenu_SetupEvent = delegate (object s, MainMenu_SetupArgs args)
            {
                args.addSubmenuButton.Invoke(args.instance, new object[3]
                {
                    "PreferenceSystem",
                    typeof(PreferenceSystemMenu<PauseMenuAction>),
                    false
                });
            };
            Events.PlayerPauseView_SetupMenusEvent += delegate (object s, PlayerPauseView_SetupMenusArgs args)
            {
                args.addMenu.Invoke(args.instance, new object[2]
                {
                    typeof(PreferenceSystemMenu<PauseMenuAction>),
                    new PreferenceSystemMenu<PauseMenuAction>(args.instance.ButtonContainer, args.module_list)
                });
            };
        }

        public void PreInject()
        {
            //RegisterMenu<PreferenceSystemMenu>();
        }

        public void PostInject() { }

        #region Logging
        // You can remove this, I just prefer a more standardized logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
