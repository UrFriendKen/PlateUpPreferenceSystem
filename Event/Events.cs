using Kitchen;
using Kitchen.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PreferenceSystem.Event
{
    public static class Events
    {
        public static EventHandler<PreferencesSaveArgs> PreferencesSaveEvent;

        public static EventHandler<MainMenuView_SetupMenusArgs> MainMenuView_SetupMenusEvent;
        public static EventHandler<StartMainMenu_SetupArgs> StartMainMenu_SetupEvent;
        public static EventHandler<StartMainMenu_SetupArgs> StartOptionsMenu_SetupEvent;
        public static EventHandler<MainMenu_SetupArgs> MainMenu_SetupEvent;
        public static EventHandler<MainMenu_SetupArgs> OptionsMenu_SetupEvent;
        public static EventHandler<PlayerPauseView_SetupMenusArgs> PlayerPauseView_SetupMenusEvent;

        public static EventHandler<PreferenceMenu_CreateSubmenusArgs<MenuAction>> PreferenceMenu_MainMenu_CreateSubmenusEvent;
        public static EventHandler<PreferenceMenu_CreateSubmenusArgs<MenuAction>> PreferenceMenu_PauseMenu_CreateSubmenusEvent;
    }

    public class PreferencesSaveArgs : EventArgs
    {
        internal PreferencesSaveArgs()
        {
        }
    }

    public class MainMenuView_SetupMenusArgs : EventArgs
    {
        public readonly MainMenuView instance;

        public readonly MethodInfo addMenu;

        public readonly ModuleList module_list;

        internal MainMenuView_SetupMenusArgs(MainMenuView instance, MethodInfo addMenu, ModuleList module_list)
        {
            this.instance = instance;
            this.addMenu = addMenu;
            this.module_list = module_list;
        }

        public void AddMenu<T>(Type type, Menu<MenuAction> menuInstance, ModuleList module_list)
        {
            addMenu.Invoke(instance, new object[] { type, menuInstance });
            menuInstance.Style = ElementStyle.MainMenu;
        }
    }

    public class PreferenceMenu_CreateSubmenusArgs<T> : EventArgs
    {
        public readonly Dictionary<Type, Menu<T>> Menus;

        public readonly Transform Container;

        public readonly ModuleList Module_list;

        public readonly object instance;

        internal PreferenceMenu_CreateSubmenusArgs(object instance, Dictionary<Type, Menu<T>> menus, Transform container, ModuleList module_list)
        {
            this.instance = instance;
            Menus = menus;
            Container = container;
            Module_list = module_list;
        }
    }
    public class StartMainMenu_SetupArgs : EventArgs
    {
        public readonly StartMainMenu instance;

        public readonly MethodInfo addActionButton;

        public readonly MethodInfo addSubmenuButton;

        public readonly MethodInfo addSpacer;

        internal StartMainMenu_SetupArgs(StartMainMenu instance, MethodInfo addActionButton, MethodInfo addSubmenuButton, MethodInfo addSpacer)
        {
            this.instance = instance;
            this.addActionButton = addActionButton;
            this.addSubmenuButton = addSubmenuButton;
            this.addSpacer = addSpacer;
        }

        public void AddSubmenuButton(object[] parameters)
        {
            addSubmenuButton.Invoke(instance, parameters);
        }

        public void AddSpacer(object[] parameters)
        {
            addSpacer.Invoke(instance, parameters);
        }

        public void AddActionButtion(object[] parameters)
        {
            addActionButton.Invoke(instance, parameters);
        }
    }

    public class MainMenu_SetupArgs : EventArgs
    {
        public readonly StartMainMenu instance;

        public readonly MethodInfo addActionButton;

        public readonly MethodInfo addSubmenuButton;

        public readonly MethodInfo addSpacer;

        internal MainMenu_SetupArgs(StartMainMenu instance, MethodInfo addActionButton, MethodInfo addSubmenuButton, MethodInfo addSpacer)
        {
            this.instance = instance;
            this.addActionButton = addActionButton;
            this.addSubmenuButton = addSubmenuButton;
            this.addSpacer = addSpacer;
        }

        public void AddSubmenuButton(object[] parameters)
        {
            addSubmenuButton.Invoke(instance, parameters);
        }

        public void AddSpacer(object[] parameters)
        {
            addSpacer.Invoke(instance, parameters);
        }

        public void AddActionButtion(object[] parameters)
        {
            addActionButton.Invoke(instance, parameters);
        }
    }

    public class PlayerPauseView_SetupMenusArgs : EventArgs
    {
        public readonly PlayerPauseView instance;

        public readonly MethodInfo addMenu;

        public readonly ModuleList module_list;

        internal PlayerPauseView_SetupMenusArgs(PlayerPauseView instance, MethodInfo addMenu, ModuleList module_list)
        {
            this.instance = instance;
            this.addMenu = addMenu;
            this.module_list = module_list;
        }

        public void AddMenu<T>(Type type, Menu<MenuAction> menuInstance)
        {
            addMenu.Invoke(instance, new object[] { type, menuInstance });
        }
    }
}
