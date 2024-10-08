﻿using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Event;
using PreferenceSystem.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PreferenceSystem.Menus
{
    public class PreferenceSystemMenu<T> : Menu<T>
    {
        public PreferenceSystemMenu(Transform container, ModuleList module_list) : base(container, module_list) { }

        private static Dictionary<(Type, Type), string> RegisteredMenus = new Dictionary<(Type, Type), string>();
        private static Dictionary<(Type, Type), int> MenuPages = new Dictionary<(Type, Type), int>();
        private static Dictionary<(string, Type), Action<int>> RegisteredButtons = new Dictionary<(string, Type), Action<int>>();
        private static Dictionary<(string, Type), int> ButtonPages = new Dictionary<(string, Type), int>();

        private static int MenusPerPage = 5;

        private static Dictionary<(Type, int), int> MenuPageCounter = new Dictionary<(Type, int), int>();

        private static Dictionary<Type, List<int>> Pages = new Dictionary<Type, List<int>>();
        private static Dictionary<Type, List<string>> PageNames = new Dictionary<Type, List<string>>();

        private Option<int> PageSelector = null;

        private static int CurrentPage = 0;


        public static void RegisterMenu(string name, Type type, Type generic)
        {
            if (!RegisteredMenus.ContainsKey((type, generic)))
            {
                if (!Pages.ContainsKey(generic))
                    Pages.Add(generic, new List<int>());
                if (!PageNames.ContainsKey(generic))
                    PageNames.Add(generic, new List<string>());

                bool foundValidPage = false;
                int page = 0;
                RegisteredMenus.Add((type, generic), name);

                while (!foundValidPage)
                {
                    if (MenuPageCounter.ContainsKey((generic, page)))
                    {
                        int pageCount = MenuPageCounter[(generic, page)];
                        if (pageCount < MenusPerPage)
                        {
                            MenuPageCounter[(generic, page)]++;
                            foundValidPage = true;
                        }
                        else
                        {
                            page++;
                        }
                    }
                    else
                    {
                        MenuPageCounter.Add((generic, page), 0);
                        MenuPageCounter[(generic, page)]++;
                        foundValidPage = true;
                        break;
                    }
                }
                if (!Pages[generic].Contains(page))
                {
                    Pages[generic].Add(page);
                    PageNames[generic].Add("Page: " + page);
                }
                MenuPages.Add((type, generic), page);
            }
        }

        public static void RegisterButton(string name, Action<int> on_activate, Type generic)
        {
            if (!RegisteredButtons.ContainsKey((name, generic)))
            {
                if (!Pages.ContainsKey(generic))
                    Pages.Add(generic, new List<int>());
                if (!PageNames.ContainsKey(generic))
                    PageNames.Add(generic, new List<string>());

                RegisteredButtons.Add((name, generic), on_activate);

                bool foundValidPage = false;
                int page = 0;
                while (!foundValidPage)
                {
                    if (MenuPageCounter.ContainsKey((generic, page)))
                    {
                        int pageCount = MenuPageCounter[(generic, page)];
                        if (pageCount < MenusPerPage)
                        {
                            MenuPageCounter[(generic, page)]++;
                            foundValidPage = true;
                        }
                        else
                        {
                            page++;
                        }
                    }
                    else
                    {
                        MenuPageCounter.Add((generic, page), 0);
                        MenuPageCounter[(generic, page)]++;
                        foundValidPage = true;
                        break;
                    }
                }
                if (!Pages[generic].Contains(page))
                {
                    Pages[generic].Add(page);
                    PageNames[generic].Add("Page: " + page);
                }
                ButtonPages.Add((name, generic), page);
            }
        }



        public override void Setup(int player_id)
        {
            CurrentPage = 0;
            Type mainOrPauseMenuType = GetType().GetGenericArguments()[0];
            if (!Pages.ContainsKey(mainOrPauseMenuType))
                Pages.Add(mainOrPauseMenuType, new List<int>());
            if (!PageNames.ContainsKey(mainOrPauseMenuType))
                PageNames.Add(mainOrPauseMenuType, new List<string>());

            PageSelector = new Option<int>(Pages[mainOrPauseMenuType], CurrentPage, PageNames[mainOrPauseMenuType]);
            PageSelector.OnChanged += delegate (object _, int result)
            {
                CurrentPage = result;
                Redraw(CurrentPage);
            };

            Redraw(CurrentPage);
        }

        protected override ButtonElement AddSubmenuButton(string label, Type menu, bool skip_stack = false)
        {
            Main.LogInfo($"AddSubmenuButton {label} for menu: {menu}");
            return base.AddSubmenuButton(label, menu, skip_stack);
        }

        private void Redraw(int pageNumber = 0)
        {
            ModuleList.Clear();

            Type generic = GetType().GetGenericArguments()[0];

            AddLabel("Preference System");
            New<SpacerElement>(true);

            if (Pages[generic].Count >= 2)
            {
                AddSelect<int>(PageSelector);
            }

            New<SpacerElement>(true);

            bool hasMenus = false;

            foreach ((Type, Type) menu in RegisteredMenus.Keys)
            {
                if (menu.Item2 == generic)
                {
                    hasMenus = true;
                    if (MenuPages[menu] == pageNumber)
                    {
                        AddSubmenuButton(RegisteredMenus[menu], menu.Item1, false);
                    }
                }
            }

            foreach ((string, Type) menu in RegisteredButtons.Keys)
            {
                if (menu.Item2 == generic)
                {
                    hasMenus = true;
                    if (ButtonPages[menu] == pageNumber)
                    {
                        AddButton(menu.Item1, RegisteredButtons[menu]);
                    }
                }
            }

            if (!hasMenus)
            {
                AddInfo($"No {generic.Name} preferences.");
            }

            New<SpacerElement>(true);
            New<SpacerElement>(true);
            AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate (int i)
            {
                this.RequestPreviousMenu();
            }, 0, 1f, 0.2f);

            string prefSetName = PreferenceSystemRegistry.LastLoadedPreferenceSetName;
            if (!PreferenceSystemRegistry.LastLoadedPreferenceSetName.IsNullOrEmpty())
            {
                AddButton($"{(PreferenceSystemRegistry.IsPreferencesTampered ? "<color=#f55>" : string.Empty)}" +
                $"{PreferenceSystemRegistry.LastLoadedPreferenceSetName} Loaded", delegate (int _) { }, 0, 0.75f, 0.2f)
                .SetSelectable(false);
            }
        }

        public override void CreateSubmenus(ref Dictionary<Type, Menu<T>> menus)
        {
            EventUtils.InvokeEvent(nameof(Events.PreferenceMenu_MainMenu_CreateSubmenusEvent), Events.PreferenceMenu_MainMenu_CreateSubmenusEvent?.GetInvocationList(), null, new PreferenceMenu_CreateSubmenusArgs<T>(this, menus, this.Container, this.ModuleList));
        }
    }
}
