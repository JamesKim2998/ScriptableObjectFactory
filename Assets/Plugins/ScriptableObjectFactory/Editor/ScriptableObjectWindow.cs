using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace ScriptableObjectFactory
{
    internal class EndNameEdit : EndNameEditAction
    {
        #region implemented abstract members of EndNameEditAction

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            AssetDatabase.CreateAsset(EditorUtility.InstanceIDToObject(instanceId),
                AssetDatabase.GenerateUniqueAssetPath(pathName));
        }

        #endregion
    }

    public class ScriptableObjectWindow : EditorWindow
    {
        private const float LabelWidth = 180f;

        private bool _initialFocusSet;

        private string _searchFilter = string.Empty;

        private string SearchFilter
        {
            get { return _searchFilter; }
            set
            {
                if (value != _searchFilter)
                    _searchResults = FindMatchingNames(value);
                _searchFilter = value;
            }
        }

        string[] _searchResults = { };
        int _selectedIndex;

        static Type[] _types;
        static string[] _scriptableObjectNames;

        [MenuItem("Assets/Create/ScriptableObject")]
        public static void Open()
        {
            var types = TypeCache.GetTypesDerivedFrom<ScriptableObject>();
            _types = types.Where(ShouldIncludeType).ToArray();
            _scriptableObjectNames = _types.Select(x => x.FullName).ToArray();
            GetWindow<ScriptableObjectWindow>(true, "Create a new ScriptableObject", true).ShowPopup();
        }

        static bool ShouldIncludeType(Type type)
        {
            if (type.IsSubclassOf(typeof(EditorWindow))
                || type.IsSubclassOf(typeof(Editor)))
                return false;

            var fullName = type.FullName;
            return !fullName.StartsWith("Unity")
                   && !fullName.StartsWith("UnityEngine")
                   && !fullName.StartsWith("UnityEditor")
                   && !fullName.StartsWith("Sirenix")
                   && !fullName.StartsWith("DG")
                   && !fullName.StartsWith("E7")
                   && !fullName.StartsWith("Heureka")
                   && !fullName.StartsWith("Google")
                   && !fullName.StartsWith("Zenject")
                   && !fullName.StartsWith("TouchScript")
                   && !fullName.StartsWith("ScriptableObjectFactory")
                   && !fullName.StartsWith("Packages");
        }

        private void Awake()
        {
            _searchResults = FindMatchingNames(string.Empty);
        }

        public void OnGUI()
        {
            DrawAssemblySelection();
            DrawSearch();
            DrawSelectionPopup();
            DrawCreateButton();
        }

        private void DrawAssemblySelection()
        {
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            _searchResults = FindMatchingNames(_searchFilter);
            GUILayout.EndHorizontal();
        }

        private void DrawSearch()
        {
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(LabelWidth));

            GUI.SetNextControlName("SearchField");
            SearchFilter = EditorGUILayout.TextField(SearchFilter);
            GUILayout.EndHorizontal();

            if (!_initialFocusSet)
            {
                _initialFocusSet = true;
                EditorGUI.FocusTextInControl("SearchField");
            }
        }

        private Vector2 _scrollPos;

        private void DrawSelectionPopup()
        {
            GUILayout.Space(6);

            using (var scope = new EditorGUILayout.ScrollViewScope(_scrollPos, EditorStyles.helpBox))
            {
                _selectedIndex = GUILayout.SelectionGrid(_selectedIndex, _searchResults, 1);
                _scrollPos = scope.scrollPosition;
            }
        }

        private void DrawCreateButton()
        {
            GUILayout.Space(6);
            if (GUILayout.Button("Create"))
            {
                var realIndex = Array.FindIndex(_scriptableObjectNames, n => n == _searchResults[_selectedIndex]);
                var asset = CreateInstance(_types[realIndex]);
                var fileName = _scriptableObjectNames[realIndex].Split('.').Last();

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    asset.GetInstanceID(),
                    CreateInstance<EndNameEdit>(),
                    fileName + ".asset",
                    AssetPreview.GetMiniThumbnail(asset),
                    null);

                Close();
            }
        }

        private string[] FindMatchingNames(string filter)
        {
            var selectedName = _searchResults.Length == 0
                ? string.Empty
                : _searchResults[_selectedIndex];

            var matchingNames = string.IsNullOrEmpty(filter)
                ? _scriptableObjectNames
                : _scriptableObjectNames.Where(name => IsMatch(name, filter)).ToArray();

            var newIndex = Array.FindIndex(matchingNames, matchingName => matchingName == selectedName);
            _selectedIndex = newIndex < 0
                ? 0
                : newIndex;

            return matchingNames;
        }

        static bool IsMatch(string text, string searchTerm)
        {
            try
            {
                var splitSearchTerms = searchTerm.Split(null);
                return splitSearchTerms.All(term =>
                {
                    var regex = new Regex(term, RegexOptions.IgnoreCase);
                    return regex.Match(text).Success;
                });
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}