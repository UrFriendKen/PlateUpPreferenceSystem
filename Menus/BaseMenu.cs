using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Preferences;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PreferenceSystem.Menus
{
    public class BaseMenu<T> : Menu<T>
    {
        private string mod_id = "";

        private int CreateNewProfileIndex;

        private PreferenceManager manager;

        public BaseMenu(Transform container, ModuleList module_list)
            : base(container, module_list)
        {
        }

        public override void Setup(int player_id)
        {
        }

        protected void BoolOption(PreferenceBool pref)
        {
            Add(new Option<bool>(new List<bool> { false, true }, pref.Value, new List<string>
            {
                base.Localisation["SETTING_DISABLED"],
                base.Localisation["SETTING_ENABLED"]
            })).OnChanged += delegate (object _, bool f)
            {
                pref.Set(f);
                manager.Save();
            };
        }

        protected void AddProfileSelector(string mod_id, Action<string> action, PreferenceManager manager, bool updateOnHighlight = true)
        {
            this.mod_id = mod_id;
            this.manager = manager;
            List<string> list = GlobalPreferences.GetProfiles(mod_id).ToList();
            string current_profile = GlobalPreferences.GetProfile(mod_id);
            if (list.Count > 0)
            {
                if (!list.Contains(current_profile))
                {
                    current_profile = list[0];
                }
            }
            else
            {
                current_profile = "";
            }
            list.Add("Create");
            CreateNewProfileIndex = list.Count - 1;
            manager.SetProfile(current_profile);
            manager.Load();
            Option<string> option = new Option<string>(list, current_profile, list);
            SelectElement selectElement = AddSelect(option);
            option.OnChanged += delegate (object s, string args)
            {
                current_profile = args;
            };
            if (updateOnHighlight)
            {
                selectElement.OnOptionChosen += delegate (int i)
                {
                    if (i == CreateNewProfileIndex)
                    {
                        RequestSubMenu(typeof(TextEntryMainMenu), skip_stack: true);
                        TextInputView.RequestTextInput(base.Localisation["NEW_PROFILE_PROMPT"], "", 20, CreateNewProfile);
                    }
                };
                selectElement.OnOptionHighlighted += delegate
                {
                    if (current_profile != "Create")
                    {
                        Main.LogInfo("------------------------ Selected Profile " + current_profile);
                        GlobalPreferences.SetProfile(mod_id, current_profile);
                        action(current_profile);
                        manager.SetProfile(current_profile);
                        manager.Load();
                    }
                    else
                    {
                        manager.SetProfile();
                        manager.Load();
                    }
                    manager.Save();
                };
                return;
            }
            selectElement.OnOptionChosen += delegate (int i)
            {
                if (i == CreateNewProfileIndex)
                {
                    RequestSubMenu(typeof(TextEntryMainMenu), skip_stack: true);
                    TextInputView.RequestTextInput(base.Localisation["NEW_PROFILE_PROMPT"], "", 20, CreateNewProfile);
                }
                else
                {
                    if (current_profile != "Create")
                    {
                        Main.LogInfo("------------------------ Selected Profile " + current_profile);
                        GlobalPreferences.SetProfile(mod_id, current_profile);
                        action(current_profile);
                        manager.SetProfile(current_profile);
                        manager.Load();
                    }
                    else
                    {
                        manager.SetProfile();
                        manager.Load();
                    }
                    manager.Save();
                }
            };
        }

        private void CreateNewProfile(TextInputView.TextInputState result, string name)
        {
            if (result == TextInputView.TextInputState.TextEntryComplete && name != "Create")
            {
                List<string> list = GlobalPreferences.GetProfiles(mod_id).ToList();
                if (!list.Contains(name))
                {
                    GlobalPreferences.AddProfile(mod_id, name);
                }
                GlobalPreferences.SetProfile(mod_id, name);
                manager.SetProfile(name);
                manager.Load();
                manager.Save();
            }
            RequestSubMenu(GetType(), skip_stack: true);
        }
    }
}
