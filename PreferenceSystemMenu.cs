using KitchenLib.DevUI;
using KitchenLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PreferenceSystem
{
    public class PreferenceSystemMenu : BaseUI
    {
        public const float TOTAL_DRAWABLE_WIDTH = 800f;
        public const float TOTAL_DRAWABLE_HEIGHT = 1070f;

        public const float WINDOW_WIDTH = 775f;
        public const float WINDOW_HEIGHT = 1050f;

        public const float HARDCODED_DEFAULT_PADDING = 4;

        public const string LEFT_ARROW = "<==";
        public const string RIGHT_ARROW = "==>";
        
        public PreferenceSystemMenu()
        {
            ButtonName = "PrefSys";
        }

        protected enum Mode
        {
            None,
            Import,
            Export,
            Load
        }

        protected readonly List<(Mode, string)> Modes = new List<(Mode, string)>()
        {
            (Mode.Import, "Import"),
            (Mode.Export, "Export"),
            (Mode.Load, "Load")
        };
        protected bool ModeChanged = false;
        protected Mode SelectedMode = Mode.None;

        private Texture2D _background;
        private GUIStyle _labelMiddleLeftStyle;
        private GUIStyle _labelMiddleCenterStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _previewStyle;

        public override void OnInit()
        {
            _background = new Texture2D(64, 64);
            Color grayWithAlpha = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    _background.SetPixel(x, y, grayWithAlpha);
                }
            }
            _background.Apply();
        }

        public override void Setup()
        {
            _labelMiddleLeftStyle = new GUIStyle(GUI.skin.label);
            _labelMiddleLeftStyle.alignment = TextAnchor.MiddleLeft;
            _labelMiddleLeftStyle.stretchHeight = true;
            _labelMiddleLeftStyle.wordWrap = true;

            _labelMiddleCenterStyle = new GUIStyle(GUI.skin.label);
            _labelMiddleCenterStyle.alignment = TextAnchor.MiddleCenter;
            _labelMiddleCenterStyle.wordWrap = true;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            _headerStyle.fontStyle = FontStyle.Bold;

            _statusStyle = new GUIStyle(GUI.skin.textArea);
            _statusStyle.stretchHeight = true;

            _previewStyle = new GUIStyle(GUI.skin.textArea);
            _previewStyle.stretchHeight = true;
            
            GUI.DrawTexture(new Rect(0f, 0f, TOTAL_DRAWABLE_WIDTH, TOTAL_DRAWABLE_HEIGHT), _background, ScaleMode.StretchToFill);
            GUILayout.BeginArea(new Rect((TOTAL_DRAWABLE_WIDTH - WINDOW_WIDTH) / 2, 10f, WINDOW_WIDTH, WINDOW_HEIGHT));

            DrawModeButtons(width: WINDOW_WIDTH, height: 60f);

            switch (SelectedMode)
            {
                case Mode.Import:
                    DrawImport();
                    break;
                case Mode.Export:
                    DrawExport();
                    break;
                case Mode.Load:
                    DrawLoad();
                    break;
                default:
                    break;
            }
            ModeChanged = false;

            GUILayout.EndArea();
        }

        protected virtual void DrawModeButtons(float width = 400f, float height = 300f, int maxColCount = 4)
        {
            int rowCount = Mathf.CeilToInt((float)Modes.Count / maxColCount);
            float buttonHeight = height / rowCount;
            int i = 0;
            foreach ((Mode mode, string text) in Modes)
            {
                if (i % maxColCount == 0)
                    GUILayout.BeginHorizontal(GUILayout.Width(width));

                int fullRowsDrawn = i / maxColCount;
                int buttonsInRow = Mathf.Min(Modes.Count - (fullRowsDrawn * maxColCount), maxColCount);

                if (GUILayout.Button(text, GUILayout.Width(width/buttonsInRow - HARDCODED_DEFAULT_PADDING), GUILayout.Height(buttonHeight - HARDCODED_DEFAULT_PADDING)))
                {
                    ModeChanged = true;
                    SelectedMode = mode;
                }

                i++;
                if (i % maxColCount == 0 || i == Modes.Count)
                    GUILayout.EndHorizontal();
            }
        }

        private Dictionary<string, string> _registeredMods = new Dictionary<string, string>();
        private Vector2 _modsListScrollPos = Vector2.zero;
        private Dictionary<string, string> _selectedForExport = new Dictionary<string, string>();
        private Queue<string> _exportToBeRemoved = new Queue<string>();
        private Vector2 _selectedModsScrollPos = Vector2.zero;
        private const float MODS_LIST_HEIGHT = 200f;
        private const float SCROLLBAR_WIDTH = 30f;

        private const float EXPORT_OPTIONS_HEIGHT = 200f;
        private bool _optionsReadOnlyMode = false;

        private string _preferenceSetName = string.Empty;
        private string statusMessage = "Select mods to export.";
        private string previewText = string.Empty;
        private Vector2 _previewScrollPos = Vector2.zero;

        protected void DrawExport()
        {
            if (ModeChanged)
            {
                _registeredMods = PreferenceSystemRegistry.RegisteredMods;
            }

            float listWidth = WINDOW_WIDTH / 2f;
            float modLabelWidth = (listWidth - SCROLLBAR_WIDTH) * 0.8f;
            float transferButtonWidth = (listWidth - SCROLLBAR_WIDTH) * 0.2f;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(listWidth));
            GUILayout.Label("Mod List", _headerStyle);
            GUILayout.BeginScrollView(_modsListScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(MODS_LIST_HEIGHT));
            foreach (KeyValuePair<string, string> mod in _registeredMods)
            {
                if (_selectedForExport.ContainsKey(mod.Key))
                    continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(mod.Value, GUILayout.Width(modLabelWidth));
                if (GUILayout.Button(RIGHT_ARROW, GUILayout.Width(transferButtonWidth)))
                {
                    if (!_selectedForExport.ContainsKey(mod.Key))
                        _selectedForExport.Add(mod.Key, mod.Value);
                }
                GUILayout.Label("", GUILayout.Width(SCROLLBAR_WIDTH));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(listWidth));
            GUILayout.Label("To Be Exported", _headerStyle);
            GUILayout.BeginScrollView(_selectedModsScrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(MODS_LIST_HEIGHT));
            foreach (KeyValuePair<string, string> mod in _selectedForExport)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(LEFT_ARROW, GUILayout.Width(listWidth * 0.2f)))
                {
                    _exportToBeRemoved.Enqueue(mod.Key);
                }
                GUILayout.Label(mod.Value, GUILayout.Width(listWidth * 0.8f));
                GUILayout.EndHorizontal();
            }

            while (_exportToBeRemoved.Count > 0)
            {
                string toBeRemoved = _exportToBeRemoved.Dequeue();
                _selectedForExport.Remove(toBeRemoved);
            }


            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH * 0.6f));
            //_optionsReadOnlyMode = GUILayout.Toggle(_optionsReadOnlyMode, " Lock Preference Editing When Used");

            GUILayout.Label("");

            GUILayout.Label("Preference Set Name:");
            _preferenceSetName = GUILayout.TextField(_preferenceSetName);

            if (GUILayout.Button("Preview"))
            {
                if (PreferenceSystemRegistry.TryConvert(_preferenceSetName, _selectedForExport.Keys.ToArray(), _optionsReadOnlyMode, out PreferenceSet preferenceSet, out statusMessage))
                {
                    previewText = preferenceSet.Preview();
                    statusMessage = "This is only for reference and cannot be used to import. After confirming the data is correct, you can use \"Generate\" or \"Export\" to create the sharable text.";
                }
            }
            if (GUILayout.Button("Generate"))
            {
                if (PreferenceSystemRegistry.TryConvert(_preferenceSetName, _selectedForExport.Keys.ToArray(), _optionsReadOnlyMode, out PreferenceSet preferenceSet, out statusMessage))
                {
                    previewText = Utils.StringUtils.ToBase64(PreferenceSystemRegistry.Serialize(preferenceSet));
                    statusMessage = "Successfully generated. Copy and share the preview text which, when imported, will create a preference set that can be loaded.";
                }
            }
            if (GUILayout.Button("Export"))
            {
                if (PreferenceSystemRegistry.Export(_preferenceSetName, _optionsReadOnlyMode, out PreferenceSet preferenceSet, out statusMessage, _selectedForExport.Keys.ToArray()))
                {
                    previewText = Utils.StringUtils.ToBase64(PreferenceSystemRegistry.Serialize(preferenceSet));
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Status:");
            GUILayout.Label(statusMessage, _previewStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Label("");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Preview:",GUILayout.Width(WINDOW_WIDTH * 0.8f));
            if (GUILayout.Button("Copy") && !previewText.IsNullOrEmpty())
            {
                GUIUtility.systemCopyBuffer = previewText;
            }
            GUILayout.EndHorizontal();
            _previewScrollPos = GUILayout.BeginScrollView(_previewScrollPos);
            GUILayout.TextArea(previewText, _previewStyle);
            GUILayout.EndScrollView();
        }


        private string _importBase64 = string.Empty;
        private Vector2 _importDataScrollPos = Vector2.zero;
        private string _importNameOverride = string.Empty;
        private string _importStatusMessage = "Paste import data and click \"Import\". You can rename the preference set by entering a new name.";
        protected void DrawImport()
        {
            GUILayout.Label("Encoded Import Data:");
            _importDataScrollPos = GUILayout.BeginScrollView(_importDataScrollPos, GUILayout.Height(600f));
            _importBase64 = GUILayout.TextArea(_importBase64, _previewStyle);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH * 0.6f));
            GUILayout.Label("New Name:");
            _importNameOverride = GUILayout.TextField(_importNameOverride);
            if (GUILayout.Button("Import"))
            {
                if (PreferenceSystemRegistry.Import(_importBase64, out _importStatusMessage, nameOverride: _importNameOverride))
                {
                    _importBase64 = string.Empty;
                    _importNameOverride = string.Empty;
                    statusMessage = "Successfully imported preference set. Go to \"Load\" mode to activate the preference set.";
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Status:");
            GUILayout.Label(_importStatusMessage, _previewStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private string _selectedPreferenceSet = string.Empty;
        private string _selectedPreferenceSetPreview = string.Empty;
        private Vector2 _preferenceSetsScrollPosition = Vector2.zero;
        protected void DrawLoad()
        {
            if (ModeChanged)
            {
                PreferenceSystemRegistry.InitPreferenceSets();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Set Name", _headerStyle, GUILayout.Width(WINDOW_WIDTH * 0.4f));
            GUILayout.Label("Created Date", _headerStyle, GUILayout.Width(WINDOW_WIDTH * 0.3f));
            GUILayout.Label("");
            GUILayout.EndHorizontal();

            _preferenceSetsScrollPosition = GUILayout.BeginScrollView(_preferenceSetsScrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(300f));
            foreach (KeyValuePair<string, PreferenceSet> item in PreferenceSystemRegistry.GetCachedPreferenceSets())
            {
                PreferenceSet set = item.Value;
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.Key, GUILayout.Width(WINDOW_WIDTH * 0.4f));
                GUILayout.Label(DateTimeOffset.FromUnixTimeSeconds(set.CreatedAt).ToString("yyyy/MM/dd - HH:mm:ss"), _labelMiddleCenterStyle, GUILayout.Width(WINDOW_WIDTH * 0.3f));
                if (GUILayout.Button("Select"))
                {
                    _selectedPreferenceSet = item.Key;
                    _selectedPreferenceSetPreview = set.Preview();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load", GUILayout.Width(WINDOW_WIDTH * 0.5f)) && !_selectedPreferenceSet.IsNullOrEmpty())
            {
                PreferenceSystemRegistry.Load(_selectedPreferenceSet);
            }
            if (GUILayout.Button("Delete", GUILayout.Width(WINDOW_WIDTH * 0.5f)) && !_selectedPreferenceSet.IsNullOrEmpty())
            {
                if (PreferenceSystemRegistry.Delete(_selectedPreferenceSet))
                {
                    _selectedPreferenceSet = string.Empty;
                    _selectedPreferenceSetPreview = string.Empty;
                }
            }
            GUILayout.EndHorizontal();
            _previewScrollPos = GUILayout.BeginScrollView(_previewScrollPos);
            GUILayout.Label(_selectedPreferenceSetPreview, _previewStyle);
            GUILayout.EndScrollView();
        }
    }
}
