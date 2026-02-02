using UnityEngine;
using System;

namespace ForkTrack.API
{
    /// <summary>
    /// Manages secure storage and retrieval of ForkTrack API tokens
    /// Tokens are stored in EditorPrefs (Editor) or PlayerPrefs (Runtime)
    /// </summary>
    public static class TokenAuthenticator
    {
        private const string TOKEN_KEY = "ForkTrack_APIToken";
        private const string API_URL_KEY = "ForkTrack_APIURL";
        private const string DEFAULT_API_URL = "https://forktrack.pixfork.com";

        #region Token Management

        /// <summary>
        /// Saves the API token securely
        /// </summary>
        public static void SaveToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[TokenAuthenticator] Attempted to save empty token");
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(TOKEN_KEY, token);
#else
            PlayerPrefs.SetString(TOKEN_KEY, token);
            PlayerPrefs.Save();
#endif
            Debug.Log("[TokenAuthenticator] API token saved");
        }

        /// <summary>
        /// Retrieves the stored API token
        /// </summary>
        public static string GetToken()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetString(TOKEN_KEY, "");
#else
            return PlayerPrefs.GetString(TOKEN_KEY, "");
#endif
        }

        /// <summary>
        /// Checks if a valid token is stored
        /// </summary>
        public static bool HasToken()
        {
            string token = GetToken();
            return !string.IsNullOrEmpty(token);
        }

        /// <summary>
        /// Clears the stored API token
        /// </summary>
        public static void ClearToken()
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.DeleteKey(TOKEN_KEY);
#else
            PlayerPrefs.DeleteKey(TOKEN_KEY);
            PlayerPrefs.Save();
#endif
            Debug.Log("[TokenAuthenticator] API token cleared");
        }

        #endregion

        #region API URL Management

        /// <summary>
        /// Saves the API base URL
        /// </summary>
        public static void SaveAPIUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                url = DEFAULT_API_URL;
            }

#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(API_URL_KEY, url);
#else
            PlayerPrefs.SetString(API_URL_KEY, url);
            PlayerPrefs.Save();
#endif
        }

        /// <summary>
        /// Retrieves the stored API base URL
        /// </summary>
        public static string GetAPIUrl()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetString(API_URL_KEY, DEFAULT_API_URL);
#else
            return PlayerPrefs.GetString(API_URL_KEY, DEFAULT_API_URL);
#endif
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a configured API client instance
        /// </summary>
        public static ForkTrackAPIClient CreateClient()
        {
            string token = GetToken();
            string apiUrl = GetAPIUrl();

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[TokenAuthenticator] No API token configured. Use TokenAuthenticator.SaveToken() first.");
                return null;
            }

            return new ForkTrackAPIClient(token, apiUrl);
        }

        /// <summary>
        /// Validates the stored token by making an API call
        /// </summary>
        public static void ValidateStoredToken(MonoBehaviour coroutineRunner, Action<bool> onComplete)
        {
            var client = CreateClient();
            if (client == null)
            {
                onComplete?.Invoke(false);
                return;
            }

            coroutineRunner.StartCoroutine(client.ValidateToken(onComplete));
        }

        #endregion
    }
}
