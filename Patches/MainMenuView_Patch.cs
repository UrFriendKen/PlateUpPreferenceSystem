using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Event;
using PreferenceSystem.Utils;
using System.Reflection;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch(typeof(MainMenuView), "SetupMenus")]
    static class MainMenuViewPatch
    {
        private static bool Prefix(MainMenuView __instance)
        {
            FieldInfo field = ReflectionUtils.GetField<LocalMenuView<MainMenuAction>>("Panel");
            PanelElement panelElement = (PanelElement)field.GetValue(__instance);
            FieldInfo field2 = ReflectionUtils.GetField<LocalMenuView<MainMenuAction>>("ModuleList");
            ModuleList module_list = (ModuleList)field2.GetValue(__instance);
            panelElement.gameObject.SetActive(value: false);
            MethodInfo method = ReflectionUtils.GetMethod<LocalMenuView<MainMenuAction>>("AddMenu");
            MainMenuView_SetupMenusArgs args = new MainMenuView_SetupMenusArgs(__instance, method, module_list);
            EventUtils.InvokeEvent("MainMenuView_SetupMenusEvent", Events.MainMenuView_SetupMenusEvent?.GetInvocationList(), null, args);
            return true;
        }
    }
}
