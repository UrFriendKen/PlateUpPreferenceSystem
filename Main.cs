using Kitchen;
using KitchenLib;
using KitchenLib.Event;
using KitchenMods;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace PreferenceSystem
{
    public class Main : BaseMod, IModSystem
    {
        // GUID must be unique and is recommended to be in reverse domain name notation
        // Mod Name is displayed to the player and listed in the mods menu
        // Mod Version must follow semver notation e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.PreferenceSystem";
        public const string MOD_NAME = "PreferenceSystem";
        public const string MOD_VERSION = "0.1.1";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.5";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.3" current and all future
        // e.g. ">=1.1.3 <=1.2.3" for all from/until

        private PreferenceSystemManager PrefManager;

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnInitialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        protected override void OnUpdate()
        {
        }

        // Example usage
        protected override void OnPostActivate(KitchenMods.Mod mod)
        {
            // For log file output so the official plateup support staff can identify if/which a mod is being used
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");

            base.OnPostActivate(mod);
            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);
            PrefManager.AddLabel("Label");
            PrefManager.AddInfo("Info");

            PrefManager.AddSubmenu("Submenu 1", "Submenu1");
            PrefManager.AddSubmenu("Nested Submenu", "NestedSubmenu");
            PrefManager.AddOption<bool>("boolPrefExample", true, new bool[] { false, true }, new string[] { "Disabled", "Enabled" });
            PrefManager.SubmenuDone();

            PrefManager.AddSubmenu("Submenu 2", "Submenu2");
            PrefManager.AddOption<int>("intPrefExample", 0, new int[] { 0, 1, 2 }, new string[] { "Option 0", "Option 1", "Option 2" });
            PrefManager.SubmenuDone();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.MainMenu);
            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }
        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
