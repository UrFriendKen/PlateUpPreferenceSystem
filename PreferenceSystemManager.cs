﻿using Kitchen;
using Kitchen.Modules;
using PreferenceSystem.Event;
using PreferenceSystem.Menus;
using PreferenceSystem.Preferences;
using PreferenceSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;
using static PreferenceSystem.Utils.TypeUtils;

namespace PreferenceSystem
{
    public class PreferenceSystemManager
    {
        public enum MenuType
        {
            MainMenu,
            PauseMenu
        }

        public readonly string MOD_GUID;
        public readonly string MOD_NAME;

        private bool _isPreferencesEventsRegistered = false;
        public bool IsPreferencesEventsRegistered
        {
            get { return _isPreferencesEventsRegistered; }
        }

        AssemblyBuilder _assemblyBuilder;
        ModuleBuilder _moduleBuilder;

        private static readonly Regex sWhitespace = new Regex(@"\s+");

        private PreferenceManager _prefManager;

        private Dictionary<string, Type> _registeredPreferences = new Dictionary<string, Type>();
        private Dictionary<string, Action<bool>> boolPreferencesOnChanged = new Dictionary<string, Action<bool>>();
        private Dictionary<string, Action<int>> intPreferencesOnChanged = new Dictionary<string, Action<int>>();
        private Dictionary<string, Action<float>> floatPreferencesOnChanged = new Dictionary<string, Action<float>>();
        private Dictionary<string, Action<string>> stringPreferencesOnChanged = new Dictionary<string, Action<string>>();

        private Dictionary<string, object> _defaultValues;
        public Dictionary<string, object> Defaults => new Dictionary<string, object>(_defaultValues);

        public static Type[] AllowedTypes => new Type[]
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string)
        };

        public static Dictionary<string, Type> AllowedTypesDict => AllowedTypes.ToDictionary(x => x.FullName, x => x);

        private bool _menuRegistered = false;
        //private bool _mainMenuRegistered = false;
        //private bool _pauseMenuRegistered = false;
        private Type _topLevelTypeKey;
        //private Type _mainTopLevelTypeKey;
        //private Type _pauseTopLevelTypeKey;
        private Queue<Type> _menuTypeKeys = new Queue<Type>();
        //private Queue<Type> _mainMenuTypeKeys = new Queue<Type>();
        //private Queue<Type> _pauseMenuTypeKeys = new Queue<Type>();
        private Queue<List<(ElementType, object)>> _completedElements = new Queue<List<(ElementType, object)>>();
        private Stack<Type> _tempMenuTypeKeys = new Stack<Type>();
        //private Stack<Type> _tempMainMenuTypeKeys = new Stack<Type>();
        //private Stack<Type> _tempPauseMenuTypeKeys = new Stack<Type>();
        private Stack<List<(ElementType, object)>> _elements = new Stack<List<(ElementType, object)>>();
        private Stack<int> _conditionalBlockers = new Stack<int>();

        internal enum ElementType
        {
            Label,
            Info,
            Select,
            Button,
            ButtonWithConfirm,
            PlayerRow,
            SubmenuButton,
            ProfileSelector,
            DeleteProfileButton,
            BoolOption,
            IntOption,
            FloatOption,
            StringOption,
            Spacer,
            ConditionalBlocker,
            ConditionalBlockerDone,
            ActionButton,
            PageSelector,
            PagedItem,
            PagedItemDone
        }

        internal bool IsSelectableElement(ElementType elementType)
        {
            switch (elementType)
            {
                case ElementType.Select:
                case ElementType.Button:
                case ElementType.ButtonWithConfirm:
                case ElementType.PlayerRow:
                case ElementType.SubmenuButton:
                case ElementType.ProfileSelector:
                case ElementType.DeleteProfileButton:
                case ElementType.BoolOption:
                case ElementType.IntOption:
                case ElementType.FloatOption:
                case ElementType.StringOption:
                case ElementType.ActionButton:
                case ElementType.PageSelector:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Create PreferenceSystemManager instance.
        /// </summary>
        /// <param name="modGUID">Unique mod identifier</param>
        /// <param name="modName">Name displayed on mod menu button</param>
        public PreferenceSystemManager(string modGUID, string modName)
        {
            MOD_GUID = modGUID;
            MOD_NAME = modName;
            _prefManager = new PreferenceManager(modGUID);

            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.{MOD_GUID}"), AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Module");

            _topLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}");
            //_mainTopLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_Main");
            //_pauseTopLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_Pause");
            _elements.Push(new List<(ElementType, object)>());
            _conditionalBlockers.Push(0);

            _completedElements = new Queue<List<(ElementType, object)>>();

            PreferenceSystemRegistry.Add(this);
        }

        private static bool IsAllowedType(Type type, bool throwExceptionIfNotAllowed = false)
        {
            if (!type.IsEnum &&
                !AllowedTypes.Contains(type))
            {
                if (throwExceptionIfNotAllowed)
                    ThrowTypeException();
                return false;
            }
            return true;
        }

        private static void ThrowTypeException()
        {
            string allowedTypesStr = "";
            for (int i = 0; i < AllowedTypes.Length; i++)
            {
                allowedTypesStr += AllowedTypes[i].ToString();
                if (i != AllowedTypes.Length - 1) allowedTypesStr += ", ";
            }
            throw new ArgumentException($"Type TPref is not supported! Only use enums, {allowedTypesStr}.");
        }

        private bool IsUsedKey(string key, bool throwExceptionIfUsed = false)
        {
            if (_registeredPreferences.ContainsKey(key))
            {
                if (throwExceptionIfUsed)
                    ThrowKeyException(key);
                return true;
            }
            return false;
        }

        private static void ThrowKeyException(string key)
        {
            throw new ArgumentException($"Key {key} already exists!");
        }

        private void Preference_OnChanged<T>(string key, T value)
        {
            Set(key, value);
        }

        /// <summary>
        /// Create selector with a linked preference
        /// </summary>
        /// <typeparam name="T">Type of preference value</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <param name="initialValue">Starting value when preference is created for the first time</param>
        /// <param name="values">Array of allowed values</param>
        /// <param name="strings">Array of string representations for each value</param>
        /// <returns>Instance of PreferenceSystemManager (for method chaining)</returns>
        public PreferenceSystemManager AddOption<T>(string key, T initialValue, T[] values, string[] strings)
        {
            return PrivateAddOption<T>(key, initialValue, values, strings, false, null);
        }

        /// <summary>
        /// Create selector with a linked preference
        /// </summary>
        /// <typeparam name="T">Type of preference value</typeparam>
        /// <param name="key">Unique identifier</param>
        /// <param name="initialValue">Starting value when preference is created for the first time</param>
        /// <param name="values">Array of allowed values</param>
        /// <param name="strings">Array of string representations for each value</param>
        /// <param name="on_changed">Callback when selector value is changed</param>
        /// <returns>Instance of PreferenceSystemManager (for method chaining)</returns>
        public PreferenceSystemManager AddOption<T>(string key, T initialValue, T[] values, string[] strings, Action<T> on_changed)
        {
            return PrivateAddOption<T>(key, initialValue, values, strings, false, on_changed);
        }

        /// <summary>
        /// Create selector with a linked preference
        /// </summary>
        /// <typeparam name="T">Type of preference value</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <param name="initialValue">Starting value when preference is created for the first time</param>
        /// <param name="values">Array of allowed values</param>
        /// <param name="strings">Array of string representations for each value</param>
        /// <param name="redraw">Redraw menu when selector value is changed</param>
        /// <returns>Instance of PreferenceSystemManager (for method chaining)</returns>
        public PreferenceSystemManager AddOption<T>(string key, T initialValue, T[] values, string[] strings, bool redraw)
        {
            return PrivateAddOption<T>(key, initialValue, values, strings, false, null, redraw);
        }

        /// <summary>
        /// Create selector with a linked preference
        /// </summary>
        /// <typeparam name="T">Type of preference value</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <param name="initialValue">Starting value when preference is created for the first time</param>
        /// <param name="values">Array of allowed values</param>
        /// <param name="strings">Array of string representations for each value</param>
        /// <param name="on_changed">Callback when selector value is changed</param>
        /// <param name="redraw">Redraw menu when selector value is changed</param>
        /// <returns>Instance of PreferenceSystemManager (for method chaining)</returns>
        public PreferenceSystemManager AddOption<T>(string key, T initialValue, T[] values, string[] strings, Action<T> on_changed, bool redraw)
        {
            return PrivateAddOption<T>(key, initialValue, values, strings, false, on_changed, redraw);
        }

        /// <summary>
        /// Create hidden preference which is not displayed in menu
        /// </summary>
        /// <typeparam name="T">Type of preference value</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <param name="initialValue">Starting value when preference is created for the first time</param>
        /// <param name="doLoad">Set true if called after RegisterMenu. Otherwise, false.</param>>
        /// <returns>Instance of PreferenceSystemManager (for method chaining)</returns>
        public PreferenceSystemManager AddProperty<T>(string key, T initialValue, bool doLoad = false)
        {
            PrivateAddOption<T>(key, initialValue, null, null, true, null);
            if (doLoad)
                Load();
            return this;
        }

        private PreferenceSystemManager PrivateAddOption<T>(string key, T initialValue, T[] values, string[] strings, bool doNotShow, Action<T> on_changed, bool redraw = false)
        {
            IsAllowedType(typeof(T), true);
            IsUsedKey(key, true);

            if (typeof(T) == typeof(bool))
            {
                PreferenceBool preference = _prefManager.RegisterPreference(new PreferenceBool(key, ChangeType<bool>(initialValue)));
                if (on_changed != null)
                    boolPreferencesOnChanged[key] = ChangeType<Action<bool>>(on_changed);

                if (!doNotShow)
                {
                    EventHandler<bool> handler = delegate (object _, bool b)
                    {
                        Preference_OnChanged(key, b);
                        if (on_changed != null)
                            on_changed(ChangeType<T>(b));
                    };
                    _elements.Peek().Add((ElementType.BoolOption, new OptionData<bool>(MOD_GUID, key, values.Cast<bool>().ToList(), strings.ToList(), handler, redraw)));
                }
            }
            else if (typeof(T) == typeof(int))
            {
                PreferenceInt preference = _prefManager.RegisterPreference(new PreferenceInt(key, ChangeType<int>(initialValue)));
                if (on_changed != null)
                    intPreferencesOnChanged[key] = ChangeType<Action<int>>(on_changed);

                if (!doNotShow)
                {
                    EventHandler<int> handler = delegate (object _, int i)
                    {
                        Preference_OnChanged(key, i);
                        if (on_changed != null)
                            on_changed(ChangeType<T>(i));
                    };
                    _elements.Peek().Add((ElementType.IntOption, new OptionData<int>(MOD_GUID, key, values.Cast<int>().ToList(), strings.ToList(), handler, redraw)));
                }
            }
            else if (typeof(T) == typeof(float))
            {
                PreferenceFloat preference = _prefManager.RegisterPreference(new PreferenceFloat(key, ChangeType<float>(initialValue)));
                if (on_changed != null)
                    floatPreferencesOnChanged[key] = ChangeType<Action<float>>(on_changed);
                if (!doNotShow)
                {
                    EventHandler<float> handler = delegate (object _, float f)
                    {
                        Preference_OnChanged(key, f);
                        if (on_changed != null)
                            on_changed(ChangeType<T>(f));
                    };
                    _elements.Peek().Add((ElementType.FloatOption, new OptionData<float>(MOD_GUID, key, values.Cast<float>().ToList(), strings.ToList(), handler, redraw)));
                }
            }
            else if (typeof(T) == typeof(string))
            {
                PreferenceString preference = _prefManager.RegisterPreference(new PreferenceString(key, ChangeType<string>(initialValue)));
                if (on_changed != null)
                    stringPreferencesOnChanged[key] = ChangeType<Action<string>>(on_changed);

                if (!doNotShow)
                {
                    EventHandler<string> handler = delegate (object _, string s)
                    {
                        Preference_OnChanged(key, s);
                        if (on_changed != null)
                            on_changed(ChangeType<T>(s));
                    };
                    _elements.Peek().Add((ElementType.StringOption, new OptionData<string>(MOD_GUID, key, values.Cast<string>().ToList(), strings.ToList(), handler, redraw)));
                }
            }
            else if (typeof(T).IsEnum)
            {
                PreferenceString preference = _prefManager.RegisterPreference(new PreferenceString(key, initialValue.ToString()));
                if (on_changed != null)
                    stringPreferencesOnChanged[key] = (string s) =>
                    {
                        on_changed(ChangeType<T>(s));
                    }; 

                if (!doNotShow)
                {
                    EventHandler<string> handler = delegate (object _, string s)
                    {
                        Preference_OnChanged(key, s);
                        if (on_changed != null)
                            on_changed(ChangeType<T>(s));
                    };
                    _elements.Peek().Add((ElementType.StringOption, new OptionData<string>(MOD_GUID, key, values.Select(x => x.ToString()).ToList(), strings.ToList(), handler, redraw)));
                }
            }
            _registeredPreferences.Add(key, typeof(T));
            return this;
        }

        /// <summary>
        /// Retrieve preference value
        /// </summary>
        /// <typeparam name="T">Type of preference value.</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <returns>Value of preference. If preference does not exist, default is returned instead.</returns>
        public T Get<T>(string key)
        {
            return (T)(Get(key, typeof(T)) ?? default(T));
        }

        /// <summary>
        /// Try to retrieve preference value
        /// </summary>
        /// <typeparam name="T">Type of preference value.</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <param name="value">Value of preference, if it exists. Otherwise, default</param>
        /// <returns>True if value successfully retrieved, otherwise false. Value of preference. If preference does not exist, default is returned instead.</returns>
        public bool TryGet<T>(string key, out T value)
        {
            if (!Has<T>(key))
            {
                value = default;
                return false;
            }
            value = Get<T>(key);
            return true;
        }

        /// <summary>
        /// Check if prference key is registered
        /// </summary>
        /// <typeparam name="T">Type of preference value.</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <returns></returns>
        public bool Has<T>(string key)
        {
            return Has(key, typeof(T));
        }

        private bool Has(string key, Type valueType)
        {
            IsAllowedType(valueType, true);
            
            if (valueType == typeof(bool))
                return _prefManager.HasPreference<PreferenceBool>(key);
            else if (valueType == typeof(int))
                return _prefManager.HasPreference<PreferenceInt>(key);
            else if (valueType == typeof(float))
                return _prefManager.HasPreference<PreferenceFloat>(key);
            else if (valueType == typeof(string))
                return _prefManager.HasPreference<PreferenceString>(key);
            else if (valueType.IsEnum)
                return _prefManager.HasPreference<PreferenceString>(key);
            return false;
        }

        private object Get(string key, Type valueType)
        {
            IsAllowedType(valueType, true);

            object value = null;
            if (valueType == typeof(bool))
            {
                value = _prefManager.GetPreference<PreferenceBool>(key)?.Get();
            }
            else if (valueType == typeof(int))
            {
                value = _prefManager.GetPreference<PreferenceInt>(key)?.Get();
            }
            else if (valueType == typeof(float))
            {
                value = _prefManager.GetPreference<PreferenceFloat>(key)?.Get();
            }
            else if (valueType == typeof(string))
            {
                value = _prefManager.GetPreference<PreferenceString>(key)?.Get();
            }
            else if (valueType.IsEnum)
            {
                value = _prefManager.GetPreference<PreferenceString>(key)?.Get();
                if (value != null)
                {
                    try
                    {
                        value = Enum.Parse(valueType, value.ToString());
                    }
                    catch
                    {
                        Main.LogError($"Failed to parse {value} as {valueType}");
                        value = null;
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// Set preference value
        /// </summary>
        /// <typeparam name="T">Type of preference value.</typeparam>
        /// <param name="key">Unique preference identifier</param>
        /// <param name="value">Value to be applied</param>
        public void Set<T>(string key, T value)
        {
            Set(key, typeof(T), value);
        }

        private void Set(string key, Type valueType , object value)
        {
            IsAllowedType(valueType, true);

            if (valueType == typeof(bool))
            {
                bool b = ChangeType<bool>(value);
                _prefManager.GetPreference<PreferenceBool>(key)?.Set(b);
                if (boolPreferencesOnChanged.TryGetValue(key, out var on_changed))
                    on_changed(b);
            }
            else if (valueType == typeof(int))
            {
                int i = ChangeType<int>(value);
                _prefManager.GetPreference<PreferenceInt>(key)?.Set(i);
                if (intPreferencesOnChanged.TryGetValue(key, out var on_changed))
                    on_changed(i);
            }
            else if (valueType == typeof(float))
            {
                float f = ChangeType<float>(value);
                _prefManager.GetPreference<PreferenceFloat>(key)?.Set(f);
                if (floatPreferencesOnChanged.TryGetValue(key, out var on_changed))
                    on_changed(f);
            }
            else if (valueType == typeof(string))
            {
                string s = ChangeType<string>(value);
                _prefManager.GetPreference<PreferenceString>(key)?.Set(s);
                if (stringPreferencesOnChanged.TryGetValue(key, out var on_changed))
                    on_changed(s);
            }
            else if (valueType.IsEnum)
            {
                string s = value.ToString();
                _prefManager.GetPreference<PreferenceString>(key)?.Set(s);
                if (stringPreferencesOnChanged.TryGetValue(key, out var on_changed))
                    on_changed(s);
            }
            Save();
        }

        /// <summary>
        /// Change current preference profile and loads preference values. If profile name does not exist, a new preference profile is created.
        /// </summary>
        /// <param name="profileName">Profile name</param>
        public void SetProfile(string profileName)
        {
            if (!GlobalPreferences.DoesProfileExist(MOD_GUID, profileName))
            {
                GlobalPreferences.AddProfile(MOD_GUID, profileName);
            }
            GlobalPreferences.SetProfile(MOD_GUID, profileName);
            _prefManager.SetProfile(profileName);
            _prefManager.Load();
            _prefManager.Save();
        }

        /// <summary>
        /// Try to change current preference profile and loads preference values, if profile exists.
        /// </summary>
        /// <param name="profileName">Profile name</param>
        /// <returns>True if profile was changed. Otherwise, false</returns>
        public bool TrySetProfile(string profileName)
        {
            if (!GlobalPreferences.DoesProfileExist(MOD_GUID, profileName))
            {
                return false;
            }
            SetProfile(profileName);
            return true;
        }

        private void Save()
        {
            _prefManager.Save();
        }

        private void Load()
        {
            _prefManager.Load();
        }

        internal PreferenceSystemManagerData GetData()
        {
            PreferenceSystemManagerData result = new PreferenceSystemManagerData()
            {
                ModGuid = MOD_GUID,
                ModName = MOD_NAME
            };
            foreach (KeyValuePair<string, Type> pref in _registeredPreferences)
            {
                result.Add(pref.Key, Get(pref.Key, pref.Value));
            }
            return result;
        }

        internal bool LoadData(string preferenceSetName, PreferenceSystemManagerData data)
        {
            SetProfile($"{preferenceSetName}_Loaded");
            foreach (PreferenceData prefData in data.Preferences)
            {
                try
                {
                    Set(prefData.Key, prefData.ValueType, prefData.Value);
                }
                catch (Exception ex)
                {
                    Main.LogError($"{ex.Message}\n{ex.StackTrace}");
                    return false;
                }
            }
            return true;
        }

        internal bool IsPreferencesEqual(PreferenceSystemManagerData data)
        {
            foreach (PreferenceData prefData in data.Preferences)
            {
                object value = Get(prefData.Key, prefData.ValueType);
                if (value == null)
                    continue;

                if (prefData.Value != value)
                    return false;
            }
            return true;
        }

        private readonly struct LabelData
        {
            public readonly string Text;
            public LabelData(string text)
            {
                Text = text;
            }
        }
        private readonly struct InfoData
        {
            public readonly string Text;
            public InfoData(string text)
            {
                Text = text;
            }
        }
        private readonly struct SelectData
        {
            public readonly List<string> Options;
            public readonly Action<int> OnActivate;
            public readonly int Index;
            public readonly bool Redraw;
            public SelectData(List<string> options, Action<int> on_activate, int index = 0, bool redraw = false)
            {
                Options = options;
                OnActivate = on_activate;
                Index = index;
                Redraw = redraw;
            }
        }
        private readonly struct ButtonData
        {
            public readonly string ButtonText;
            public readonly Action<int> OnActivate;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public readonly bool CloseOnPress;
            public ButtonData(string button_text, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                ButtonText = button_text;
                OnActivate = on_activate;
                Arg = arg;
                Scale = scale;
                Padding = padding;
            }
            public ButtonData(string button_text, Action<int> on_activate, bool closeOnPress = false, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                ButtonText = button_text;
                OnActivate = on_activate;
                Arg = arg;
                Scale = scale;
                Padding = padding;
                CloseOnPress = closeOnPress;
            }
        }
        private readonly struct ButtonWithConfirmData
        {
            public readonly string ButtonText;
            public readonly string InfoText;
            public readonly Action<GenericChoiceDecision> Callback;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public ButtonWithConfirmData(string button_text, string info_text, Action<GenericChoiceDecision> callback, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                ButtonText = button_text;
                InfoText = info_text;
                Callback = callback;
                Arg = arg;
                Scale = scale;
                Padding = padding;
            }
        }
        private readonly struct PlayerRowData
        {
            public readonly string Username;
            public readonly PlayerInfo Player;
            public readonly Action<int> OnKick;
            public readonly Action<int> OnRemove;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public PlayerRowData(string username, PlayerInfo player, Action<int> on_kick, Action<int> on_remove, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                Username = username;
                Player = player;
                OnKick = on_kick;
                OnRemove = on_remove;
                Arg = arg;
                Scale = scale;
                Padding = padding;
            }
        }
        private readonly struct SubmenuButtonData
        {
            public readonly string ButtonText;
            public readonly Type MenuKey;
            //public readonly Type MainMenuKey;
            //public readonly Type PauseMenuKey;
            public readonly bool SkipStack;
            public SubmenuButtonData(string button_text, Type menu_key, bool skip_stack = false)
            {
                ButtonText = button_text;
                MenuKey = menu_key;
                SkipStack = skip_stack;
            }
        }
        private readonly struct ActionButtonData
        {
            public readonly Type MenuType;
            public readonly string ButtonText;
            private readonly MenuAction MenuAction;
            public object Action => (MenuType == typeof(MenuAction)) ? MenuAction.Action : ((MenuType == typeof(MenuAction)) ? MenuAction.PauseAction : null);
            public ActionButtonData(string button_text, object action)
            {
                ButtonText = button_text;
                MenuType = action.GetType();
                if (action is MainMenuAction mainMenuAction)
                {
                    MenuAction = new MenuAction(mainMenuAction);
                }
                else if (action is PauseMenuAction pauseMenuAction)
                {
                    MenuAction = new MenuAction(pauseMenuAction);
                }
                else
                {
                    throw new Exception("ActionButton action type must be a MainMenuAction or PauseMenuAction!");
                }
            }
        }
        private readonly struct OptionData<T>
        {
            public readonly string ModGUID;
            public readonly string Key;
            public readonly List<T> Values;
            public readonly List<string> Strings;
            public readonly EventHandler<T> EventHandler;
            public readonly bool Redraw;

            public OptionData(string modGuid, string key, List<T> values, List<string> strings, EventHandler<T> eventHandler, bool redraw)
            {
                ModGUID = modGuid;
                Key = key;
                Values = values;
                Strings = strings;
                EventHandler = eventHandler;
                Redraw = redraw;
            }
        }
        private readonly struct DeleteProfileButtonData
        {
            public readonly string ButtonText;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public DeleteProfileButtonData(string button_text, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                ButtonText = button_text;
                Arg = arg;
                Scale = scale;
                Padding = padding;
            }
        }
        private readonly struct ConditionalBlockerData
        {
            public readonly Func<bool> ShouldBlock;
            public ConditionalBlockerData(Func<bool> shouldBlock)
            {
                ShouldBlock = shouldBlock;
            }
        }

        private readonly struct PageSelectorData
        {
            public readonly int MaxItemsPerPage;

            public PageSelectorData(int maxItemsPerPage)
            {
                MaxItemsPerPage = maxItemsPerPage;
            }
        }

        public PreferenceSystemManager AddLabel(string text)
        {
            _elements.Peek().Add((ElementType.Label, new LabelData(text)));
            return this;
        }

        public PreferenceSystemManager AddInfo(string text)
        {
            _elements.Peek().Add((ElementType.Info, new InfoData(text)));
            return this;
        }

        public PreferenceSystemManager AddSelect(List<string> options, Action<int> on_activate, int index = 0, bool redraw = false)
        {
            _elements.Peek().Add((ElementType.Select, new SelectData(options, on_activate, index, redraw)));
            return this;
        }

        public PreferenceSystemManager AddButton(string button_text, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.Button, new ButtonData(button_text, on_activate, arg, scale, padding)));
            return this;
        }

        public PreferenceSystemManager AddButton(string button_text, Action<int> on_activate, bool closeOnPress, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.Button, new ButtonData(button_text, on_activate, closeOnPress, arg, scale, padding)));
            return this;
        }

        public PreferenceSystemManager AddButtonWithConfirm(string button_text, string info_text, Action<GenericChoiceDecision> callback, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.ButtonWithConfirm, new ButtonWithConfirmData(button_text, info_text, callback, arg, scale, padding)));
            return this;
        }

        public PreferenceSystemManager AddPlayerRow(string username, PlayerInfo player, Action<int> on_kick, Action<int> on_remove, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.PlayerRow, new PlayerRowData(username, player, on_kick, on_remove, arg, scale, padding)));
            return this;
        }

        public PreferenceSystemManager AddSubmenu(string button_text, string submenu_key, bool skip_stack = false)
        {
            Type typeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_{sWhitespace.Replace(submenu_key, "")}");
            if (_menuTypeKeys.Contains(typeKey) || _tempMenuTypeKeys.Contains(typeKey))
            {
                throw new ArgumentException("Submenu key already exists!");
            }
            _tempMenuTypeKeys.Push(typeKey);
            _elements.Peek().Add((ElementType.SubmenuButton, new SubmenuButtonData(button_text, typeKey, skip_stack)));
            _elements.Push(new List<(ElementType, object)>());
            _conditionalBlockers.Push(0);
            return this;
        }

        public PreferenceSystemManager AddSelfRegisteredSubmenu<TMenu>(string button_text, bool skip_stack = false) where TMenu : Menu<MenuAction>
        {
            _elements.Peek().Add((ElementType.SubmenuButton, new SubmenuButtonData(button_text, typeof(TMenu), skip_stack)));
            return this;
        }

        public PreferenceSystemManager AddActionButton(string button_text, PauseMenuAction action)
        {
            _elements.Peek().Add((ElementType.ActionButton, new ActionButtonData(button_text, action)));
            return this;
        }
        public PreferenceSystemManager AddActionButton(string button_text, MainMenuAction action)
        {
            _elements.Peek().Add((ElementType.ActionButton, new ActionButtonData(button_text, action)));
            return this;
        }

        public PreferenceSystemManager AddProfileSelector()
        {
            _elements.Peek().Add((ElementType.ProfileSelector, null));
            return this;
        }

        public PreferenceSystemManager AddDeleteProfileButton(string button_text = "Delete Profile", int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.DeleteProfileButton, new DeleteProfileButtonData(button_text, arg, scale, padding)));
            return this;
        }

        public PreferenceSystemManager AddResetPreferencesButton(string button_text, Action onReset = null, bool requireConfirm = true, string confirmInfoText = "Are you sure you want to reset all preferences?", int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            Action<int> onChanged = (int _) =>
            {
                ResetToDefault();
                if (onReset != null)
                    onReset();
            };

            if (requireConfirm)
            {
                AddButtonWithConfirm(button_text, confirmInfoText, (GenericChoiceDecision gcd) =>
                {
                    switch (gcd)
                    {
                        case GenericChoiceDecision.Accept:
                            onChanged(arg);
                            break;
                        default:
                            break;
                    }
                }, arg, scale, padding);
            }
            else
            {
                AddButton(button_text, onChanged, arg, scale, padding);
            }
            return this;
        }

        public PreferenceSystemManager AddSpacer()
        {
            _elements.Peek().Add((ElementType.Spacer, null));
            return this;
        }

        public PreferenceSystemManager SubmenuDone()
        {
            if (_elements.Count < 2)
            {
                throw new Exception("Submenu depth already at highest level.");
            }
            CompletedSubmenuTransfer();
            return this;
        }

        public PreferenceSystemManager AddConditionalBlocker(Func<bool> shouldBlock)
        {
            _elements.Peek().Add((ElementType.ConditionalBlocker, new ConditionalBlockerData(shouldBlock)));
            _conditionalBlockers.Push(_conditionalBlockers.Pop() + 1);
            return this;
        }

        public PreferenceSystemManager ConditionalBlockerDone()
        {
            if (_conditionalBlockers.Peek() < 1)
            {
                throw new Exception("No conditional blockers to terminate.");
            }
            int conditionalBlockerLevel = 1;
            int pagedItemLevel = 0;
            bool shouldThrowException = false;
            foreach (ElementType elementType in _elements.Peek().Select(x => x.Item1).Reverse())
            {
                bool shouldBreak = false;
                switch (elementType)
                {
                    case ElementType.ConditionalBlocker:
                        if (conditionalBlockerLevel == 1)
                        {
                            shouldThrowException = pagedItemLevel != 0;
                            shouldBreak = true;
                        }
                        else
                            conditionalBlockerLevel--;
                        break;
                    case ElementType.ConditionalBlockerDone:
                        conditionalBlockerLevel++;
                        break;
                    case ElementType.PagedItem:
                        pagedItemLevel++;
                        break;
                    case ElementType.PagedItemDone:
                        pagedItemLevel--;
                        break;
                }
                if (shouldBreak)
                {
                    break;
                }
            }
            if (shouldThrowException)
            {
                throw new Exception("Conditional blocker must not cross paged item boundary!");
            }
            _conditionalBlockers.Push(_conditionalBlockers.Pop() - 1);
            _elements.Peek().Add((ElementType.ConditionalBlockerDone, null));
            return this;
        }

        public PreferenceSystemManager AddPageSelector(int maxItemsPerPage)
        {
            foreach (ElementType elementType in _elements.Peek().Select(x => x.Item1))
            {
                if (IsSelectableElement(elementType))
                    throw new Exception("Page selector must be the first selectable element!");
            }
            _elements.Peek().Add((ElementType.PageSelector, new PageSelectorData(maxItemsPerPage)));
            return this;
        }

        public PreferenceSystemManager StartPagedItem()
        {
            if (!_elements.Peek().Select(x => x.Item1).Contains(ElementType.PageSelector))
            {
                throw new Exception("Cannot start paged item! PageSelector must be added first.");
            }
            bool shouldThrowException = false;
            foreach (ElementType elementType in _elements.Peek().Select(x => x.Item1).Reverse())
            {
                bool isStartPagedItem = false;
                bool shouldBreak = false;
                switch (elementType)
                {
                    case ElementType.PagedItem:
                        isStartPagedItem = true;
                        shouldBreak = true;
                        break;
                    case ElementType.PagedItemDone:
                    case ElementType.PageSelector:
                        shouldBreak = true;
                        break;
                }
                if (shouldBreak)
                {
                    if (isStartPagedItem)
                        shouldThrowException = true;
                    break;
                }
            }
            if (shouldThrowException)
            {
                throw new Exception("Cannot start paged item! Terminate the previous paged item first.");
            }
            _elements.Peek().Add((ElementType.PagedItem, null));
            return this;
        }

        public PreferenceSystemManager PagedItemDone()
        {
            bool shouldThrowException = true;
            foreach (ElementType elementType in _elements.Peek().Select(x => x.Item1).Reverse())
            {
                bool isStartPagedItem = false;
                bool shouldBreak = false;
                switch (elementType)
                {
                    case ElementType.PagedItem:
                        isStartPagedItem = true;
                        shouldBreak = true;
                        break;
                    case ElementType.PagedItemDone:
                    case ElementType.PageSelector:
                        shouldBreak = true;
                        break;
                }
                if (shouldBreak)
                {
                    if (isStartPagedItem)
                        shouldThrowException = false;
                    break;
                }
            }
            if (shouldThrowException)
            {
                throw new Exception("No paged item to terminate.");
            }
            _elements.Peek().Add((ElementType.PagedItemDone, null));
            return this;
        }

        public void ResetToDefault()
        {
            if (_defaultValues == null)
                return;

            foreach (KeyValuePair<string, Type> pref in _registeredPreferences)
            {
                Set(pref.Key, pref.Value, _defaultValues[pref.Key]);
            }
        }

        private void PopulateDefaults()
        {
            if (_defaultValues == null)
            {
                _defaultValues = new Dictionary<string, object>();
                foreach (KeyValuePair<string, Type> pref in _registeredPreferences)
                {
                    _defaultValues.Add(pref.Key, Get(pref.Key, pref.Value));
                }
            }
        }

        private Type CreateTypeKey(string typeName)
        {
            //Creating dummy types to use as keys for submenu instances when registering the submenus to keys in CreateSubmenusEvent
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public);
            Type type = typeBuilder.CreateType();
            return type;
        }

        private void CompletedSubmenuTransfer()
        {
            _completedElements.Enqueue(_elements.Pop());
            _conditionalBlockers.Pop();
            _menuTypeKeys.Enqueue(_tempMenuTypeKeys.Pop());
        }

        public void RegisterMenu(MenuType menuType)
        {
            PopulateDefaults();

            _prefManager.SetProfile(GlobalPreferences.GetProfile(MOD_GUID));
            Load();
            _tempMenuTypeKeys.Push(_topLevelTypeKey);

            if (!_isPreferencesEventsRegistered)
            {
                _isPreferencesEventsRegistered = true;
                while (_elements.Count > 0)
                {
                    CompletedSubmenuTransfer();
                }

                while (_completedElements.Count > 0)
                {
                    List<(ElementType, object)> submenuElements = _completedElements.Dequeue();
                    Type menuKey = _menuTypeKeys.Dequeue();

                    Events.PreferenceMenu_MainMenu_CreateSubmenusEvent += (s, args) =>
                    {
                        if (!args.Menus.TryGetValue(typeof(ConfirmMenu<MenuAction>), out Menu<MenuAction> confirmMenu))
                        {
                            confirmMenu = new ConfirmMenu<MenuAction>(args.Container, args.Module_list);
                            args.Menus.Add(typeof(ConfirmMenu<MenuAction>), confirmMenu);
                        }
                        Submenu<MenuAction> submenu = new Submenu<MenuAction>(args.Container, args.Module_list, MOD_GUID, _prefManager, submenuElements, (ConfirmMenu<MenuAction>)confirmMenu);
                        args.Menus.Add(menuKey, submenu);
                    };

                    Events.PreferenceMenu_PauseMenu_CreateSubmenusEvent += (s, args) =>
                    {
                        if (!args.Menus.TryGetValue(typeof(ConfirmMenu<MenuAction>), out Menu<MenuAction> confirmMenu))
                        {
                            confirmMenu = new ConfirmMenu<MenuAction>(args.Container, args.Module_list);
                            args.Menus.Add(typeof(ConfirmMenu<MenuAction>), confirmMenu);
                        }
                        Submenu<MenuAction> submenu = new Submenu<MenuAction>(args.Container, args.Module_list, MOD_GUID, _prefManager, submenuElements, (ConfirmMenu<MenuAction>)confirmMenu);
                        args.Menus.Add(menuKey, submenu);
                    };
                }
            }

            //if (menuType == MenuType.MainMenu && !_mainMenuRegistered)
            //{
            //    PreferenceSystemMenu<MenuAction>.RegisterMenu(MOD_NAME, _mainTopLevelTypeKey, typeof(MenuAction));
            //    _mainMenuRegistered = true;
            //}
            //else if (menuType == MenuType.PauseMenu && !_pauseMenuRegistered)
            //{
            //    PreferenceSystemMenu<MenuAction>.RegisterMenu(MOD_NAME, _pauseTopLevelTypeKey, typeof(MenuAction));
            //    _pauseMenuRegistered = true;
            //}

            if (!_menuRegistered)
            {
                PreferenceSystemMenu<MenuAction>.RegisterMenu(MOD_NAME, _topLevelTypeKey, typeof(MenuAction));
                _menuRegistered = true;
            }
        }

        private class Submenu<T> : BaseMenu<T>
        {
            private readonly string _modGUID;
            private readonly PreferenceManager _prefManager;
            private readonly List<(ElementType, object)> _elements;
            private readonly ConfirmMenu<T> _confirmMenu;
            private List<bool> _isBlocking;
            private int currentPage = 1;

            public Submenu(Transform container, ModuleList module_list, string ModGUID, PreferenceManager preferenceManager, List<(ElementType, object)> elements, ConfirmMenu<T> confirmMenu) : base(container, module_list)
            {
                _modGUID = ModGUID;
                _prefManager = preferenceManager;
                _elements = elements;
                _confirmMenu = confirmMenu;
                _isBlocking = new List<bool>();
            }

            public override void Setup(int player_id)
            {
                Redraw(player_id);
            }

            private void Redraw(int player_id, int selectElementIndex = -1)
            {
                ModuleList.Clear();
                _isBlocking.Clear();
                int totalPagedItemsCount = _elements.Select(x => x.Item1).Where(x => x == ElementType.PagedItem).Count();
                int startPagedItemIndex = 0;
                int endPagedItemIndex = 0;
                int currentPagedItemsDrawn = 0;
                bool hasPageSelector = false;
                bool isPagedItemBlocked = false;
                for (int i = 0; i < _elements.Count; i++)
                {
                    int elementIndex = i;
                    (ElementType type, object data) element = _elements[i];

                    if (isPagedItemBlocked && element.type != ElementType.PagedItemDone)
                        continue;

                    switch (element.type)
                    {
                        case ElementType.ConditionalBlocker:
                            ConditionalBlockerData conditionalBlockerData = (ConditionalBlockerData)element.data;
                            _isBlocking.Add(conditionalBlockerData.ShouldBlock());
                            break;
                        case ElementType.ConditionalBlockerDone:
                            if (_isBlocking.Count > 0)
                                _isBlocking.RemoveAt(_isBlocking.Count - 1);
                            break;
                        default:
                            if (_isBlocking.Count > 0 && _isBlocking.Where(block => block).Any())
                                continue;
                            break;
                    }

                    Element menuElement = null;
                    switch (element.type)
                    {
                        case ElementType.Label:
                            AddLabel(((LabelData)element.data).Text);
                            break;
                        case ElementType.Info:
                            AddInfo(((InfoData)element.data).Text);
                            break;
                        case ElementType.Select:
                            SelectData selectData = (SelectData)element.data;
                            menuElement = AddSelect(selectData.Options, delegate(int selectDataIndex)
                            {
                                selectData.OnActivate(selectDataIndex);
                                if (selectData.Redraw)
                                {
                                    Redraw(player_id, elementIndex);
                                }
                            }, selectData.Index);
                            break;
                        case ElementType.Button:
                            ButtonData buttonData = (ButtonData)element.data;

                            Action<int> buttonDataOnActivate = buttonData.OnActivate;
                            if (buttonData.CloseOnPress && typeof(T) == typeof(MenuAction))
                            {
                                buttonDataOnActivate = delegate (int i)
                                {
                                    buttonData.OnActivate(i);
                                    RequestAction(ChangeType<T>(PauseMenuAction.CloseMenu));
                                };
                            }
                            AddButton(buttonData.ButtonText, buttonDataOnActivate, buttonData.Arg, buttonData.Scale, buttonData.Padding);
                            break;
                        case ElementType.ActionButton:
                            ActionButtonData actionButtonData = (ActionButtonData)element.data;
                            if (actionButtonData.MenuType == typeof(T))
                            {
                                AddActionButton(actionButtonData.ButtonText, ChangeType<T>(actionButtonData.Action));
                            }
                            break;
                        case ElementType.ButtonWithConfirm:
                            ButtonWithConfirmData buttonWithConfirmData = (ButtonWithConfirmData)element.data;
                            AddButtonWithConfirm(buttonWithConfirmData.ButtonText, buttonWithConfirmData.InfoText, buttonWithConfirmData.Callback, buttonWithConfirmData.Arg, buttonWithConfirmData.Scale, buttonWithConfirmData.Padding);
                            break;
                        case ElementType.PlayerRow:
                            PlayerRowData playerRowData = (PlayerRowData)element.data;
                            AddPlayerRow(playerRowData.Username, playerRowData.Player, playerRowData.OnKick, playerRowData.OnRemove, playerRowData.Arg, playerRowData.Scale, playerRowData.Padding);
                            break;
                        case ElementType.SubmenuButton:
                            SubmenuButtonData submenuButtonData = (SubmenuButtonData)element.data;
                            AddSubmenuButton(submenuButtonData.ButtonText, submenuButtonData.MenuKey, submenuButtonData.SkipStack);
                            break;
                        case ElementType.BoolOption:
                            OptionData<bool> boolOptionData = (OptionData<bool>)element.data;
                            Option<bool> boolOption = new Option<bool>(boolOptionData.Values, _prefManager.GetPreference<PreferenceBool>(boolOptionData.Key).Value, boolOptionData.Strings);
                            menuElement = AddSelect(boolOption);
                            boolOption.OnChanged += boolOptionData.EventHandler;
                            if (boolOptionData.Redraw)
                            {
                                boolOption.OnChanged += delegate (object _, bool _)
                                {
                                    Redraw(player_id, elementIndex);
                                };
                            }
                            break;
                        case ElementType.IntOption:
                            OptionData<int> intOptionData = (OptionData<int>)element.data;
                            Option<int> intOption = new Option<int>(intOptionData.Values, _prefManager.GetPreference<PreferenceInt>(intOptionData.Key).Value, intOptionData.Strings);
                            menuElement = AddSelect(intOption);
                            intOption.OnChanged += intOptionData.EventHandler;
                            if (intOptionData.Redraw)
                            {
                                intOption.OnChanged += delegate (object _, int _)
                                {
                                    Redraw(player_id, elementIndex);
                                };
                            }
                            break;
                        case ElementType.FloatOption:
                            OptionData<float> floatOptionData = (OptionData<float>)element.data;
                            Option<float> floatOption = new Option<float>(floatOptionData.Values, _prefManager.GetPreference<PreferenceFloat>(floatOptionData.Key).Value, floatOptionData.Strings);
                            menuElement = AddSelect(floatOption);
                            floatOption.OnChanged += floatOptionData.EventHandler;
                            if (floatOptionData.Redraw)
                            {
                                floatOption.OnChanged += delegate (object _, float _)
                                {
                                    Redraw(player_id, elementIndex);
                                };
                            }
                            break;
                        case ElementType.StringOption:
                            OptionData<string> stringOptionData = (OptionData<string>)element.data;
                            Option<string> stringOption = new Option<string>(stringOptionData.Values, _prefManager.GetPreference<PreferenceString>(stringOptionData.Key).Value, stringOptionData.Strings);
                            menuElement = AddSelect(stringOption);
                            stringOption.OnChanged += stringOptionData.EventHandler;
                            if (stringOptionData.Redraw)
                            {
                                stringOption.OnChanged += delegate (object _, string _)
                                {
                                    Redraw(player_id, elementIndex);
                                };
                            }
                            break;
                        case ElementType.ProfileSelector:
                            AddProfileSelector(_modGUID, delegate (string s)
                            {
                                Redraw(player_id, i);
                            }, _prefManager, true);
                            break;
                        case ElementType.DeleteProfileButton:
                            DeleteProfileButtonData deleteProfileButtonData = (DeleteProfileButtonData)element.data;
                            AddDeleteProfileButton(deleteProfileButtonData.ButtonText, _prefManager, deleteProfileButtonData.Arg, deleteProfileButtonData.Scale, deleteProfileButtonData.Padding);
                            break;
                        case ElementType.Spacer:
                            New<SpacerElement>();
                            break;
                        case ElementType.PageSelector:
                            hasPageSelector = true;
                            PageSelectorData pageSelectorData = (PageSelectorData)element.data;
                            int pageSelectorMaxItems = Mathf.Max(1, pageSelectorData.MaxItemsPerPage);
                            IEnumerable<int> pageIndex = Enumerable.Range(1, Mathf.CeilToInt(totalPagedItemsCount / (float)pageSelectorMaxItems));
                            Option<int> pageSelectorOption = new Option<int>(pageIndex.ToList(), currentPage, pageIndex.Select(i => $"Page {i}").ToList());
                            AddSelect(pageSelectorOption);
                            pageSelectorOption.OnChanged += delegate (object _, int i)
                            {
                                currentPage = i;
                                Redraw(player_id, elementIndex);
                            };
                            startPagedItemIndex = (currentPage - 1) * pageSelectorMaxItems;
                            endPagedItemIndex = startPagedItemIndex + pageSelectorMaxItems - 1;
                            break;
                        case ElementType.PagedItem:
                            if (hasPageSelector && (currentPagedItemsDrawn < startPagedItemIndex || currentPagedItemsDrawn > endPagedItemIndex))
                                isPagedItemBlocked = true;
                            currentPagedItemsDrawn++;
                            break;
                        case ElementType.PagedItemDone:
                            isPagedItemBlocked = false;
                            break;
                        default:
                            break;
                    }

                    if (menuElement != null && selectElementIndex == i)
                    {
                        ModuleList.Select(menuElement);
                    }
                }

                AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
                {
                    RequestPreviousMenu();
                });

                Container?.Find("Panel(Clone)")?.GetComponent<PanelElement>()?.SetTarget(ModuleList);
                Transform playerPauseViewGO = Container?.parent?.parent?.parent;
                if (playerPauseViewGO?.GetComponent<PlayerPauseView>() != null)
                {
                    playerPauseViewGO.localPosition = -ModuleList.BoundingBox.center;
                }
            }

            protected ButtonElement AddButtonWithConfirm(string label, string infoText, Action<GenericChoiceDecision> callback, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                return AddButton(label, delegate (int _)
                {
                    _confirmMenu.SetAction(callback, infoText);
                    RequestSubMenu(typeof(ConfirmMenu<T>));
                }, arg, scale, padding);
            }

            protected void AddDeleteProfileButton(string label, PreferenceManager manager, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                AddButton(label, delegate (int _)
                {
                    string current_profile = GlobalPreferences.GetProfile(_modGUID);
                    if (!current_profile.IsNullOrEmpty())
                    {
                        _confirmMenu.SetAction(delegate (GenericChoiceDecision decision)
                        {
                            if (decision == GenericChoiceDecision.Accept)
                            {
                                List<string> list = GlobalPreferences.GetProfiles(_modGUID).ToList();
                                int currentProfileIndex = list.IndexOf(current_profile);

                                if (currentProfileIndex == -1)
                                {
                                    Debug.LogError($"[PreferenceSystem] Failed to find index of {current_profile}.");
                                }
                                else
                                {
                                    GlobalPreferences.RemoveProfile(_modGUID, current_profile);
                                    Debug.LogError($"[PreferenceSystem] Removed profile {current_profile}.");
                                    string profileToLoad = "";
                                    if (list.Count > 1)
                                    {
                                        if (currentProfileIndex > 0)
                                            currentProfileIndex--;
                                        else
                                            currentProfileIndex++;
                                        profileToLoad = list[currentProfileIndex];
                                    }
                                    GlobalPreferences.SetProfile(_modGUID, profileToLoad);
                                    manager.SetProfile(profileToLoad);
                                    manager.Load();
                                    manager.Save();
                                }
                            }
                        }, $"Delete preference profile, {current_profile}?");
                        RequestSubMenu(typeof(ConfirmMenu<T>));
                    }
                }, arg, scale, padding);
            }
        }

        public class ConfirmMenu<T> : BaseMenu<T>
        {
            private string _infoText = String.Empty;
            private Action<GenericChoiceDecision> _callback = null;

            public ConfirmMenu(Transform container, ModuleList module_list) : base(container, module_list)
            {
            }

            public override void Setup(int player_id)
            {
                AddLabel("Confirm?");
                if (!_infoText.IsNullOrEmpty())
                    AddInfo(_infoText);
                New<SpacerElement>();
                New<SpacerElement>();
                AddButton("Accept", delegate (int _)
                {
                    Complete(GenericChoiceDecision.Accept);
                });
                AddButton("Cancel", delegate (int _)
                {
                    Complete(GenericChoiceDecision.Cancel);
                });
            }

            protected void Complete(GenericChoiceDecision decision)
            {
                if (_callback != null)
                    _callback(decision);
                RequestPreviousMenu();
            }

            public void SetAction(Action<GenericChoiceDecision> callback, string infoText = "")
            {
                _infoText = infoText?? "";
                _callback = callback;
            }
        }
    }
}
