# Kružić Game SDK za Unity

SDK za integraciju Unity igara sa [Kružić](https://kruzic.rs) platformom.

## Zahtevi

- Unity 2020.3 ili noviji
- WebGL build target

## Instalacija

### Opcija 1: Unity Package Manager (preporučeno)

1. Otvori **Window > Package Manager**
2. Klikni **+** dugme i izaberi **Add package from git URL...**
3. Unesi: `https://github.com/kruzic/game-sdk-unity.git`

### Opcija 2: Ručna instalacija

1. Preuzmi ovaj repozitorijum
2. Kopiraj `Runtime` folder u `Assets/Kruzic` folder tvog projekta

## Brzi početak

```csharp
using Kruzic.GameSDK;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Obavesti platformu da je igra spremna
        KruzicSDK.Instance.Ready();

        // Proveri da li je korisnik prijavljen
        KruzicSDK.Instance.IsSignedIn((signedIn) =>
        {
            Debug.Log($"Korisnik prijavljen: {signedIn}");
        });

        // Dobij podatke o korisniku
        KruzicSDK.Instance.GetUserDetails((user) =>
        {
            if (user != null)
            {
                Debug.Log($"Ime: {user.name}");
            }
        });
    }
}
```

## API Reference

### Inicijalizacija

#### `KruzicSDK.Instance.Ready()`

Obaveštava platformu da je igra učitana i spremna. Pozovi ovu metodu kada se igra inicijalizuje.

```csharp
void Start()
{
    KruzicSDK.Instance.Ready();
}
```

### Korisnik

#### `IsSignedIn(Action<bool> callback)`

Proverava da li je korisnik prijavljen.

```csharp
KruzicSDK.Instance.IsSignedIn((signedIn) =>
{
    if (signedIn)
    {
        // Korisnik je prijavljen
    }
});
```

#### `GetUserDetails(Action<UserDetails> callback)`

Vraća detalje o trenutnom korisniku.

```csharp
KruzicSDK.Instance.GetUserDetails((user) =>
{
    if (user != null)
    {
        Debug.Log($"ID: {user.id}");
        Debug.Log($"Ime: {user.name}");
        Debug.Log($"Slika: {user.image}");
    }
});
```

#### `GetUserId(Action<string> callback)`

Vraća samo ID korisnika.

```csharp
KruzicSDK.Instance.GetUserId((userId) =>
{
    Debug.Log($"User ID: {userId}");
});
```

### Podaci

#### `GetData<T>(string key, Action<T> callback)`

Učitava sačuvanu vrednost za trenutnog korisnika.

```csharp
[Serializable]
public class PlayerData
{
    public int level;
    public int coins;
}

KruzicSDK.Instance.GetData<PlayerData>("player", (data) =>
{
    if (data != null)
    {
        Debug.Log($"Level: {data.level}");
    }
});
```

#### `SetData<T>(string key, T value, Action<bool> callback = null)`

Čuva vrednost za trenutnog korisnika. Vrednost mora biti serijalizabilna.

```csharp
var data = new PlayerData { level = 5, coins = 100 };
KruzicSDK.Instance.SetData("player", data, (success) =>
{
    Debug.Log($"Sačuvano: {success}");
});
```

#### `DeleteData(string key, Action<bool> callback = null)`

Briše sačuvanu vrednost.

```csharp
KruzicSDK.Instance.DeleteData("player", (success) =>
{
    Debug.Log($"Obrisano: {success}");
});
```

#### `ListData(Action<string[]> callback)`

Vraća listu svih sačuvanih ključeva.

```csharp
KruzicSDK.Instance.ListData((keys) =>
{
    foreach (var key in keys)
    {
        Debug.Log($"Ključ: {key}");
    }
});
```

### Helper metode

#### `SaveProgress<T>(T progress, Action<bool> callback = null)`

Čuva napredak igre pod ključem "progress".

```csharp
[Serializable]
public class GameProgress
{
    public int level;
    public int[] unlockedAchievements;
}

var progress = new GameProgress { level = 3 };
KruzicSDK.Instance.SaveProgress(progress);
```

#### `LoadProgress<T>(Action<T> callback)`

Učitava napredak igre.

```csharp
KruzicSDK.Instance.LoadProgress<GameProgress>((progress) =>
{
    if (progress != null)
    {
        currentLevel = progress.level;
    }
});
```

#### `SaveHighScore(int score, Action<bool> callback = null)`

Čuva high score.

```csharp
KruzicSDK.Instance.SaveHighScore(1000);
```

#### `GetHighScore(Action<int> callback)`

Učitava high score.

```csharp
KruzicSDK.Instance.GetHighScore((score) =>
{
    Debug.Log($"High score: {score}");
});
```

### Utility

#### `IsInIframe()`

Proverava da li igra radi unutar Kružić iframe-a.

```csharp
if (KruzicSDK.Instance.IsInIframe())
{
    // Igra je na Kružić platformi
}
```

## Editor/Dev Mode

Kada razvijaš u Unity Editoru (van WebGL build-a), SDK automatski koristi `PlayerPrefs` za čuvanje podataka i vraća mock podatke za korisnika:

- `IsSignedIn` vraća `true`
- `GetUserDetails` vraća dev korisnika sa ID-jem "dev-user"
- `GetData`/`SetData` koriste `PlayerPrefs`

Ovo omogućava testiranje bez potrebe za deploy-ovanjem na Kružić.

## Primer: Čuvanje napretka

```csharp
using System;
using UnityEngine;
using Kruzic.GameSDK;

[Serializable]
public class GameSave
{
    public int level = 1;
    public int coins = 0;
    public string[] achievements = new string[0];
}

public class SaveManager : MonoBehaviour
{
    private GameSave currentSave;

    void Start()
    {
        KruzicSDK.Instance.Ready();
        LoadGame();
    }

    public void LoadGame()
    {
        KruzicSDK.Instance.LoadProgress<GameSave>((save) =>
        {
            currentSave = save ?? new GameSave();
            Debug.Log($"Loaded: Level {currentSave.level}, Coins {currentSave.coins}");
        });
    }

    public void SaveGame()
    {
        KruzicSDK.Instance.SaveProgress(currentSave, (success) =>
        {
            if (success)
            {
                Debug.Log("Game saved!");
            }
        });
    }

    public void AddCoins(int amount)
    {
        currentSave.coins += amount;
        SaveGame();
    }

    public void CompleteLevel()
    {
        currentSave.level++;
        SaveGame();
    }
}
```

## WebGL Build Settings

Za optimalan rad na Kružić platformi, preporučujemo sledeća podešavanja:

1. **Player Settings > Resolution and Presentation**
   - Run In Background: ✓ Enabled

2. **Player Settings > Publishing Settings**
   - Compression Format: Gzip ili Brotli
   - Decompression Fallback: ✓ Enabled

## Licenca

MIT
