using KitchenLib.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PreferenceSystem
{
    public class PreferenceSet
    {
        public string Name;
        public long CreatedAt;
        public bool ReadOnlyMode;
        public List<PreferenceSystemManagerData> Managers;

        [JsonIgnore]
        public IEnumerable<string> Mods => Managers.Select(x => x.ModName);
        [JsonIgnore]
        private List<string> _modGuids => Managers.Select(x => x.ModGuid).ToList();

        public PreferenceSet()
        {
            Name = string.Empty;
            ReadOnlyMode = false;
        }

        public void Add(PreferenceSystemManagerData data)
        {
            if (Managers == null)
                Managers = new List<PreferenceSystemManagerData>();

            int indexOfData = _modGuids.IndexOf(data.ModGuid);
            if (indexOfData != -1)
            {
                Managers[indexOfData] = data;
            }
            else
            {
                Managers.Add(data);
            }
        }

        public string Preview()
        {
            string result = $"Name: {Name}\nCreated: {DateTimeOffset.FromUnixTimeSeconds(CreatedAt).ToString("yyyy/MM/dd HH:mm:ss")}\nRead Only Mode: {ReadOnlyMode}\n";
            foreach (PreferenceSystemManagerData manager in Managers)
            {
                string managerResult = $"\tMod: {manager.ModName}\n";
                foreach (PreferenceData prefData in manager.Preferences)
                {
                    managerResult += $"\t\t{prefData.Key}: {prefData.Value} ({prefData.TypeKey})\n";
                }
                result += managerResult;
            }
            return result;
        }
    }

    public class PreferenceSystemManagerData
    {
        public string ModGuid;
        public string ModName;
        [JsonIgnore]
        public List<PreferenceData> Preferences => GetPreferences();

        [JsonProperty]
        private List<string> _keys;
        [JsonProperty]
        private List<string> _types;
        [JsonProperty]
        private List<object> _values;

        public void Add(PreferenceData data)
        {
            if (_keys == null)
                _keys = new List<string>();
            if (_types == null)
                _types = new List<string>();
            if (_values == null)
                _values = new List<object>();

            int indexOfData = _keys.IndexOf(data.Key);
            if (indexOfData != -1)
            {
                _types[indexOfData] = data.TypeKey;
                _values[indexOfData] = data.Value;
            }
            else
            {
                _keys.Add(data.Key);
                _types.Add(data.TypeKey);
                _values.Add(data.Value);
            }
        }

        public void Add<T>(string key, T value)
        {
            Add(new PreferenceData(key, value));
        }

        public List<PreferenceData> GetPreferences()
        {
            List<PreferenceData> data = new List<PreferenceData>();
            for (int i = 0; i < _keys.Count; i++)
            {
                data.Add(new PreferenceData(_keys[i], _types[i], _values[i]));
            }
            return data;
        }
    }

    public class PreferenceData
    {
        public string Key;
        public string TypeKey;
        public object Value;

        public Type ValueType
        {
            get
            {
                if (!PreferenceSystemManager.AllowedTypesDict.TryGetValue(TypeKey, out Type type))
                {
                    return null;
                }
                return type;
            }
        }

        public PreferenceData(string key, object value)
        {
            Key = key;
            TypeKey = value.GetType().FullName;
            Value = value;
        }

        public PreferenceData(string key, Type valueType, object value)
        {
            Key = key;
            TypeKey = valueType.FullName;
            Value = value;
        }

        internal PreferenceData(string key, string typeKey, object value)
        {
            Key = key;
            TypeKey = typeKey;
            Value = value;
        }
    }

    internal static class PreferenceSystemRegistry
    {
        public static readonly string PREFERENCE_SETS_FOLDER_PATH = Application.persistentDataPath + "/PreferenceSystem/PreferenceSets";

        private static Dictionary<string, PreferenceSet> _preferenceSetsCache = new Dictionary<string, PreferenceSet>();
        private static Dictionary<string, string> _loadedPreferenceSetFilepaths = new Dictionary<string, string>();

        private struct RegisteredManager
        {
            public readonly string GUID;
            public readonly string Name;
            private readonly PreferenceSystemManager Manager;

            public RegisteredManager(string guid, string name, PreferenceSystemManager manager)
            {
                GUID = guid;
                Name = name;
                Manager = manager;
            }

            public PreferenceSystemManagerData GetData()
            {
                return Manager.GetData();
            }

            public bool LoadPreferences(string preferenceSetName, PreferenceSystemManagerData managerData)
            {
                return Manager.LoadData(preferenceSetName, managerData);
            }
        }

        private static Dictionary<string, RegisteredManager> _registeredPrefManagers = new Dictionary<string, RegisteredManager>();

        public static Dictionary<string, string> RegisteredMods => GetRegisteredMods();

        public static void Add(PreferenceSystemManager manager)
        {
            if (_registeredPrefManagers.ContainsKey(manager.MOD_GUID))
            {
                Debug.LogWarning($"[PreferenceSystem] PreferenceSystemManager for {manager.MOD_GUID} already registered! Skipping add to registry.");
                return;
            }
            _registeredPrefManagers.Add(manager.MOD_GUID, new RegisteredManager(manager.MOD_GUID, manager.MOD_NAME, manager));
        }

        public static bool Export(string preferenceSetName, bool readOnlyMode, out PreferenceSet preferenceSet, out string statusMessage, string[] guids = null)
        {
            if (!Directory.Exists($"{PREFERENCE_SETS_FOLDER_PATH}"))
                Directory.CreateDirectory($"{PREFERENCE_SETS_FOLDER_PATH}");

            string filename = GetPreferenceSetFileName(preferenceSetName, DateTime.UtcNow);
            string filepath = $"{PREFERENCE_SETS_FOLDER_PATH}/{filename}";
            if (File.Exists(filepath))
            {
                preferenceSet = default;
                statusMessage = $"{filename} is used.";
                return false;
            }

            if (!TryConvert(preferenceSetName, guids, readOnlyMode, out preferenceSet, out statusMessage))
            {
                return false;
            }

            string json = Serialize(preferenceSet);
            string base64 = Utils.StringUtils.ToBase64(json);
            File.WriteAllText(filepath, base64);
            statusMessage = $"Preference Set exported to {filepath}";
            InitPreferenceSets();
            return true;
        }

        public static bool Import(string base64, out string statusMessage, string nameOverride = null)
        {
            if (!Directory.Exists($"{PREFERENCE_SETS_FOLDER_PATH}"))
                Directory.CreateDirectory($"{PREFERENCE_SETS_FOLDER_PATH}");

            string json;
            try
            {
                json = Utils.StringUtils.FromBase64(base64);
            }
            catch (Exception ex)
            {
                Main.LogError($"{ex.Message}\n{ex.StackTrace}");
                statusMessage = "Invalid Base64 string! Check that all the text is correctly copied.";
                return false;
            }

            if (!Deserialize(json, out PreferenceSet preferenceSet))
            {
                statusMessage = "Invalid data contained in import! Check that all the text is correctly copied.";
                return false;
            }

            if (!nameOverride.IsNullOrEmpty())
            {
                preferenceSet.Name = nameOverride;
                preferenceSet.CreatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                json = Serialize(preferenceSet);
                base64 = Utils.StringUtils.ToBase64(json);
            }


            string filename = GetPreferenceSetFileName(preferenceSet.Name, DateTimeOffset.FromUnixTimeSeconds(preferenceSet.CreatedAt));
            string filepath = $"{PREFERENCE_SETS_FOLDER_PATH}/{filename}";

            if (File.Exists(filepath))
            {
                statusMessage = $"{filename} is used. If this preference set has not already been imported, please provide a new preference set name.";
                return false;
            }

            File.WriteAllText(filepath, base64);
            statusMessage = $"Preference Set imported and saved to {filepath}";
            InitPreferenceSets();
            return true;
        }

        public static string GetPreferenceSetFileName(string preferenceSetName, DateTimeOffset dateTime)
        {
            return $"{preferenceSetName}.txt";
        }

        public static bool TryConvert(string preferenceSetName, string[] guids, bool readOnlyMode, out PreferenceSet preferenceSet, out string statusMessage)
        {
            preferenceSet = default;
            if (preferenceSetName.IsNullOrEmpty())
            {
                statusMessage = $"Preference Set Name cannot be empty.";
                return false;
            }
            if (guids.IsNullOrEmpty())
            {
                statusMessage = "At least one mod must be selected.";
                return false;
            }


            preferenceSet = new PreferenceSet()
            {
                Name = preferenceSetName,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ReadOnlyMode = readOnlyMode
            };
            foreach(string guid in guids)
            {
                if (!_registeredPrefManagers.TryGetValue(guid, out RegisteredManager manager))
                {
                    preferenceSet = default;
                    statusMessage = $"{guid} not found!";
                    return false;
                }
                preferenceSet.Add(manager.GetData());
            }
            statusMessage = "Successfully converted";
            return true;
        }

        public static string Serialize(PreferenceSet preferenceSet)
        {
            return JsonConvert.SerializeObject(preferenceSet, Formatting.Indented);
        }

        public static bool Deserialize(string json, out PreferenceSet preferenceSet)
        {
            preferenceSet = default;
            try
            {
                preferenceSet = JsonConvert.DeserializeObject<PreferenceSet>(json);
                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"{ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public static void InitPreferenceSets()
        {
            if (!Directory.Exists($"{PREFERENCE_SETS_FOLDER_PATH}"))
                Directory.CreateDirectory($"{PREFERENCE_SETS_FOLDER_PATH}");
            _preferenceSetsCache.Clear();
            _loadedPreferenceSetFilepaths.Clear();
            foreach (string filepath in Directory.GetFiles(PREFERENCE_SETS_FOLDER_PATH))
            {
                string base64 = File.ReadAllText(filepath);
                string json;
                try
                {
                    json = Utils.StringUtils.FromBase64(base64);
                    if (Deserialize(json, out PreferenceSet preferenceSet))
                    {
                        _preferenceSetsCache.Add(preferenceSet.Name, preferenceSet);
                        _loadedPreferenceSetFilepaths.Add(preferenceSet.Name, filepath);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        public static Dictionary<string, PreferenceSet> GetCachedPreferenceSets()
        {
            return new Dictionary<string, PreferenceSet>(_preferenceSetsCache);
        }

        public static IEnumerable<string> GetCachedPreferenceSetNames()
        {
            return _preferenceSetsCache.Keys;
        }

        public static string Preview(PreferenceSet preferenceSet)
        {
            return preferenceSet.Preview();
        }

        public static string Preview(string preferenceSetName)
        {
            if (!_preferenceSetsCache.TryGetValue(preferenceSetName, out PreferenceSet set))
                return string.Empty;
            return Preview(set);
        }

        public static void Load(string preferenceSetName)
        {
            if (_preferenceSetsCache.TryGetValue(preferenceSetName, out PreferenceSet preferenceSet))
            {
                foreach (PreferenceSystemManagerData managerData in preferenceSet.Managers)
                {
                    LoadManagerData(preferenceSet.Name, managerData);
                }
            }
        }
        
        private static bool LoadManagerData(string preferenceSetName, PreferenceSystemManagerData managerData)
        {
            if (!_registeredPrefManagers.TryGetValue(managerData.ModGuid, out RegisteredManager manager))
            {
                return false;
            }
            return manager.LoadPreferences(preferenceSetName, managerData);
        }

        internal static bool Delete(string preferenceSetName)
        {
            if (!_loadedPreferenceSetFilepaths.TryGetValue(preferenceSetName, out string filepath))
            {
                return false;
            }

            try
            {
                File.Delete(filepath);
                InitPreferenceSets();
                return true;
            }
            catch (Exception ex)
            {
                Main.LogError($"{ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public static Dictionary<string, string> GetRegisteredMods()
        {
            return _registeredPrefManagers.ToDictionary(x => x.Key, x => x.Value.Name);
        }
    }
}
