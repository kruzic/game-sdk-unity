#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Kruzic.GameSDK;
using System.Collections.Generic;

namespace Kruzic.GameSDK.Editor
{
    /// <summary>
    /// Editor prozor za upravljanje mock podacima za testiranje Kruzic SDK-a.
    /// Omogućava podešavanje mock korisničkih postavki i podataka igre bez deploy-a na WebGL.
    /// </summary>
    public class KruzicMockServerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string newKey = "";
        private string newValue = "";

        // Mock User Settings
        private bool mockUserSignedIn = true;
        private string mockUserId = "dev-user";
        private string mockUsername = "Dev User";
        private string mockAvatar = "";

        private List<MockDataEntry> dataEntries = new List<MockDataEntry>();

        [MenuItem("Kruzic/Mock Server Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<KruzicMockServerWindow>("Kruzic Mock Server");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            LoadMockData();
            LoadMockUserSettings();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawUserSettings();
            EditorGUILayout.Space(10);
            DrawDataManagement();
            EditorGUILayout.Space(10);
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Kruzic Mock Server Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Podesi mock podatke za testiranje Kruzic SDK-a u Unity Editoru. " +
                "Ovi podaci će biti korišćeni prilikom pokretanja u Play režimu bez WebGL build-a.",
                MessageType.Info
            );
            EditorGUILayout.Space(5);
        }

        private void DrawUserSettings()
        {
            EditorGUILayout.LabelField("Podešavanja Mock Korisnika", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            mockUserSignedIn = EditorGUILayout.Toggle("Korisnik Prijavljen", mockUserSignedIn);

            EditorGUI.BeginDisabledGroup(!mockUserSignedIn);
            mockUserId = EditorGUILayout.TextField("ID Korisnika", mockUserId);
            mockUsername = EditorGUILayout.TextField("Korisničko Ime", mockUsername);
            mockAvatar = EditorGUILayout.TextField("Avatar URL", mockAvatar);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                SaveMockUserSettings();
            }
        }

        private void DrawDataManagement()
        {
            EditorGUILayout.LabelField("Mock Podaci Igre", EditorStyles.boldLabel);

            // Sekcija za dodavanje novih podataka
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dodaj Nove Podatke", EditorStyles.miniBoldLabel);

            newKey = EditorGUILayout.TextField("Ključ", newKey);
            EditorGUILayout.LabelField("Vrednost (JSON format):", EditorStyles.miniLabel);
            newValue = EditorGUILayout.TextArea(newValue, GUILayout.Height(60));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Dodaj Podatke", GUILayout.Width(120)))
            {
                AddMockData();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Prikaz postojećih podataka
            EditorGUILayout.LabelField($"Sačuvani Podaci ({dataEntries.Count} stavki)", EditorStyles.miniBoldLabel);

            if (dataEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("Nema sačuvanih mock podataka. Dodajte podatke iznad za testiranje SDK-a.", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < dataEntries.Count; i++)
                {
                    DrawDataEntry(i);
                }
            }
        }

        private void DrawDataEntry(int index)
        {
            var entry = dataEntries[index];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Ključ: {entry.key}", EditorStyles.boldLabel, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Obriši", GUILayout.Width(60)))
            {
                dataEntries.RemoveAt(index);
                SaveMockData();
                return;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Vrednost:", EditorStyles.miniLabel);
            entry.value = EditorGUILayout.TextArea(entry.value, GUILayout.Height(60));

            if (EditorGUI.EndChangeCheck())
            {
                SaveMockData();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Učitaj Podatke"))
            {
                LoadMockData();
                LoadMockUserSettings();
            }

            if (GUILayout.Button("Obriši Sve Podatke"))
            {
                if (EditorUtility.DisplayDialog(
                    "Obriši Sve Mock Podatke",
                    "Da li ste sigurni da želite da obrišete sve mock podatke?",
                    "Da",
                    "Ne"))
                {
                    dataEntries.Clear();
                    SaveMockData();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void AddMockData()
        {
            if (string.IsNullOrWhiteSpace(newKey))
            {
                EditorUtility.DisplayDialog("Nevažeći Ključ", "Ključ ne može biti prazan.", "OK");
                return;
            }

            // Provera za duplirane ključeve
            if (dataEntries.Exists(e => e.key == newKey))
            {
                EditorUtility.DisplayDialog("Duplirani Ključ", $"Ključ '{newKey}' već postoji.", "OK");
                return;
            }

            dataEntries.Add(new MockDataEntry
            {
                key = newKey,
                value = newValue
            });

            SaveMockData();

            // Clear inputs
            newKey = "";
            newValue = "";
        }

        private void LoadMockData()
        {
            string json = PlayerPrefs.GetString(KruzicMockServer.MOCK_DATA_KEY, "");

            if (!string.IsNullOrEmpty(json))
            {
                var container = JsonUtility.FromJson<MockDataContainer>(json);
                dataEntries = container.entries ?? new List<MockDataEntry>();
            }
            else
            {
                dataEntries = new List<MockDataEntry>();
            }
        }

        private void SaveMockData()
        {
            var container = new MockDataContainer { entries = dataEntries };
            string json = JsonUtility.ToJson(container);
            PlayerPrefs.SetString(KruzicMockServer.MOCK_DATA_KEY, json);
            PlayerPrefs.Save();

            // Also update KruzicMockServer
            KruzicMockServer.UpdateMockData(dataEntries);
        }

        private void LoadMockUserSettings()
        {
            mockUserSignedIn = PlayerPrefs.GetInt(KruzicMockServer.MOCK_LOGGED_IN_KEY, 1) == 1;
            mockUserId = PlayerPrefs.GetString(KruzicMockServer.MOCK_USER_ID_KEY, "dev-user");
            mockUsername = PlayerPrefs.GetString(KruzicMockServer.MOCK_USERNAME_KEY, "Dev User");
            mockAvatar = PlayerPrefs.GetString(KruzicMockServer.MOCK_AVATAR_KEY, "");

            KruzicMockServer.UpdateMockUser(mockUserSignedIn, mockUserId, mockUsername, mockAvatar);
        }

        private void SaveMockUserSettings()
        {
            int valueToSave = mockUserSignedIn ? 1 : 0;
            PlayerPrefs.SetInt(KruzicMockServer.MOCK_LOGGED_IN_KEY, valueToSave);
            PlayerPrefs.SetString(KruzicMockServer.MOCK_USER_ID_KEY, mockUserId);
            PlayerPrefs.SetString(KruzicMockServer.MOCK_USERNAME_KEY, mockUsername);
            PlayerPrefs.SetString(KruzicMockServer.MOCK_AVATAR_KEY, mockAvatar);
            PlayerPrefs.Save();

            Debug.Log($"[Kruzic Mock Window] SaveMockUserSettings: Saving mockUserSignedIn={mockUserSignedIn}, value={valueToSave}");

            KruzicMockServer.UpdateMockUser(mockUserSignedIn, mockUserId, mockUsername, mockAvatar);
        }
    }

    /// <summary>
    /// Unos podataka za mock server skladištenje.
    /// </summary>
    [System.Serializable]
    public class MockDataEntry
    {
        public string key;
        public string value;
    }

    /// <summary>
    /// Kontejner za serijalizaciju mock podataka u PlayerPrefs.
    /// </summary>
    [System.Serializable]
    public class MockDataContainer
    {
        public List<MockDataEntry> entries;
    }
}
#endif
