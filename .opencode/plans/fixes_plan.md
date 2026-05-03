# FF-Prot Critical Fixes Plan

## 1. Fix WorldCity.cs (Remove Invalid Appended Code)
**File:** `Assets/_Game/Models/WorldCity.cs`
**Action:** Delete all content after line 278 (the extra `CityDatabase` code appended incorrectly). The valid file ends with:
```csharp
        public override string ToString()
        {
            return $"[WorldCity] {DisplayName}, {Country} ({(IsUnlocked ? "🔓" : "🔒")})";
        }
    }
}
```

## 2. Update CityDatabase.cs (12+ MVP Cities)
**File:** `Assets/_Game/Models/CityDatabase.cs`
**Changes:**
- Add missing cities to reach 12+ (add Tokyo, Singapore, New York, London from original `LoadCities`)
- Fix `GetCity` method:
  ```csharp
  public static WorldCity GetCity(string id)
  {
      if (AllCities.TryGetValue(id, out var city)) return city;
      return null;
  }
  ```
- Update `Initialize()` to load all MVP cities (use the full list from original `WorldCity.cs` lines 298-432)

## 3. Add Missing `using System;` to Models
**Files to update:**
- `Assets/_Game/Models/Agent.cs` → Add `using System;` (for `Math.Max/Min`)
- `Assets/_Game/Models/Client.cs` → Add `using System;` (for `Math.Max/Min`)
- `Assets/_Game/Models/Cargo.cs` → Add `using System;` (for `Guid.NewGuid()`)
- `Assets/_Game/Models/Quote.cs` → Add `using System;` (for `Guid.NewGuid()`)

## 4. Replace `GetValueOrDefault` (Unsupported in .NET Standard 2.1)
**Files to fix:**
- `Assets/_Game/Managers/CargoManager.cs`:
  ```csharp
  // Old:
  float multiplier = Constants.CargoValueMultipliers.GetValueOrDefault(cargoType, 1.0f);
  // New:
  float multiplier = Constants.CargoValueMultipliers.ContainsKey(cargoType) 
      ? Constants.CargoValueMultipliers[cargoType] 
      : 1.0f;
  ```
- `Assets/_Game/Managers/ClientManager.cs`:
  ```csharp
  // Old:
  return RelationshipWithClients.GetValueOrDefault(clientId, 50f);
  // New:
  return RelationshipWithClients.ContainsKey(clientId) ? RelationshipWithClients[clientId] : 50f;
  ```
- `Assets/_Game/Managers/EventManager.cs`:
  ```csharp
  // Old:
  return EventHistory.GetValueOrDefault(cargoId, new List<GameEvent>());
  // New:
  return EventHistory.ContainsKey(cargoId) ? EventHistory[cargoId] : new List<GameEvent>();
  ```

## 5. Fix Event Signatures (Mismatched Parameters)
**File:** `Assets/_Game/Managers/AgentManager.cs`
**Changes:**
```csharp
// Old:
public event System.Action OnPriceSurge;
public event System.Action OnCargoAbandoned;
public event System.Action OnAgentDisappeared;
// New:
public event System.Action<Agent, Cargo, float> OnPriceSurge;
public event System.Action<Agent, string> OnCargoAbandoned;
public event System.Action<Agent, int> OnAgentDisappeared;
// Repeat for all events: match invocation parameters to delegate signature
```

**File:** `Assets/_Game/Managers/EconomyManager.cs`
```csharp
// Old:
public event Action OnXPGained;
// New:
public event Action<int, int> OnXPGained;
```

**File:** `Assets/_Game/Managers/ClientManager.cs`
```csharp
// Old:
public event Action<Client> OnRelationshipChanged;
// New:
public event Action<Client, float> OnRelationshipChanged;
```

**File:** `Assets/_Game/Managers/EventManager.cs`
```csharp
// Old:
public event Action<GameEvent> OnEventTriggered;
public event Action<GameEvent> OnEventResolved;
// New:
public event Action<GameEvent, Cargo> OnEventTriggered;
public event Action<GameEvent, Cargo, int> OnEventResolved;
```

## 6. Fix SaveManager.cs (JsonUtility Generic Call)
**File:** `Assets/_Game/Managers/SaveManager.cs`
```csharp
// Old:
var saveData = JsonUtility.FromJson(json);
// New:
var saveData = JsonUtility.FromJson<SaveData>(json);
```

## 7. Repair Truncated GameUI.cs
**File:** `Assets/_Game/UI/GameUI.cs`
**Action:** Complete the truncated line `_tabMarket = _root.Q` to proper UI Toolkit query (e.g., `_tabMarket = _root.Q<Button>("TabMarket");`) and ensure the file ends with proper closing braces.

## 8. Replace `Math.Clamp` with `Mathf.Clamp`
**File:** `Assets/_Game/Models/GameEvent.cs`
```csharp
// Old:
return Math.Clamp(probability, 0.01f, 0.30f);
// New:
return Mathf.Clamp(probability, 0.01f, 0.30f);
```

## 9. Fix WorldMap.cs Resources.Load
**File:** `Assets/_Game/Map/WorldMap.cs`
```csharp
// Old:
Texture2D tex = Resources.Load(path);
// New:
Texture2D tex = Resources.Load<Texture2D>(path);
```

## 10. Call CityDatabase.Initialize() in GameBootstrapper
**File:** `Assets/_Game/Managers/GameBootstrapper.cs`
**Action:** Add `CityDatabase.Initialize();` to `SetupInitialData()` method before loading other managers.

## Verification Steps
1. Open project in Unity
2. Check Console for compile errors
3. Test CityDatabase.AllCities count (should be 12+)
4. Verify managers initialize without null reference errors
5. Test save/load functionality with SaveManager