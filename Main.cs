using PreferenceSystem.Event;
using KitchenMods;
using System;
using System.IO;
using UnityEngine;
using PreferenceSystem.Menus;
using Kitchen;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace PreferenceSystem
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.PreferenceSystem";
        public const string MOD_NAME = "PreferenceSystem";
        public const string MOD_VERSION = "1.0.0";

        private Harmony _harmonyInstance;
        private static List<Assembly> PatchedAssemblies = new List<Assembly>();

        PreferenceSystemManager _prefManager;

        internal const string PREFERENCE_SYSTEM_BUTTON_LOCATION_ID = "preferenceSystemButtonLocation";

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
        }

        private void SetupMenus()
        {
            Events.StartOptionsMenu_SetupEvent = (EventHandler<StartMainMenu_SetupArgs>)Delegate.Combine(Events.StartOptionsMenu_SetupEvent, (EventHandler<StartMainMenu_SetupArgs>)delegate (object s, StartMainMenu_SetupArgs args)
            {
                LogError("StartOptionsMenu invoke");
                args.addSubmenuButton.Invoke(args.instance, new object[3]
                {
                    "PreferenceSystem",
                    typeof(PreferenceSystemMenu<MainMenuAction>),
                    false
                });
            });
            Events.MainMenuView_SetupMenusEvent = (EventHandler<MainMenuView_SetupMenusArgs>)Delegate.Combine(Events.MainMenuView_SetupMenusEvent, (EventHandler<MainMenuView_SetupMenusArgs>)delegate (object s, MainMenuView_SetupMenusArgs args)
            {
                args.addMenu.Invoke(args.instance, new object[2]
                {
                    typeof(PreferenceSystemMenu<MainMenuAction>),
                    new PreferenceSystemMenu<MainMenuAction>(args.instance.ButtonContainer, args.module_list)
                });
            });
            Events.OptionsMenu_SetupEvent = (EventHandler<MainMenu_SetupArgs>)Delegate.Combine(Events.OptionsMenu_SetupEvent, (EventHandler<MainMenu_SetupArgs>)delegate (object s, MainMenu_SetupArgs args)
            {
                args.addSubmenuButton.Invoke(args.instance, new object[3]
                {
                    "PreferenceSystem",
                    typeof(PreferenceSystemMenu<PauseMenuAction>),
                    false
                });
            });
            Events.PlayerPauseView_SetupMenusEvent = (EventHandler<PlayerPauseView_SetupMenusArgs>)Delegate.Combine(Events.PlayerPauseView_SetupMenusEvent, (EventHandler<PlayerPauseView_SetupMenusArgs>)delegate (object s, PlayerPauseView_SetupMenusArgs args)
            {
                args.addMenu.Invoke(args.instance, new object[2]
                {
                    typeof(PreferenceSystemMenu<PauseMenuAction>),
                    new PreferenceSystemMenu<PauseMenuAction>(args.instance.ButtonContainer, args.module_list)
                });
            });
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
