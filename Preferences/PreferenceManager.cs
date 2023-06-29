using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PreferenceSystem.Preferences
{
    public class PreferenceManager
    {
        private class StoredPreference
        {
            public string Key;

            public string Value;

            public string Type;
        }

        private readonly string PREFERENCE_FOLDER_PATH = Application.persistentDataPath + "/PreferenceSystem/Preferences";

        private string preferenceFilePath = "";

        private readonly string modId;

        private readonly Dictionary<(string, string), PreferenceBase> preferences = new Dictionary<(string, string), PreferenceBase>();

        private string currentProfile = "";

        /// <summary>
        /// Create a preference manager attached to the given mod ID.
        /// </summary>
        /// <param name="modId">The mod ID.</param>
        public PreferenceManager(string modId)
        {
            this.modId = modId;
            if (!Directory.Exists(PREFERENCE_FOLDER_PATH + "/" + this.modId))
            {
                Directory.CreateDirectory(PREFERENCE_FOLDER_PATH + "/" + this.modId);
            }
            preferenceFilePath = PREFERENCE_FOLDER_PATH + "/" + this.modId + "/" + this.modId + currentProfile + ".json";
        }

        /// <summary>
        /// Set the current mod-level preference profile of the preference manager.
        /// </summary>
        /// <param name="profile">The name of the profile.</param>
        public void SetProfile(string profile = "")
        {
            if (!string.IsNullOrEmpty(profile))
            {
                profile = "-" + profile;
            }
            currentProfile = profile;
            preferenceFilePath = PREFERENCE_FOLDER_PATH + "/" + modId + "/" + modId + currentProfile + ".json";
        }

        /// <summary>
        /// Get the preference associated with a given key. Preferences need to be registered with 
        /// <see cref="M:PreferenceSystem.Preferences.PreferenceManager.RegisterPreference``1(``0)" /> before using this.
        /// </summary>
        /// <typeparam name="T">The type of the preference.</typeparam>
        /// <param name="key">The key of the preference.</param>
        /// <returns>The requested preference.</returns>
        public T GetPreference<T>(string key) where T : PreferenceBase
        {
            if (preferences.ContainsKey((key, typeof(T).Name)))
            {
                return (T)preferences[(key, typeof(T).Name)];
            }
            Main.LogWarning("Unable to get preference with " + key + ", key not registered.");
            return null;
        }

        /// <summary>
        /// Get the value associated with the preference with a given key. Preferences need to be 
        /// registered with <see cref="M:PreferenceSystem.Preferences.PreferenceManager.RegisterPreference``1(``0)" /> before using this. It is recommended
        /// to use <see cref="M:PreferenceSystem.Preferences.PreferenceManager.GetPreference``1(System.String)" /> along with <see cref="M:PreferenceSystem.Preferences.PreferenceBase`1.Get" /> 
        /// instead of this method.
        /// </summary>
        /// <typeparam name="T">The type of the preference.</typeparam>
        /// <param name="key">The key of the preference.</param>
        /// <returns>The value associated with the preference.</returns>
        public object Get<T>(string key) where T : PreferenceBase
        {
            if (preferences.ContainsKey((key, typeof(T).Name)))
            {
                return ((dynamic)preferences[(key, typeof(T).Name)]).Get();
            }
            Main.LogWarning("Unable to get value of " + key + ", key not registered.");
            return null;
        }

        /// <summary>
        /// Get the value associated with the preference with a given key. Preferences need to be 
        /// registered with <see cref="M:PreferenceSystem.Preferences.PreferenceManager.RegisterPreference``1(``0)" /> before using this. It is recommended
        /// to use <see cref="M:PreferenceSystem.Preferences.PreferenceBase`1.Set(`0)" /> instead of this method. Note that this method
        /// is not type safe and will throw an exception if the given value is not the correct type
        /// for the preference.
        /// </summary>
        /// <typeparam name="T">The type of the preference.</typeparam>
        /// <param name="key">The key of the preference.</param>
        /// <param name="value">The new value of the preference.</param>
        public void Set<T>(string key, object value)
        {
            if (preferences.ContainsKey((key, typeof(T).Name)))
            {
                ((dynamic)preferences[(key, typeof(T).Name)]).Set((dynamic)value);
            }
            else
            {
                Main.LogWarning("Unable to set value of " + key + ", key not registered.");
            }
        }

        /// <summary>
        /// Save the current values of the preferences managed by this preference manager to the
        /// current profile's file on disk.
        /// </summary>
        public void Save()
        {
            List<StoredPreference> list = new List<StoredPreference>();
            foreach (var key in preferences.Keys)
            {
                list.Add(new StoredPreference
                {
                    Key = key.Item1,
                    Value = preferences[key].Serialize(),
                    Type = key.Item2
                });
            }
            string contents = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(preferenceFilePath, contents);
        }

        /// <summary>
        /// Load the values of the preferences managed by this preference manager from the
        /// current profile's file on disk.
        /// </summary>
        public void Load()
        {
            string value = "";
            if (File.Exists(preferenceFilePath))
            {
                value = File.ReadAllText(preferenceFilePath);
            }
            if (string.IsNullOrEmpty(value))
            {
                Main.LogWarning("Unable to load preferences, file empty or not saved.");
                return;
            }
            List<StoredPreference> list = JsonConvert.DeserializeObject<List<StoredPreference>>(value);
            foreach (StoredPreference item in list)
            {
                if (!preferences.ContainsKey((item.Key, item.Type)))
                {
                    Main.LogWarning("Unable to load " + item.Key + ", key not registered.");
                }
                else
                {
                    preferences[(item.Key, item.Type)].Deserialize(item.Value);
                }
            }
            list.Clear();
        }

        /// <summary>
        /// Register a preference with this preference manager.
        /// </summary>
        /// <typeparam name="T">the type of the preference.</typeparam>
        /// <param name="preference">the preference to register.</param>
        /// <returns>A reference to the input preference.</returns>
        public T RegisterPreference<T>(T preference) where T : PreferenceBase
        {
            if (preferences.ContainsKey((preference.Key, preference.GetType().Name)))
            {
                Main.LogWarning("Unable to register " + preference.Key + ", key already registered.");
                return null;
            }
            preferences.Add((preference.Key, preference.GetType().Name), preference);
            return preference;
        }
    }
}
