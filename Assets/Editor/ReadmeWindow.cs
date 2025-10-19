// Assets/Editor/ReadmeWindow.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public sealed class ReadmeWindow : EditorWindow
{
    private const string README_PATH = "Assets/README.txt";
    private const string LOGO_PATH = "Assets/Editor/ReadmeLogo.png";
    private const string START_SCENE = "Assets/Scenes/NavMeshTutorialStart.unity";
    private const string FINAL_SCENE = "Assets/Scenes/NavMeshTutorialFinal.unity";
    private const string SESSION_KEY = "ReadmeWindowShownThisSession";

    private string _text = "";
    private Texture2D _logo = null;
    private Vector2 _scroll;

    [InitializeOnLoadMethod]
    private static void AutoShowOnLoad()
    {
        if (BuildPipeline.isBuildingPlayer) return;
        if (SessionState.GetBool(SESSION_KEY, false)) return;

        SessionState.SetBool(SESSION_KEY, true);
        EditorApplication.delayCall += () =>
        {
            var w = CreateInstance<ReadmeWindow>();
            w.titleContent = new GUIContent("Project README");
            w.minSize = new Vector2(520, 420);
            w.ShowUtility();
        };
    }

    [MenuItem("Endasil/Project README")]
    private static void ShowManual()
    {
        var w = GetWindow<ReadmeWindow>(utility: true, title: "Project README", focus: true);
        w.minSize = new Vector2(520, 420);
        w.Show();
    }

    private void OnEnable()
    {
        if (File.Exists(README_PATH))
            _text = File.ReadAllText(README_PATH);

        _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LOGO_PATH);
    }

    private void OnGUI()
    {
        var r = EditorGUILayout.GetControlRect(false, 64);
        r.width = 64;
        if (_logo != null) GUI.DrawTexture(r, _logo, ScaleMode.ScaleToFit);

        GUILayout.Space(4);
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Welcome", EditorStyles.boldLabel);
            GUILayout.Space(2);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // word-wrapped, respects spaces; use textArea font for steadier spacing
            var style = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true, font = EditorStyles.textArea.font };

            string[] lines = string.IsNullOrEmpty(_text)
                ? new[] { "README.txt not found at " + README_PATH }
                : _text.Replace("\r\n", "\n").Split('\n');

            foreach (string rawLine in lines)
            {
                // preserve leading spaces for indentation
                int leadingSpaces = 0;
                while (leadingSpaces < rawLine.Length && rawLine[leadingSpaces] == ' ') leadingSpaces++;

                string line = rawLine; // no Trim()

                // simple [Name](URL) detection - inline only when the whole line is a single link
                int openBracket = line.IndexOf('[');
                int closeBracket = line.IndexOf(']');
                int openParen = line.IndexOf('(');
                int closeParen = line.LastIndexOf(')');

                bool looksLikeSingleLink =
                    openBracket >= 0 &&
                    closeBracket > openBracket &&
                    openParen == closeBracket + 1 &&
                    closeParen > openParen &&
                    openBracket == leadingSpaces && // link starts after indentation
                    closeParen == line.Length - 1;  // line is only that link (plus indent)

                if (looksLikeSingleLink)
                {
                    // indent visually the same number of spaces (approx 8 px per space)
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(leadingSpaces * 8);

                    string linkText = line.Substring(openBracket + 1, closeBracket - openBracket - 1);
                    string url = line.Substring(openParen + 1, closeParen - openParen - 1);

                    if (EditorGUILayout.LinkButton(linkText)) Application.OpenURL(url);
                    GUILayout.EndHorizontal();
                }
                else
                {
                    // regular text - respects indentation because we kept spaces
                    EditorGUILayout.LabelField(line, style);
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!File.Exists(START_SCENE)))
                {
                    if (GUILayout.Button("Open NavMeshTutorialStart.unity"))
                        EditorSceneManager.OpenScene(START_SCENE, OpenSceneMode.Single);
                }

                using (new EditorGUI.DisabledScope(!File.Exists(FINAL_SCENE)))
                {
                    if (GUILayout.Button("Open NavMeshTutorialFinal.unity"))
                        EditorSceneManager.OpenScene(FINAL_SCENE, OpenSceneMode.Single);
                }
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Close")) Close();
        }
    }
}
