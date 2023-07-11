using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Event;
using PreferenceSystem.Utils;
using System.Linq;
using System.Reflection;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch(typeof(OptionsMenu<PauseMenuAction>), "Setup")]
    internal class OptionsMenu_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(StartMainMenu __instance)
        {
            ProfileManager.Main.Load();
            MethodInfo addActionButton = __instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single((m) => m.Name == "AddActionButton" && m.GetParameters().Length == 3);
            MethodInfo method = ReflectionUtils.GetMethod<MainMenu>("AddSubmenuButton");
            MethodInfo addSpacer = ReflectionUtils.GetMethod<MainMenu>("New").MakeGenericMethod(typeof(SpacerElement));
            MainMenu_SetupArgs args = new MainMenu_SetupArgs(__instance, addActionButton, method, addSpacer);
            EventUtils.InvokeEvent(Events.OptionsMenu_SetupEvent, args);
            return true;
        }
    }

    [HarmonyPatch(typeof(OptionsMenu<MainMenuAction>), "Setup")]
    internal class MainOptionsMenu_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(StartMainMenu __instance)
        {
            ProfileManager.Main.Load();
            MethodInfo addActionButton = __instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single((m) => m.Name == "AddActionButton" && m.GetParameters().Length == 3);
            MethodInfo method = ReflectionUtils.GetMethod<StartMainMenu>("AddSubmenuButton");
            MethodInfo addSpacer = ReflectionUtils.GetMethod<StartMainMenu>("New").MakeGenericMethod(typeof(SpacerElement));
            StartMainMenu_SetupArgs args = new StartMainMenu_SetupArgs(__instance, addActionButton, method, addSpacer);
            EventUtils.InvokeEvent(Events.StartOptionsMenu_SetupEvent, args);
            return true;
        }
    }
}
