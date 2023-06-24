using KitchenLib;
using KitchenMods;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PreferenceSystem
{
    public class Main : BaseMod, IModSystem
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.PreferenceSystem";
        public const string MOD_NAME = "PreferenceSystem";
        public const string MOD_VERSION = "0.2.4";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.5";

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnPostActivate(Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            RegisterMenu<PreferenceSystemMenu>();

            if (!Directory.Exists($"{PreferenceSystemRegistry.PREFERENCE_SETS_FOLDER_PATH}"))
                Directory.CreateDirectory($"{PreferenceSystemRegistry.PREFERENCE_SETS_FOLDER_PATH}");
        }

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
