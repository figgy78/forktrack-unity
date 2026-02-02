using UnityEngine;
using UnityEditor;
using ForkTrack.API;
using System.Collections;
using System.Collections.Generic;

namespace ForkTrack.Editor
{
    /// <summary>
    /// Editor window for managing ForkTrack API settings and testing connectivity
    /// </summary>
    public class ForkTrackSettingsWindow : EditorWindow
    {
        private string apiToken = "";
        private string apiUrl = "";
        private string testGraphId = "";

        private bool isValidatingToken = false;
        private bool tokenValidationResult = false;
        private string validationMessage = "";

        private bool isFetchingGraphs = false;
        private List<GraphInfo> availableGraphs = new List<GraphInfo>();
        private Vector2 graphListScroll;

        private GUIStyle headerStyle;
        private GUIStyle successStyle;
        private GUIStyle errorStyle;
        private GUIStyle sectionStyle;

        [System.Serializable]
        private class GraphInfo
        {
            public string id;
            public string name;
            public string version;
        }

        [MenuItem("Tools/ForkTrack/API Settings", priority = 1)]
        public static void ShowWindow()
        {
            ForkTrackSettingsWindow window = GetWindow<ForkTrackSettingsWindow>("ForkTrack API");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Load existing settings
            apiToken = TokenAuthenticator.GetToken();
            apiUrl = TokenAuthenticator.GetAPIUrl();
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 10)
                };
            }

            if (successStyle == null)
            {
                successStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
            }

            if (errorStyle == null)
            {
                errorStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.8f, 0.2f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("ForkTrack API Configuration", headerStyle);
            EditorGUILayout.Space(5);

            // API Configuration Section
            DrawAPIConfigurationSection();

            EditorGUILayout.Space(10);

            // Token Status Section
            DrawTokenStatusSection();

            EditorGUILayout.Space(10);

            // Graph Browser Section
            DrawGraphBrowserSection();

            EditorGUILayout.Space(10);

            // Quick Links Section
            DrawQuickLinksSection();
        }

        private void DrawAPIConfigurationSection()
        {
            EditorGUILayout.BeginVertical(sectionStyle);

            EditorGUILayout.LabelField("API Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // API URL
            EditorGUILayout.LabelField("API URL:", EditorStyles.miniLabel);
            apiUrl = EditorGUILayout.TextField(apiUrl);
            if (GUILayout.Button("Reset to Default", GUILayout.Width(150)))
            {
                apiUrl = "https://forktrack.pixfork.com";
            }

            EditorGUILayout.Space(5);

            // API Token
            EditorGUILayout.LabelField("API Token:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();

            // Password field for token
            string displayToken = string.IsNullOrEmpty(apiToken) ? "" : new string('*', apiToken.Length);
            string newToken = EditorGUILayout.PasswordField(displayToken);

            if (newToken != displayToken)
            {
                apiToken = newToken;
            }

            if (GUILayout.Button("Paste", GUILayout.Width(60)))
            {
                apiToken = EditorGUIUtility.systemCopyBuffer;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Action Buttons
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !string.IsNullOrEmpty(apiToken);
            if (GUILayout.Button("Save Settings"))
            {
                TokenAuthenticator.SaveToken(apiToken);
                TokenAuthenticator.SaveAPIUrl(apiUrl);
                EditorUtility.DisplayDialog("Success", "API settings saved successfully!", "OK");
            }
            GUI.enabled = true;

            if (GUILayout.Button("Clear Token"))
            {
                if (EditorUtility.DisplayDialog("Clear Token", "Are you sure you want to clear the stored API token?", "Yes", "No"))
                {
                    TokenAuthenticator.ClearToken();
                    apiToken = "";
                    validationMessage = "";
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTokenStatusSection()
        {
            EditorGUILayout.BeginVertical(sectionStyle);

            EditorGUILayout.LabelField("Token Status", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            bool hasToken = TokenAuthenticator.HasToken();
            string statusText = hasToken ? "Token Configured" : "No Token Set";
            GUIStyle statusStyle = hasToken ? successStyle : errorStyle;
            EditorGUILayout.LabelField($"Status: {statusText}", statusStyle);

            EditorGUILayout.Space(5);

            if (!string.IsNullOrEmpty(validationMessage))
            {
                GUIStyle messageStyle = tokenValidationResult ? successStyle : errorStyle;
                EditorGUILayout.LabelField(validationMessage, messageStyle);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = hasToken && !isValidatingToken;
            if (GUILayout.Button(isValidatingToken ? "Validating..." : "Validate Token"))
            {
                ValidateToken();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawGraphBrowserSection()
        {
            EditorGUILayout.BeginVertical(sectionStyle);

            EditorGUILayout.LabelField("Graph Browser", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            GUI.enabled = TokenAuthenticator.HasToken() && !isFetchingGraphs;
            if (GUILayout.Button(isFetchingGraphs ? "Loading..." : "Fetch Available Graphs"))
            {
                FetchGraphList();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(5);

            if (availableGraphs.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {availableGraphs.Count} graphs:", EditorStyles.miniLabel);

                graphListScroll = EditorGUILayout.BeginScrollView(graphListScroll, GUILayout.Height(150));

                foreach (var graph in availableGraphs)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(graph.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Version: {graph.version}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"ID: {graph.id}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Copy ID", GUILayout.Width(80)))
                    {
                        EditorGUIUtility.systemCopyBuffer = graph.id;
                        Debug.Log($"Copied graph ID: {graph.id}");
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickLinksSection()
        {
            EditorGUILayout.BeginVertical(sectionStyle);

            EditorGUILayout.LabelField("Quick Links", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Open ForkTrack Web App"))
            {
                Application.OpenURL("https://forktrack.pixfork.com");
            }

            if (GUILayout.Button("Generate API Token"))
            {
                Application.OpenURL("https://forktrack.pixfork.com/app");
            }

            if (GUILayout.Button("View Documentation"))
            {
                Application.OpenURL("https://github.com/forktrack/unity-package");
            }

            EditorGUILayout.EndVertical();
        }

        private void ValidateToken()
        {
            Debug.Log("[ForkTrackSettingsWindow] ValidateToken button clicked");
            isValidatingToken = true;
            validationMessage = "Validating...";

            var apiClient = TokenAuthenticator.CreateClient();
            Debug.Log($"[ForkTrackSettingsWindow] API Client created: {apiClient != null}");

            if (apiClient == null)
            {
                validationMessage = "✗ Failed to create API client";
                isValidatingToken = false;
                return;
            }

            Debug.Log("[ForkTrackSettingsWindow] Starting coroutine...");
            EditorCoroutineUtility.StartCoroutine(apiClient.ValidateToken(isValid =>
            {
                Debug.Log($"[ForkTrackSettingsWindow] Validation completed: {isValid}");
                isValidatingToken = false;
                tokenValidationResult = isValid;

                if (isValid)
                {
                    validationMessage = "✓ Token is valid and active";
                }
                else
                {
                    validationMessage = "✗ Token is invalid or expired";
                }

                Repaint();
            }), this);
        }

        private void FetchGraphList()
        {
            isFetchingGraphs = true;
            availableGraphs.Clear();

            var apiClient = TokenAuthenticator.CreateClient();

            EditorCoroutineUtility.StartCoroutine(apiClient.ListGraphs(
                response =>
                {
                    isFetchingGraphs = false;

                    if (response != null && response.graphs != null)
                    {
                        foreach (var graph in response.graphs)
                        {
                            availableGraphs.Add(new GraphInfo
                            {
                                id = graph.id,
                                name = graph.name,
                                version = $"v{graph.version_major}.{graph.version_minor}"
                            });
                        }

                        Debug.Log($"Fetched {availableGraphs.Count} graphs");
                    }

                    Repaint();
                },
                error =>
                {
                    isFetchingGraphs = false;
                    EditorUtility.DisplayDialog("Error", $"Failed to fetch graphs:\n{error}", "OK");
                    Repaint();
                }
            ), this);
        }
    }

    /// <summary>
    /// Utility class for running coroutines in the editor
    /// </summary>
    public static class EditorCoroutineUtility
    {
        public static void StartCoroutine(IEnumerator routine, EditorWindow window)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine, window);
            coroutine.Start();
        }

        private class EditorCoroutine
        {
            private readonly IEnumerator routine;
            private readonly EditorWindow window;
            private UnityEngine.Networking.UnityWebRequestAsyncOperation currentAsyncOp;

            public EditorCoroutine(IEnumerator routine, EditorWindow window)
            {
                this.routine = routine;
                this.window = window;
            }

            public void Start()
            {
                EditorApplication.update += Update;
            }

            private void Update()
            {
                try
                {
                    // If we're waiting for an async operation, check if it's done
                    if (currentAsyncOp != null)
                    {
                        if (!currentAsyncOp.isDone)
                        {
                            return; // Still waiting
                        }
                        currentAsyncOp = null; // Done, continue coroutine
                    }

                    // Move to next yield
                    if (!routine.MoveNext())
                    {
                        // Coroutine finished
                        EditorApplication.update -= Update;
                        if (window != null)
                        {
                            window.Repaint();
                        }
                        return;
                    }

                    // Check if the current yield is a UnityWebRequestAsyncOperation
                    if (routine.Current is UnityEngine.Networking.UnityWebRequestAsyncOperation asyncOp)
                    {
                        currentAsyncOp = asyncOp;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Editor coroutine error: {e.Message}\n{e.StackTrace}");
                    EditorApplication.update -= Update;
                }
            }
        }
    }
}
