using HarmonyLib;
using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Event;
using PreferenceSystem.Utils;
using System.Reflection;

namespace PreferenceSystem.Patches
{
    [HarmonyPatch(typeof(PlayerPauseView), "SetupMenus")]
    internal class PlayerPauseViewPatch
    {
        [HarmonyPrefix]
        private static void Postfix(PlayerPauseView __instance)
        {
            //FieldInfo field = ReflectionUtils.GetField<LocalMenuView<MenuAction>>("Panel");
            //PanelElement panelElement = (PanelElement)field.GetValue(__instance);
            FieldInfo field2 = ReflectionUtils.GetField<LocalMenuView<MenuAction>>("ModuleList");
            ModuleList module_list = (ModuleList)field2.GetValue(__instance);
            //panelElement.gameObject.SetActive(value: false);
            MethodInfo method = ReflectionUtils.GetMethod<LocalMenuView<MenuAction>>("AddMenu");
            PlayerPauseView_SetupMenusArgs args = new PlayerPauseView_SetupMenusArgs(__instance, method, module_list);
            EventUtils.InvokeEvent("PlayerPauseView_SetupMenusEvent", Events.PlayerPauseView_SetupMenusEvent?.GetInvocationList(), null, args);
        }
    }
}
