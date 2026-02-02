using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Net;
using System.Text;

namespace ForkTrack.API
{
    /// <summary>
    /// HTTP client for communicating with the ForkTrack backend API
    /// Handles authentication, graph fetching, and version checking
    /// </summary>
    public class ForkTrackAPIClient
    {
        private const string DEFAULT_API_URL = "https://forktrack.pixfork.com";

        private readonly string apiBaseUrl;
        private readonly string apiToken;

        public ForkTrackAPIClient(string apiToken, string apiBaseUrl = DEFAULT_API_URL)
        {
            this.apiToken = apiToken;
            this.apiBaseUrl = apiBaseUrl.TrimEnd('/');

            {
                // Only use Tls12 - Tls13 isn't available in this .NET version
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Temporarily add this to test if it's a certificate issue
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, cert, chain, sslPolicyErrors) => true;
            }
        }

        #region Graph API

        /// <summary>
        /// Fetches a graph by ID from the ForkTrack backend.
        /// Transforms the API response into ForkTrack JSON format with schemaVersion.
        /// </summary>
        public IEnumerator FetchGraph(string graphId, Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{apiBaseUrl}/api/graphs/{graphId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new AcceptAllCertificatesHandler();
                request.SetRequestHeader("Authorization", $"Bearer {apiToken}");
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // API returns: { id, name, graph_data: { nodes, edges, ... } }
                        // We need to transform to: { schemaVersion, nodes, edges, ... }
                        string json = request.downloadHandler.text;

                        // Extract graph_data from the response and add schemaVersion
                        // This is more reliable than re-serializing parsed objects
                        string graphJson = ExtractAndTransformGraphData(json);
                        if (graphJson == null)
                        {
                            onError?.Invoke("Invalid API response: missing or invalid graph_data");
                            yield break;
                        }

                        onSuccess?.Invoke(graphJson);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse graph: {e.Message}");
                    }
                }
                else
                {
                    string errorMsg = $"Failed to fetch graph: {request.error} (Status: {request.responseCode})";
                    Debug.LogError($"[ForkTrackAPIClient] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Extracts graph_data from API response and transforms it to ForkTrack JSON format
        /// by adding schemaVersion. Uses string manipulation to preserve the exact JSON structure.
        /// </summary>
        private string ExtractAndTransformGraphData(string apiResponseJson)
        {
            // Find the start of graph_data object
            const string graphDataKey = "\"graph_data\":";
            int keyIndex = apiResponseJson.IndexOf(graphDataKey);
            if (keyIndex < 0)
                return null;

            int objectStart = keyIndex + graphDataKey.Length;

            // Skip whitespace
            while (objectStart < apiResponseJson.Length && char.IsWhiteSpace(apiResponseJson[objectStart]))
                objectStart++;

            if (objectStart >= apiResponseJson.Length || apiResponseJson[objectStart] != '{')
                return null;

            // Find the matching closing brace
            int braceCount = 1;
            int objectEnd = objectStart + 1;
            bool inString = false;
            char prevChar = '\0';

            while (objectEnd < apiResponseJson.Length && braceCount > 0)
            {
                char c = apiResponseJson[objectEnd];

                // Track string state (ignore braces inside strings)
                if (c == '"' && prevChar != '\\')
                    inString = !inString;

                if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                }

                prevChar = c;
                objectEnd++;
            }

            if (braceCount != 0)
                return null;

            // Extract the graph_data content (without the outer braces)
            string graphDataContent = apiResponseJson.Substring(objectStart + 1, objectEnd - objectStart - 2);

            // Build the final JSON with schemaVersion prepended
            var sb = new StringBuilder();
            sb.Append("{\"schemaVersion\":\"1.2.0\",\"version\":{\"major\":1,\"minor\":0},");
            sb.Append(graphDataContent);
            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// Fetches graph version metadata without downloading full graph data
        /// </summary>
        public IEnumerator FetchGraphVersion(string graphId, Action<GraphVersionInfo> onSuccess, Action<string> onError)
        {
            string url = $"{apiBaseUrl}/api/graphs/{graphId}/version";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new AcceptAllCertificatesHandler();
                request.SetRequestHeader("Authorization", $"Bearer {apiToken}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var versionInfo = JsonUtility.FromJson<GraphVersionInfo>(request.downloadHandler.text);
                        onSuccess?.Invoke(versionInfo);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse version info: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Failed to fetch version: {request.error}");
                }
            }
        }

        /// <summary>
        /// Lists all graphs accessible with the current API token
        /// </summary>
        public IEnumerator ListGraphs(Action<GraphListResponse> onSuccess, Action<string> onError)
        {
            string url = $"{apiBaseUrl}/api/graphs";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.certificateHandler = new AcceptAllCertificatesHandler();
                request.SetRequestHeader("Authorization", $"Bearer {apiToken}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        GraphListResponse response;

                        // Handle both array format [...] and object format { graphs: [...] }
                        if (json.TrimStart().StartsWith("["))
                        {
                            // Server returns raw array - wrap it for JsonUtility
                            json = "{\"graphs\":" + json + "}";
                        }

                        response = JsonUtility.FromJson<GraphListResponse>(json);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse graph list: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Failed to list graphs: {request.error}");
                }
            }
        }

        #endregion

        #region Token Validation

        /// <summary>
        /// Validates the API token by attempting to fetch user info
        /// </summary>
        public IEnumerator ValidateToken(Action<bool> onComplete)
        {
            string url = $"{apiBaseUrl}/api/tokens/validate";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {

                Debug.Log($"sending request to {url}");
                request.downloadHandler = new DownloadHandlerBuffer();
                request.uploadHandler = new UploadHandlerRaw(new byte[0]);
                request.SetRequestHeader("Authorization", $"Bearer {apiToken}");
                //request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                bool isValid = request.result == UnityWebRequest.Result.Success;

                if (!isValid)
                {
                    Debug.LogError($"[ForkTrackAPIClient] Token validation failed: {request.error} (Status: {request.responseCode})");
                }
                else
                {
                    Debug.Log($"[ForkTrackAPIClient] Token validation successful!");
                }

                onComplete?.Invoke(isValid);
            }
        }

        #endregion

        #region Data Models

        [Serializable]
        public class GraphVersionInfo
        {
            public string graphId;
            public string version;
            public long lastModified;
            public string modifiedBy;
        }

        [Serializable]
        public class GraphListResponse
        {
            public GraphMetadata[] graphs;
        }

        [Serializable]
        public class GraphMetadata
        {
            public string id;
            public string name;
            public string description;
            public string owner_id;
            public bool is_public;
            public string updated_at;
            public string created_at;
            public int version_major;
            public int version_minor;
        }

        #endregion

        #region API Response Models

        /// <summary>
        /// Wrapper for API response containing graph_data
        /// API returns: { id, name, graph_data: { nodes, edges, ... } }
        /// </summary>
        [Serializable]
        public class GraphAPIResponse
        {
            public string id;
            public string name;
            public GraphData graph_data;
        }

        /// <summary>
        /// The actual graph data from API response
        /// Uses API-specific wrapper classes that match the JSON structure
        /// </summary>
        [Serializable]
        public class GraphData
        {
            public APINode[] nodes;
            public APIEdge[] edges;
            public APICategory[] categories;
            public APIGroup[] groups;
            public APIObject[] objects;
            public APIAction[] actions;
            public APIVariable[] variables;
        }

        // API-specific wrapper classes that match the JSON structure from the server
        // These have nested "data" objects unlike the Core classes

        [Serializable]
        public class APINode
        {
            public string id;
            public APIPosition position;
            public string type;
            public APINodeData data;
        }

        [Serializable]
        public class APIPosition
        {
            public float x;
            public float y;
        }

        [Serializable]
        public class APINodeData
        {
            public string objectId;
            public string actionId;
            public string title;
            public string description;
            public string category;
            public string groupId;
            public bool autoCompleteOnUnlock;
            public string music;
            public APINote[] notes;
            public APITodo[] todos;
            public APICustomEvent[] customEvents;
            public APIVariableAction[] variableActions;
        }

        [Serializable]
        public class APINote
        {
            public string text;
            public string trigger;
        }

        [Serializable]
        public class APITodo
        {
            public string title;
            public string description;
            public string responsible;
            public bool resolved;
        }

        [Serializable]
        public class APICustomEvent
        {
            public string propertyId;
            public string trigger;
            public string[] values;
            public float weight;
            public float delay;
            public float amount;
            public string text;  // Optional text/note associated with the event
        }

        [Serializable]
        public class APIVariableAction
        {
            public string id;
            public string variableId;
            public string trigger;
            public string operation;
            public string value;
        }

        [Serializable]
        public class APIEdge
        {
            public string id;
            public string sourceNodeId;
            public string targetNodeId;
            public APIEdgeData data;
        }

        [Serializable]
        public class APIEdgeData
        {
            public string requiredState;
            public string condition;
            public APIVariableCondition[] variableConditions;
        }

        [Serializable]
        public class APIVariableCondition
        {
            public string id;
            public string variableId;
            public string @operator;
            public string value;
        }

        [Serializable]
        public class APICategory
        {
            public string id;
            public string name;
            public string color;
        }

        [Serializable]
        public class APIGroup
        {
            public string id;
            public string name;
            public string color;
            public APIPosition position;
            public APISize size;
        }

        [Serializable]
        public class APISize
        {
            public float width;
            public float height;
        }

        [Serializable]
        public class APIObject
        {
            public string id;
            public string name;
            public string category;
        }

        [Serializable]
        public class APIAction
        {
            public string id;
            public string name;
        }

        [Serializable]
        public class APIVariable
        {
            public string id;
            public string name;
            public string type;
            public string defaultValue;
        }

        #endregion

        #region JSON Serialization Wrappers

        // JsonUtility requires wrapper objects for array serialization
        [Serializable]
        private class NodeArrayWrapper { public APINode[] items; }
        [Serializable]
        private class EdgeArrayWrapper { public APIEdge[] items; }
        [Serializable]
        private class CategoryArrayWrapper { public APICategory[] items; }
        [Serializable]
        private class GroupArrayWrapper { public APIGroup[] items; }
        [Serializable]
        private class ObjectArrayWrapper { public APIObject[] items; }
        [Serializable]
        private class ActionArrayWrapper { public APIAction[] items; }
        [Serializable]
        private class VariableArrayWrapper { public APIVariable[] items; }

        #endregion
    }
}
