using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Kruzic.GameSDK
{
    /// <summary>
    /// SDK za integraciju igara sa Kruzic platformom.
    /// Omogucava pristup korisnickim podacima i cuvanje podataka igre.
    /// </summary>
    public class KruzicSDK : MonoBehaviour
    {
        private static KruzicSDK _instance;
        public static KruzicSDK Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("KruzicSDK");
                    _instance = go.AddComponent<KruzicSDK>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #region JavaScript Interop

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void KruzicSendMessage(string type, int requestId, string payload);

        [DllImport("__Internal")]
        private static extern void KruzicNotifyReady();

        [DllImport("__Internal")]
        private static extern bool KruzicIsInIframe();
#else
        private static void KruzicSendMessage(string type, int requestId, string payload)
        {
#if UNITY_EDITOR
            // Ako smo u Editoru koristi mock server
            Kruzic.GameSDK.Editor.KruzicMockServer.SendMessage(type, requestId, payload);
#else
            Logger.Log($"[Mock] SendMessage: {type}, {requestId}, {payload}");
#endif
        }

        private static void KruzicNotifyReady()
        {
            Logger.Log("[Mock] NotifyReady called");
        }

        private static bool KruzicIsInIframe()
        {
            return false;
        }
#endif

        #endregion

        #region Request Management

        private int _nextRequestId = 1;
        private readonly Dictionary<int, Action<SDKResponse>> _pendingRequests = new Dictionary<int, Action<SDKResponse>>();
        private const float REQUEST_TIMEOUT = 10f;

        private int SendRequest(string type, object payload, Action<SDKResponse> callback)
        {
            int requestId = _nextRequestId++;
            string payloadJson = payload != null ? JsonUtility.ToJson(payload) : "";

            _pendingRequests[requestId] = callback;
            StartCoroutine(TimeoutRequest(requestId));

            KruzicSendMessage(type, requestId, payloadJson);

            return requestId;
        }

        private IEnumerator TimeoutRequest(int requestId)
        {
            yield return new WaitForSeconds(REQUEST_TIMEOUT);

            if (_pendingRequests.ContainsKey(requestId))
            {
                var callback = _pendingRequests[requestId];
                _pendingRequests.Remove(requestId);

                callback?.Invoke(new SDKResponse
                {
                    success = false,
                    error = "Request timeout"
                });
            }
        }

        // Called from JavaScript
        public void OnMessageReceived(string jsonResponse)
        {
            try
            {
                var response = JsonUtility.FromJson<SDKResponse>(jsonResponse);

                if (_pendingRequests.ContainsKey(response.requestId))
                {
                    var callback = _pendingRequests[response.requestId];
                    _pendingRequests.Remove(response.requestId);
                    callback?.Invoke(response);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to parse response: {e.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Proverava da li igra radi unutar Kruzic iframe-a.
        /// </summary>
        public bool IsInIframe()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return KruzicIsInIframe();
#else
            return false;
#endif
        }

        /// <summary>
        /// Obaveštava platformu da je igra učitana i spremna.
        /// Pozovite ovu metodu kada se igra inicijalizuje.
        /// </summary>
        public void Ready()
        {
            Logger.Log("Game ready");
            KruzicNotifyReady();
        }

        /// <summary>
        /// Proverava da li je korisnik prijavljen.
        /// </summary>
        public void IsSignedIn(Action<bool> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendRequest("IS_USER_SIGNED_IN", null, (response) =>
            {
                if (response.success)
                {
                    var data = JsonUtility.FromJson<SignedInResponse>(response.data);
                    callback?.Invoke(data.signedIn);
                }
                else
                {
                    Logger.Error($"IsSignedIn failed: {response.error}");
                    callback?.Invoke(false);
                }
            });
#else
            // In editor, simulate signed in user
            callback?.Invoke(true);
#endif
        }

        /// <summary>
        /// Vraća detalje o trenutnom korisniku.
        /// </summary>
        public void GetUserDetails(Action<UserDetails> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendRequest("GET_USER_DETAILS", null, (response) =>
            {
                if (response.success && !string.IsNullOrEmpty(response.data))
                {
                    var user = JsonUtility.FromJson<UserDetails>(response.data);
                    callback?.Invoke(user);
                }
                else
                {
                    if (!response.success)
                    {
                        Logger.Error($"GetUserDetails failed: {response.error}");
                    }
                    callback?.Invoke(null);
                }
            });
#else
            // In editor, return mock user
            callback?.Invoke(new UserDetails
            {
                id = "dev-user",
                name = "Dev User",
                image = null
            });
#endif
        }

        /// <summary>
        /// Vraća ID trenutnog korisnika.
        /// </summary>
        public void GetUserId(Action<string> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendRequest("GET_USER_ID", null, (response) =>
            {
                if (response.success && !string.IsNullOrEmpty(response.data))
                {
                    var data = JsonUtility.FromJson<UserIdResponse>(response.data);
                    callback?.Invoke(data.userId);
                }
                else
                {
                    if (!response.success)
                    {
                        Logger.Error($"GetUserId failed: {response.error}");
                    }
                    callback?.Invoke(null);
                }
            });
#else
            callback?.Invoke("dev-user");
#endif
        }

        /// <summary>
        /// Učitava sačuvanu vrednost za trenutnog korisnika.
        /// </summary>
        public void GetData<T>(string key, Action<T> callback) where T : class
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var payload = new GetDataPayload { key = key };
            SendRequest("GET_USER_DATA", payload, (response) =>
            {
                if (response.success && !string.IsNullOrEmpty(response.data))
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<DataWrapper>(response.data);
                        if (!string.IsNullOrEmpty(wrapper.value))
                        {
                            var data = JsonUtility.FromJson<T>(wrapper.value);
                            callback?.Invoke(data);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to parse data: {e.Message}");
                    }
                }
                else if (!response.success)
                {
                    Logger.Error($"GetData failed: {response.error}");
                }
                callback?.Invoke(null);
            });
#else
            // In editor, use PlayerPrefs
            string json = PlayerPrefs.GetString($"kruzic_dev_{key}", null);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<T>(json);
                    callback?.Invoke(data);
                    return;
                }
                catch { }
            }
            callback?.Invoke(null);
#endif
        }

        /// <summary>
        /// Čuva vrednost za trenutnog korisnika.
        /// </summary>
        public void SetData<T>(string key, T value, Action<bool> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string valueJson = JsonUtility.ToJson(value);
            var payload = new SetDataPayload { key = key, value = valueJson };
            SendRequest("SET_USER_DATA", payload, (response) =>
            {
                if (!response.success)
                {
                    Logger.Error($"SetData failed: {response.error}");
                }
                callback?.Invoke(response.success);
            });
#else
            // In editor, use PlayerPrefs
            string json = JsonUtility.ToJson(value);
            PlayerPrefs.SetString($"kruzic_dev_{key}", json);
            PlayerPrefs.Save();
            callback?.Invoke(true);
#endif
        }

        /// <summary>
        /// Briše sačuvanu vrednost.
        /// </summary>
        public void DeleteData(string key, Action<bool> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var payload = new GetDataPayload { key = key };
            SendRequest("DELETE_USER_DATA", payload, (response) =>
            {
                if (!response.success)
                {
                    Logger.Error($"DeleteData failed: {response.error}");
                }
                callback?.Invoke(response.success);
            });
#else
            PlayerPrefs.DeleteKey($"kruzic_dev_{key}");
            PlayerPrefs.Save();
            callback?.Invoke(true);
#endif
        }

        /// <summary>
        /// Vraća listu svih sačuvanih ključeva.
        /// </summary>
        public void ListData(Action<string[]> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SendRequest("LIST_USER_DATA", null, (response) =>
            {
                if (response.success && !string.IsNullOrEmpty(response.data))
                {
                    try
                    {
                        var list = JsonUtility.FromJson<DataListWrapper>(response.data);
                        callback?.Invoke(list.keys ?? new string[0]);
                        return;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to parse list: {e.Message}");
                    }
                }
                else if (!response.success)
                {
                    Logger.Error($"ListData failed: {response.error}");
                }
                callback?.Invoke(new string[0]);
            });
#else
            // In editor, we can't easily list PlayerPrefs keys
            callback?.Invoke(new string[0]);
#endif
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Čuva napredak igre.
        /// </summary>
        public void SaveProgress<T>(T progress, Action<bool> callback = null) where T : class
        {
            SetData("progress", progress, callback);
        }

        /// <summary>
        /// Učitava napredak igre.
        /// </summary>
        public void LoadProgress<T>(Action<T> callback) where T : class
        {
            GetData("progress", callback);
        }

        /// <summary>
        /// Čuva high score.
        /// </summary>
        public void SaveHighScore(int score, Action<bool> callback = null)
        {
            SetData("highscore", new HighScoreData { score = score }, callback);
        }

        /// <summary>
        /// Učitava high score.
        /// </summary>
        public void GetHighScore(Action<int> callback)
        {
            GetData<HighScoreData>("highscore", (data) =>
            {
                callback?.Invoke(data?.score ?? 0);
            });
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            // Initialize mock server in Editor
            Kruzic.GameSDK.Editor.KruzicMockServer.Init();
#endif
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion
    }

    #region Data Types

    [Serializable]
    public class UserDetails
    {
        public string id;
        public string name;
        public string image;
    }

    [Serializable]
    public class SDKResponse
    {
        public string type;
        public int requestId;
        public bool success;
        public string data;
        public string error;
    }

    [Serializable]
    internal class SignedInResponse
    {
        public bool signedIn;
    }

    [Serializable]
    internal class UserIdResponse
    {
        public string userId;
    }

    [Serializable]
    internal class GetDataPayload
    {
        public string key;
    }

    [Serializable]
    internal class SetDataPayload
    {
        public string key;
        public string value;
    }

    [Serializable]
    internal class DataWrapper
    {
        public string value;
    }

    [Serializable]
    internal class DataListWrapper
    {
        public string[] keys;
    }

    [Serializable]
    internal class HighScoreData
    {
        public int score;
    }

    #endregion

    #region Logger

    internal static class Logger
    {
        private const string PREFIX = "[Kruzic SDK]";

        public static void Log(string message)
        {
            Debug.Log($"{PREFIX} {message}");
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"{PREFIX} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{PREFIX} {message}");
        }
    }

    #endregion
}
