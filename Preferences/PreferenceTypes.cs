using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PreferenceSystem.Preferences
{
    public abstract class PreferenceBase
    {
        public string Key { get; protected set; }

        internal PreferenceBase(string key)
        {
            Key = key;
        }

        public abstract string Serialize();

        public abstract void Deserialize(string json);
    }

    public abstract class PreferenceBase<T> : PreferenceBase
    {
        public T Value { get; protected set; }

        public PreferenceBase(string key, T defaultValue = default(T))
            : base(key)
        {
            Value = defaultValue;
        }

        public void Set(T value)
        {
            Value = value;
        }

        public T Get()
        {
            return Value;
        }
    }
    public class PreferenceBool : PreferenceBase<bool>
    {
        public PreferenceBool(string key, bool defaultValue = false)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return base.Value.ToString();
        }

        public override void Deserialize(string json)
        {
            base.Value = bool.Parse(json);
        }
    }

    public class PreferenceInt : PreferenceBase<int>
    {
        public PreferenceInt(string key, int defaultValue = 0)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return base.Value.ToString();
        }

        public override void Deserialize(string json)
        {
            base.Value = int.Parse(json);
        }
    }

    public class PreferenceFloat : PreferenceBase<float>
    {
        public PreferenceFloat(string key, float defaultValue = 0f)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return base.Value.ToString();
        }

        public override void Deserialize(string json)
        {
            base.Value = float.Parse(json);
        }
    }

    public class PreferenceString : PreferenceBase<string>
    {
        public PreferenceString(string key, string defaultValue = null)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return base.Value.ToString();
        }

        public override void Deserialize(string json)
        {
            base.Value = json;
        }
    }

    public class PreferenceList<T> : PreferenceBase<List<T>>
    {
        public PreferenceList(string key, List<T> defaultValue = null)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return JsonConvert.SerializeObject(base.Value);
        }

        public override void Deserialize(string json)
        {
            base.Value = JsonConvert.DeserializeObject<List<T>>(json);
        }
    }

    public class PreferenceDictionary<T1, T2> : PreferenceBase<Dictionary<T1, T2>>
    {
        public PreferenceDictionary(string key, Dictionary<T1, T2> defaultValue = null)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return JsonConvert.SerializeObject(base.Value);
        }

        public override void Deserialize(string json)
        {
            base.Value = JsonConvert.DeserializeObject<Dictionary<T1, T2>>(json);
        }
    }

    public class PreferenceEnum<T> : PreferenceBase<T> where T : Enum
    {
        public PreferenceEnum(string key, T defaultValue = default)
            : base(key, defaultValue)
        {
        }

        public override string Serialize()
        {
            return JsonConvert.SerializeObject(base.Value);
        }

        public override void Deserialize(string json)
        {
            base.Value = JsonConvert.DeserializeObject<T>(json);
        }
    }
}
