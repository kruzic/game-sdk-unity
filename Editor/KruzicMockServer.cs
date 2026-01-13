#if UNITY_EDITOR
using Kruzic.GameSDK;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Kruzic.GameSDK.Editor
{
    /// <summary>
    /// Mock server za testiranje Kruzic SDK-a u Unity Editoru bez WebGL build-a.
    /// Simulira odgovore Kruzic platforme korišćenjem PlayerPrefs za skladištenje.
    /// </summary>
    [InitializeOnLoad]
    public static class KruzicMockServer
    {
        public const string MOCK_DATA_KEY = "kruzicMockData";
        public const string MOCK_USER_ID_KEY = "kruzicMockId";
        public const string MOCK_USERNAME_KEY = "kruzicMockUsername";
        public const string MOCK_LOGGED_IN_KEY = "kruzicMockLoggedIn";
        public const string MOCK_AVATAR_KEY = "kruzicMockAvatar";

        private static Dictionary<string, MockDataEntry> mockData = new Dictionary<string, MockDataEntry>();
        private static bool isUserSignedIn = true;
        private static string userId = "dev-user";
        private static string username = "Dev User";
        private static string avatar = null;

        private static bool isInitialized = false;

        // Statički konstruktor - poziva se automatski kada se Editor učita
        static KruzicMockServer()
        {
            Init();
        }

        public static void Init()
        {
            if (isInitialized) return;
            isInitialized = true;

            LoadMockData();
            LoadMockUserSettings();

            // Registruj mock server sa KruzicSDK
            KruzicSDK.MockServerSendMessage = SendMessage;

            Debug.Log("[Kruzic SDK] Mock server initialized");
        }

        public static void UpdateMockData(List<MockDataEntry> entries)
        {
            mockData.Clear();
            foreach (var entry in entries)
            {
                mockData[entry.key] = entry;
            }

            Debug.Log($"[Kruzic SDK Mock] Data updated: {mockData.Count} entries");
        }

        public static void UpdateMockUser(bool signedIn, string id, string name, string avatarUrl)
        {
            isUserSignedIn = signedIn;
            userId = id;
            username = name;
            avatar = avatarUrl;

            Debug.Log($"[Kruzic SDK Mock] User settings updated: SignedIn={signedIn}, ID={id}, Name={name}");
        }

        private static void LoadMockData()
        {
            string json = PlayerPrefs.GetString(MOCK_DATA_KEY, "");

            if (!string.IsNullOrEmpty(json))
            {
                var container = JsonUtility.FromJson<MockDataContainer>(json);
                if (container.entries != null)
                {
                    mockData.Clear();
                    foreach (var entry in container.entries)
                    {
                        mockData[entry.key] = entry;
                    }

                    Debug.Log($"[Kruzic SDK] Mock server loaded {container.entries.Count} data entries");
                }
            }
        }

        private static void LoadMockUserSettings()
        {
            int loggedInValue = PlayerPrefs.GetInt(MOCK_LOGGED_IN_KEY, 1);
            isUserSignedIn = loggedInValue == 1;
            userId = PlayerPrefs.GetString(MOCK_USER_ID_KEY, "dev-user");
            username = PlayerPrefs.GetString(MOCK_USERNAME_KEY, "Dev User");
            avatar = PlayerPrefs.GetString(MOCK_AVATAR_KEY, "");

            if (string.IsNullOrEmpty(avatar))
            {
                avatar = null;
            }

            Debug.Log($"[Kruzic SDK Mock] LoadMockUserSettings: PlayerPrefs value={loggedInValue}, isUserSignedIn={isUserSignedIn}");
        }

        public static void SendMessage(string type, int requestId, string payload)
        {
            Debug.Log($"[Kruzic SDK Mock] SendMessage: {type}, RequestId: {requestId}");

            string responseData = null;
            bool success = true;
            string error = null;

            try
            {
                switch (type)
                {
                    case "IS_USER_SIGNED_IN":
                        responseData = HandleIsUserSignedIn();
                        break;

                    case "GET_USER_ID":
                        responseData = HandleGetUserId(out success, out error);
                        break;

                    case "GET_USER_DETAILS":
                        responseData = HandleGetUserDetails(out success, out error);
                        break;

                    case "GET_USER_DATA":
                        responseData = HandleGetUserData(payload, out success, out error);
                        break;

                    case "SET_USER_DATA":
                        responseData = HandleSetUserData(payload, out success, out error);
                        break;

                    case "LIST_USER_DATA":
                        responseData = HandleListUserData(out success, out error);
                        break;

                    case "DELETE_USER_DATA":
                        responseData = HandleDeleteUserData(payload, out success, out error);
                        break;

                    default:
                        success = false;
                        error = $"Unknown message type: {type}";
                        break;
                }
            }
            catch (System.Exception e)
            {
                success = false;
                error = $"Error: {e.Message}";
                Debug.LogError($"[Kruzic SDK Mock] {error}");
            }

            // Send response back
            SendResponse(requestId, success, responseData, error);
        }

        private static string HandleIsUserSignedIn()
        {
            // Reload from PlayerPrefs to get latest value
            LoadMockUserSettings();

            var response = new SignedInResponse { signedIn = isUserSignedIn };
            Debug.Log($"[Kruzic SDK Mock] IsUserSignedIn: {isUserSignedIn}");
            return JsonUtility.ToJson(response);
        }

        private static string HandleGetUserId(out bool success, out string error)
        {
            // Reload from PlayerPrefs to get latest value
            LoadMockUserSettings();

            success = true;
            error = null;

            if (!isUserSignedIn)
            {
                success = false;
                error = "User not signed in";
                return null;
            }

            var response = new UserIdResponse { userId = userId };
            return JsonUtility.ToJson(response);
        }

        private static string HandleGetUserDetails(out bool success, out string error)
        {
            // Reload from PlayerPrefs to get latest value
            LoadMockUserSettings();

            success = true;
            error = null;

            if (!isUserSignedIn)
            {
                success = false;
                error = "User not signed in";
                return null;
            }

            var user = new UserDetails
            {
                id = userId,
                name = username,
                image = avatar
            };

            return JsonUtility.ToJson(user);
        }

        private static string HandleGetUserData(string payload, out bool success, out string error)
        {
            success = true;
            error = null;

            // Reload settings to ensure we have latest values
            LoadMockUserSettings();
            LoadMockData();

            if (!isUserSignedIn)
            {
                success = false;
                error = "User not signed in";
                return null;
            }

            var request = JsonUtility.FromJson<GetDataPayload>(payload);

            if (mockData.TryGetValue(request.key, out MockDataEntry entry))
            {
                var wrapper = new DataWrapper { value = entry.value };
                Debug.Log($"[Kruzic SDK Mock] GetData '{request.key}': {entry.value}");
                return JsonUtility.ToJson(wrapper);
            }

            Debug.Log($"[Kruzic SDK Mock] GetData '{request.key}': not found");
            // Return null if key not found (SDK handles this)
            return null;
        }

        private static string HandleSetUserData(string payload, out bool success, out string error)
        {
            success = true;
            error = null;

            // Reload settings to ensure we have latest values
            LoadMockUserSettings();

            if (!isUserSignedIn)
            {
                success = false;
                error = "User not signed in";
                return null;
            }

            var request = JsonUtility.FromJson<SetDataPayload>(payload);

            var entry = new MockDataEntry
            {
                key = request.key,
                value = request.value
            };

            mockData[request.key] = entry;
            SaveMockDataToPrefs();

            Debug.Log($"[Kruzic SDK Mock] SetData '{request.key}': {request.value}");

            return JsonUtility.ToJson(new { success = true });
        }

        private static string HandleListUserData(out bool success, out string error)
        {
            // Reload settings to ensure we have latest values
            LoadMockUserSettings();
            LoadMockData();

            success = true;
            error = null;

            if (!isUserSignedIn)
            {
                success = false;
                error = "User not signed in";
                return null;
            }

            var keys = new List<string>(mockData.Keys);
            var response = new DataKeysResponse { keys = keys.ToArray() };
            return JsonUtility.ToJson(response);
        }

        private static string HandleDeleteUserData(string payload, out bool success, out string error)
        {
            // Reload settings to ensure we have latest values
            LoadMockUserSettings();
            LoadMockData();

            success = true;
            error = null;

            if (!isUserSignedIn)
            {
                success = false;
                error = "User not signed in";
                return null;
            }

            var request = JsonUtility.FromJson<GetDataPayload>(payload);

            if (mockData.ContainsKey(request.key))
            {
                mockData.Remove(request.key);
                SaveMockDataToPrefs();
                Debug.Log($"[Kruzic SDK Mock] DeleteData '{request.key}': deleted");
                return JsonUtility.ToJson(new { success = true });
            }

            success = false;
            error = "Key not found";
            return null;
        }

        private static void SaveMockDataToPrefs()
        {
            var entries = new List<MockDataEntry>(mockData.Values);
            var container = new MockDataContainer { entries = entries };
            string json = JsonUtility.ToJson(container);
            PlayerPrefs.SetString(MOCK_DATA_KEY, json);
            PlayerPrefs.Save();
        }

        private static void SendResponse(int requestId, bool success, string data, string error)
        {
            var response = new SDKResponse
            {
                type = "RESPONSE",
                requestId = requestId,
                success = success,
                data = data,
                error = error
            };

            string jsonResponse = JsonUtility.ToJson(response);

            Debug.Log($"[Kruzic SDK Mock] Response: RequestId: {requestId}, Success: {success}");

            // Send to SDK instance
            var instance = KruzicSDK.Instance;
            if (instance != null)
            {
                instance.OnMessageReceived(jsonResponse);
            }
        }

        #region Internal Data Models

        [System.Serializable]
        private class SignedInResponse
        {
            public bool signedIn;
        }

        [System.Serializable]
        private class UserIdResponse
        {
            public string userId;
        }

        [System.Serializable]
        private class GetDataPayload
        {
            public string key;
        }

        [System.Serializable]
        private class SetDataPayload
        {
            public string key;
            public string value;
        }

        [System.Serializable]
        private class DataWrapper
        {
            public string value;
        }

        [System.Serializable]
        private class DataKeysResponse
        {
            public string[] keys;
        }

        #endregion
    }
}
#endif
