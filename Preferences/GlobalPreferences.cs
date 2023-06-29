using System;
using System.Collections.Generic;
using System.Linq;
using PreferenceSystem.Menus;

namespace PreferenceSystem.Preferences
{
    public static class GlobalPreferences
    {
        private static PreferenceManager Manager;

        private static PreferenceDictionary<string, string> CurrentProfilePreference;

        private static PreferenceDictionary<string, string[]> ModProfilesPreference;

        private static PreferenceEnum<MenuLocations> MenuButtonLocationPreference;

        public static void Setup()
        {
            if (Manager == null)
            {
                Manager = new PreferenceManager("global");
            }
            CurrentProfilePreference = Manager.RegisterPreference(new PreferenceDictionary<string, string>("modCurrentProfile", new Dictionary<string, string>()));
            ModProfilesPreference = Manager.RegisterPreference(new PreferenceDictionary<string, string[]>("modProfiles", new Dictionary<string, string[]>()));
            MenuButtonLocationPreference = Manager.RegisterPreference(new PreferenceEnum<MenuLocations>("modCurrentProfile", MenuLocations.Options));
            Manager.Load();
        }

        public static string[] GetProfiles(string mod_id)
        {
            if (Manager == null)
            {
                Setup();
            }
            if (ModProfilesPreference.Get().ContainsKey(mod_id))
            {
                return ModProfilesPreference.Get()[mod_id];
            }
            return new string[0];
        }

        public static void AddProfile(string mod_id, string profile)
        {
            if (Manager == null)
            {
                Setup();
            }
            List<string> list = GetProfiles(mod_id).ToList();
            list.Add(profile);
            ModProfilesPreference.Get()[mod_id] = list.ToArray();
            Manager.Save();
        }

        public static void RemoveProfile(string mod_id, string profile)
        {
            if (Manager == null)
            {
                Setup();
            }
            List<string> list = GetProfiles(mod_id).ToList();
            list.Remove(profile);
            ModProfilesPreference.Get()[mod_id] = list.ToArray();
            Manager.Save();
        }

        public static bool DoesProfileExist(string mod_id, string profile)
        {
            if (Manager == null)
            {
                Setup();
            }
            return GetProfiles(mod_id).Contains(profile);
        }

        public static string GetProfile(string mod_id)
        {
            if (Manager == null)
            {
                Setup();
            }
            if (CurrentProfilePreference.Get().ContainsKey(mod_id))
            {
                return CurrentProfilePreference.Get()[mod_id];
            }
            return "";
        }

        public static void SetProfile(string mod_id, string profile)
        {
            if (Manager == null)
            {
                Setup();
            }
            if (CurrentProfilePreference.Get().ContainsKey(mod_id))
            {
                CurrentProfilePreference.Get()[mod_id] = profile;
            }
            else
            {
                CurrentProfilePreference.Get().Add(mod_id, profile);
            }
            Manager.Save();
        }
    }
}
