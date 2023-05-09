using Kitchen;
using Kitchen.Modules;
using KitchenLib;
using KitchenLib.Event;
using KitchenLib.Preferences;
using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.LookDev;
using UnityEngine.SocialPlatforms.Impl;

namespace PreferenceSystem
{
    public class PreferenceSystemManager
    {
        public enum MenuType
        {
            MainMenu,
            PauseMenu
        }

        public enum MenuAction
        {
            MainMenuNull,
            MainMenuStartSingleplayer,
            MainMenuQuit,
            MainMenuStartMultiplayer,
            MainMenuBack,

            PauseMenuCloseMenu,
            PauseMenuBack,
            PauseMenuDisconnectPlayer,
            PauseMenuQuit,
            PauseMenuAbandonRestaurant,
            PauseMenuOpenInvitePanel,
            PauseMenuLeaveGame,
            PauseMenuPracticeMode
        }

        public readonly string MOD_GUID;
        public readonly string MOD_NAME;

        private bool _isKLPreferencesEventsRegistered = false;
        public bool IsKLPreferencesEventsRegistered
        {
            get { return _isKLPreferencesEventsRegistered; }
        }

        AssemblyBuilder _assemblyBuilder;
        ModuleBuilder _moduleBuilder;

        private static readonly Regex sWhitespace = new Regex(@"\s+");

        private PreferenceManager _kLPrefManager;

        private Dictionary<string, Type> _registeredPreferences = new Dictionary<string, Type>();
        private Dictionary<string, PreferenceBool> boolPreferences = new Dictionary<string, PreferenceBool>();
        private Dictionary<string, PreferenceInt> intPreferences = new Dictionary<string, PreferenceInt>();
        private Dictionary<string, PreferenceFloat> floatPreferences = new Dictionary<string, PreferenceFloat>();
        private Dictionary<string, PreferenceString> stringPreferences = new Dictionary<string, PreferenceString>();

        public static Type[] AllowedTypes => new Type[]
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string)
        };

        public static Dictionary<string, Type> AllowedTypesDict => AllowedTypes.ToDictionary(x => x.FullName, x => x);


        private bool _mainMenuRegistered = false;
        private bool _pauseMenuRegistered = false;
        private Type _mainTopLevelTypeKey;
        private Type _pauseTopLevelTypeKey;
        private Queue<Type> _mainMenuTypeKeys = new Queue<Type>();
        private Queue<Type> _pauseMenuTypeKeys = new Queue<Type>();
        private Queue<List<(ElementType, object)>> _completedElements = new Queue<List<(ElementType, object)>>();
        private Stack<Type> _tempMainMenuTypeKeys = new Stack<Type>();
        private Stack<Type> _tempPauseMenuTypeKeys = new Stack<Type>();
        private Stack<List<(ElementType, object)>> _elements = new Stack<List<(ElementType, object)>>();
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
            Spacer
        }

        public PreferenceSystemManager(string modGUID, string modName)
        {
            MOD_GUID = modGUID;
            MOD_NAME = modName;
            _kLPrefManager = new PreferenceManager(modGUID);

            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.{MOD_GUID}"), AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("Module");

            _mainTopLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_Main");
            _pauseTopLevelTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_Pause");
            _elements.Push(new List<(ElementType, object)>());

            _completedElements = new Queue<List<(ElementType, object)>>();

            PreferenceSystemRegistry.Add(this);
        }

        private static bool IsAllowedType(Type type, bool throwExceptionIfNotAllowed = false)
        {
            if (!AllowedTypes.Contains(type))
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
            throw new ArgumentException($"Type TPref is not supported! Only use {allowedTypesStr}.");
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

        private T ChangeType<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(T));
        }

        private void Preference_OnChanged<T>(string key, T value)
        {
            Set(key, value);
        }

        public PreferenceSystemManager AddOption<T>(string key, T initialValue, T[] values, string[] strings)
        {
            return PrivateAddOption<T>(key, initialValue, values, strings, false);
        }

        public PreferenceSystemManager AddProperty<T>(string key, T initialValue)
        {
            return PrivateAddOption<T>(key, initialValue, null, null, true);
        }

        private PreferenceSystemManager PrivateAddOption<T>(string key, T initialValue, T[] values, string[] strings, bool doNotShow)
        {
            IsAllowedType(typeof(T), true);
            IsUsedKey(key, true);

            if (typeof(T) == typeof(bool))
            {
                PreferenceBool preference = _kLPrefManager.RegisterPreference(new PreferenceBool(key, ChangeType<bool>(initialValue)));
                boolPreferences.Add(key, preference);
                if (!doNotShow)
                {
                    EventHandler<bool> handler = delegate (object _, bool b)
                    {
                        Preference_OnChanged(key, b);
                    };
                    _elements.Peek().Add((ElementType.BoolOption, new OptionData<bool>(MOD_GUID, key, values.Cast<bool>().ToList(), strings.ToList(), handler)));
                }
            }
            else if (typeof(T) == typeof(int))
            {
                PreferenceInt preference = _kLPrefManager.RegisterPreference(new PreferenceInt(key, ChangeType<int>(initialValue)));
                intPreferences.Add(key, preference);
                if (!doNotShow)
                {
                    EventHandler<int> handler = delegate (object _, int i)
                    {
                        Preference_OnChanged(key, i);
                    };
                    _elements.Peek().Add((ElementType.IntOption, new OptionData<int>(MOD_GUID, key, values.Cast<int>().ToList(), strings.ToList(), handler)));
                }
            }
            else if (typeof(T) == typeof(float))
            {
                PreferenceFloat preference = _kLPrefManager.RegisterPreference(new PreferenceFloat(key, ChangeType<float>(initialValue)));
                floatPreferences.Add(key, preference);
                if (!doNotShow)
                {
                    EventHandler<float> handler = delegate (object _, float f)
                    {
                        Preference_OnChanged(key, f);
                    };
                    _elements.Peek().Add((ElementType.FloatOption, new OptionData<float>(MOD_GUID, key, values.Cast<float>().ToList(), strings.ToList(), handler)));
                }
            }
            else if (typeof(T) == typeof(string))
            {
                PreferenceString preference = _kLPrefManager.RegisterPreference(new PreferenceString(key, ChangeType<string>(initialValue)));
                stringPreferences.Add(key, preference);

                if (!doNotShow)
                {
                    EventHandler<string> handler = delegate (object _, string s)
                    {
                        Preference_OnChanged(key, s);
                    };
                    _elements.Peek().Add((ElementType.StringOption, new OptionData<string>(MOD_GUID, key, values.Cast<string>().ToList(), strings.ToList(), handler)));
                }
            }
            _registeredPreferences.Add(key, typeof(T));
            return this;
        }

        public T Get<T>(string key)
        {
            return (T)(Get(key, typeof(T)) ?? default(T));
        }

        private object Get(string key, Type valueType)
        {
            IsAllowedType(valueType, true);

            object value = null;
            if (valueType == typeof(bool))
            {
                value = _kLPrefManager.GetPreference<PreferenceBool>(key).Get();
            }
            else if (valueType == typeof(int))
            {
                value = _kLPrefManager.GetPreference<PreferenceInt>(key).Get();
            }
            else if (valueType == typeof(float))
            {
                value = _kLPrefManager.GetPreference<PreferenceFloat>(key).Get();
            }
            else if (valueType == typeof(string))
            {
                value = _kLPrefManager.GetPreference<PreferenceString>(key).Get();
            }
            return value;
        }

        public void Set<T>(string key, T value)
        {
            Set(key, typeof(T), value);
        }

        public void Set(string key, Type valueType , object value)
        {
            IsAllowedType(valueType, true);

            if (valueType == typeof(bool))
            {
                _kLPrefManager.GetPreference<PreferenceBool>(key).Set(ChangeType<bool>(value));
            }
            else if (valueType == typeof(int))
            {
                _kLPrefManager.GetPreference<PreferenceInt>(key).Set(ChangeType<int>(value));
            }
            else if (valueType == typeof(float))
            {
                _kLPrefManager.GetPreference<PreferenceFloat>(key).Set(ChangeType<float>(value));
            }
            else if (valueType == typeof(string))
            {
                _kLPrefManager.GetPreference<PreferenceString>(key).Set(ChangeType<string>(value));
            }
            Save();
        }

        public void SetProfile(string profileName)
        {
            if (!GlobalPreferences.DoesProfileExist(MOD_GUID, profileName))
            {
                GlobalPreferences.AddProfile(MOD_GUID, profileName);
            }
            GlobalPreferences.SetProfile(MOD_GUID, profileName);
            _kLPrefManager.SetProfile(profileName);
            _kLPrefManager.Load();
            _kLPrefManager.Save();
        }

        private void Save()
        {
            _kLPrefManager.Save();
        }

        private void Load()
        {
            _kLPrefManager.Load();
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
            foreach (PreferenceData prefData in data.Preferences)
            {
                try
                {
                    SetProfile($"{preferenceSetName}_Loaded");
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
            public SelectData(List<string> options, Action<int> on_activate, int index = 0)
            {
                Options = options;
                OnActivate = on_activate;
                Index = index;
            }
        }
        private readonly struct ButtonData
        {
            public readonly string ButtonText;
            public readonly Action<int> OnActivate;
            public readonly int Arg;
            public readonly float Scale;
            public readonly float Padding;
            public ButtonData(string button_text, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                ButtonText = button_text;
                OnActivate = on_activate;
                Arg = arg;
                Scale = scale;
                Padding = padding;
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
            public readonly Type MainMenuKey;
            public readonly Type PauseMenuKey;
            public readonly bool SkipStack;
            public SubmenuButtonData(string button_text, Type main_menu_key, Type pause_menu_key, bool skip_stack = false)
            {
                ButtonText = button_text;
                MainMenuKey = main_menu_key;
                PauseMenuKey = pause_menu_key;
                SkipStack = skip_stack;
            }
        }
        private readonly struct ActionButtonData
        {
            public readonly string ButtonText;
            public readonly MenuAction Action;
            public readonly ElementStyle Style;
            public ActionButtonData(string button_text, MenuAction action, ElementStyle style = ElementStyle.Default)
            {
                ButtonText = button_text;
                Action = action;
                Style = style;
            }
        }
        private readonly struct OptionData<T>
        {
            public readonly string ModGUID;
            public readonly string Key;
            public readonly List<T> Values;
            public readonly List<string> Strings;
            public readonly EventHandler<T> EventHandler;
            public OptionData(string modGuid, string key, List<T> values, List<string> strings, EventHandler<T> eventHandler)
            {
                ModGUID = modGuid;
                Key = key;
                Values = values;
                Strings = strings;
                EventHandler = eventHandler;
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

        public PreferenceSystemManager AddSelect(List<string> options, Action<int> on_activate, int index = 0)
        {
            _elements.Peek().Add((ElementType.Select, new SelectData(options, on_activate, index)));
            return this;
        }

        public PreferenceSystemManager AddButton(string button_text, Action<int> on_activate, int arg = 0, float scale = 1f, float padding = 0.2f)
        {
            _elements.Peek().Add((ElementType.Button, new ButtonData(button_text, on_activate, arg, scale, padding)));
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
            Type mainTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_{sWhitespace.Replace(submenu_key, "")}_Main");
            Type pauseTypeKey = CreateTypeKey($"{sWhitespace.Replace(MOD_NAME, "")}_{sWhitespace.Replace(submenu_key, "")}_Pause");
            if (_mainMenuTypeKeys.Contains(mainTypeKey) || _tempMainMenuTypeKeys.Contains(mainTypeKey))
            {
                throw new ArgumentException("Submenu key already exists!");
            }
            _tempMainMenuTypeKeys.Push(mainTypeKey);
            _tempPauseMenuTypeKeys.Push(pauseTypeKey);
            _elements.Peek().Add((ElementType.SubmenuButton, new SubmenuButtonData(button_text, mainTypeKey, pauseTypeKey, skip_stack)));
            _elements.Push(new List<(ElementType, object)>());
            return this;
        }

        public PreferenceSystemManager AddActionButton(string button_text, MenuAction action, ElementStyle style = ElementStyle.Default)
        {
            _elements.Peek().Add((ElementType.Button, new ActionButtonData(button_text, action, style)));
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

        private Type CreateTypeKey(string typeName)
        {
            //Creating dummy types to use as keys for submenu instances when registering the submenus to keys in KitchenLib CreateSubmenusEvent
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public);
            Type type = typeBuilder.CreateType();
            return type;
        }

        private void CompletedSubmenuTransfer()
        {
            _completedElements.Enqueue(_elements.Pop());
            _mainMenuTypeKeys.Enqueue(_tempMainMenuTypeKeys.Pop());
            _pauseMenuTypeKeys.Enqueue(_tempPauseMenuTypeKeys.Pop());
        }

        public void RegisterMenu(MenuType menuType)
        {
            _kLPrefManager.SetProfile(GlobalPreferences.GetProfile(MOD_GUID));
            Load();
            _tempMainMenuTypeKeys.Push(_mainTopLevelTypeKey);
            _tempPauseMenuTypeKeys.Push(_pauseTopLevelTypeKey);

            if (!_isKLPreferencesEventsRegistered)
            {
                _isKLPreferencesEventsRegistered = true;
                while (_elements.Count > 0)
                {
                    CompletedSubmenuTransfer();
                }

                while (_completedElements.Count > 0)
                {
                    List<(ElementType, object)> submenuElements = _completedElements.Dequeue();
                    Type mainMenuKey = _mainMenuTypeKeys.Dequeue();
                    Type pauseMenuKey = _pauseMenuTypeKeys.Dequeue();

                    Events.PreferenceMenu_MainMenu_CreateSubmenusEvent += (s, args) =>
                    {
                        if (!args.Menus.TryGetValue(typeof(ConfirmMenu<MainMenuAction>), out Menu<MainMenuAction> confirmMenu))
                        {
                            confirmMenu = new ConfirmMenu<MainMenuAction>(args.Container, args.Module_list);
                            args.Menus.Add(typeof(ConfirmMenu<MainMenuAction>), confirmMenu);
                        }
                        Submenu<MainMenuAction> submenu = new Submenu<MainMenuAction>(args.Container, args.Module_list, MOD_GUID, _kLPrefManager, submenuElements, (ConfirmMenu<MainMenuAction>)confirmMenu);
                        args.Menus.Add(mainMenuKey, submenu);
                    };

                    Events.PreferenceMenu_PauseMenu_CreateSubmenusEvent += (s, args) =>
                    {
                        if (!args.Menus.TryGetValue(typeof(ConfirmMenu<PauseMenuAction>), out Menu<PauseMenuAction> confirmMenu))
                        {
                            confirmMenu = new ConfirmMenu<PauseMenuAction>(args.Container, args.Module_list);
                            args.Menus.Add(typeof(ConfirmMenu<PauseMenuAction>), confirmMenu);
                        }
                        Submenu<PauseMenuAction> submenu = new Submenu<PauseMenuAction>(args.Container, args.Module_list, MOD_GUID, _kLPrefManager, submenuElements, (ConfirmMenu<PauseMenuAction>)confirmMenu);
                        args.Menus.Add(pauseMenuKey, submenu);
                    };
                }
            }

            if (menuType == MenuType.MainMenu && !_mainMenuRegistered)
            {
                ModsPreferencesMenu<MainMenuAction>.RegisterMenu(MOD_NAME, _mainTopLevelTypeKey, typeof(MainMenuAction));
                _mainMenuRegistered = true;
            }
            else if (menuType == MenuType.PauseMenu && !_pauseMenuRegistered)
            {
                ModsPreferencesMenu<PauseMenuAction>.RegisterMenu(MOD_NAME, _pauseTopLevelTypeKey, typeof(PauseMenuAction));
                _pauseMenuRegistered = true;
            }
        }

        private class Submenu<T> : KLMenu<T>
        {
            private readonly string _modGUID;
            private readonly PreferenceManager _kLPrefManager;
            private readonly List<(ElementType, object)> _elements;
            private readonly ConfirmMenu<T> _confirmMenu;

            public Submenu(Transform container, ModuleList module_list, string ModGUID, PreferenceManager preferenceManager, List<(ElementType, object)> elements, ConfirmMenu<T> confirmMenu) : base(container, module_list)
            {
                _modGUID = ModGUID;
                _kLPrefManager = preferenceManager;
                _elements = elements;
                _confirmMenu = confirmMenu;
            }

            public override void Setup(int player_id)
            {
                Redraw(player_id);
            }

            private void Redraw(int player_id)
            {
                ModuleList.Clear();
                foreach (var element in _elements)
                {
                    switch (element.Item1)
                    {
                        case ElementType.Label:
                            AddLabel(((LabelData)element.Item2).Text);
                            break;
                        case ElementType.Info:
                            AddInfo(((InfoData)element.Item2).Text);
                            break;
                        case ElementType.Select:
                            SelectData selectData = (SelectData)element.Item2;
                            AddSelect(selectData.Options, selectData.OnActivate, selectData.Index);
                            break;
                        case ElementType.Button:
                            ButtonData buttonData = (ButtonData)element.Item2;
                            AddButton(buttonData.ButtonText, buttonData.OnActivate, buttonData.Arg, buttonData.Scale, buttonData.Padding);
                            break;
                        case ElementType.ButtonWithConfirm:
                            ButtonWithConfirmData buttonWithConfirmData = (ButtonWithConfirmData)element.Item2;
                            AddButtonWithConfirm(buttonWithConfirmData.ButtonText, buttonWithConfirmData.InfoText, buttonWithConfirmData.Callback, buttonWithConfirmData.Arg, buttonWithConfirmData.Scale, buttonWithConfirmData.Padding);
                            break;
                        case ElementType.PlayerRow:
                            PlayerRowData playerRowData = (PlayerRowData)element.Item2;
                            AddPlayerRow(playerRowData.Username, playerRowData.Player, playerRowData.OnKick, playerRowData.OnRemove, playerRowData.Arg, playerRowData.Scale, playerRowData.Padding);
                            break;
                        case ElementType.SubmenuButton:
                            SubmenuButtonData submenuButtonData = (SubmenuButtonData)element.Item2;
                            Type submenuKey = typeof(T) == typeof(MainMenuAction) ? submenuButtonData.MainMenuKey : submenuButtonData.PauseMenuKey;
                            AddSubmenuButton(submenuButtonData.ButtonText, submenuKey, submenuButtonData.SkipStack);
                            break;
                        case ElementType.BoolOption:
                            OptionData<bool> boolOptionData = (OptionData<bool>)element.Item2;
                            Option<bool> boolOption = new Option<bool>(boolOptionData.Values, _kLPrefManager.GetPreference<PreferenceBool>(boolOptionData.Key).Value, boolOptionData.Strings);
                            Add(boolOption);
                            boolOption.OnChanged += boolOptionData.EventHandler;
                            break;
                        case ElementType.IntOption:
                            OptionData<int> intOptionData = (OptionData<int>)element.Item2;
                            Option<int> intOption = new Option<int>(intOptionData.Values, _kLPrefManager.GetPreference<PreferenceInt>(intOptionData.Key).Value, intOptionData.Strings);
                            Add(intOption);
                            intOption.OnChanged += intOptionData.EventHandler;
                            break;
                        case ElementType.FloatOption:
                            OptionData<float> floatOptionData = (OptionData<float>)element.Item2;
                            Option<float> floatOption = new Option<float>(floatOptionData.Values, _kLPrefManager.GetPreference<PreferenceFloat>(floatOptionData.Key).Value, floatOptionData.Strings);
                            Add(floatOption);
                            floatOption.OnChanged += floatOptionData.EventHandler;
                            break;
                        case ElementType.StringOption:
                            OptionData<string> stringOptionData = (OptionData<string>)element.Item2;
                            Option<string> stringOption = new Option<string>(stringOptionData.Values, _kLPrefManager.GetPreference<PreferenceString>(stringOptionData.Key).Value, stringOptionData.Strings);
                            Add(stringOption);
                            stringOption.OnChanged += stringOptionData.EventHandler;
                            break;
                        case ElementType.ProfileSelector:
                            AddProfileSelector(_modGUID, delegate (string s)
                            {
                                Redraw(player_id);
                            }, _kLPrefManager, true);
                            break;
                        case ElementType.DeleteProfileButton:
                            DeleteProfileButtonData deleteProfileButtonData = (DeleteProfileButtonData)element.Item2;
                            AddDeleteProfileButton(deleteProfileButtonData.ButtonText, _kLPrefManager, deleteProfileButtonData.Arg, deleteProfileButtonData.Scale, deleteProfileButtonData.Padding);
                            break;
                        case ElementType.Spacer:
                            New<SpacerElement>();
                            break;
                        default:
                            break;
                    }
                }
                AddButton(base.Localisation["MENU_BACK_SETTINGS"], delegate
                {
                    RequestPreviousMenu();
                });
            }

            protected void AddButtonWithConfirm(string label, string infoText, Action<GenericChoiceDecision> callback, int arg = 0, float scale = 1f, float padding = 0.2f)
            {
                AddButton(label, delegate (int _)
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
                                    if (!profileToLoad.IsNullOrEmpty())
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

        public class ConfirmMenu<T> : KLMenu<T>
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
