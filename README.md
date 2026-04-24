# Unity Serializable Dictionary

A generic dictionary implementation for Unity that serializes properly and provides a clean inspector interface similar to Unity's built-in lists.

## Features

- ✅ Full `IDictionary<TKey, TValue>` implementation
- ✅ **No concrete classes required** - use generics directly!
- ✅ Inspector support with add/remove buttons
- ✅ Works with any Unity-serializable types
- ✅ No runtime overhead - uses native `Dictionary<TKey, TValue>` internally
- ✅ Supports nested serialization (dictionaries in ScriptableObjects, prefabs, etc.)

## Installation

1. Copy the `SerializedCollections` and `Editor` folders into your Unity project
2. You can now use `SerializedDictionary<TKey, TValue>` anywhere

## Usage

### Basic Usage

Simply declare a `SerializedDictionary<TKey, TValue>` field with any serializable key and value types:

```csharp
using FusionGame.Common.SerializedCollections;

public class GameManager : MonoBehaviour
{
    public SerializedDictionary<string, int> scores;
    public SerializedDictionary<DamageType, float> damageMultipliers;
    public SerializedDictionary<int, GameObject> idToPrefab;
    public SerializedDictionary<string, Color> themeColors;
    
    void Start()
    {
        // Use like a normal dictionary
        scores["player1"] = 100;
        scores["player2"] = 85;
        
        damageMultipliers[DamageType.Fire] = 1.5f;
        damageMultipliers[DamageType.Ice] = 0.8f;
        
        // Full IDictionary support
        if (idToPrefab.TryGetValue(42, out GameObject prefab))
        {
            Instantiate(prefab);
        }
        
        // Iterate like any dictionary
        foreach (var kvp in scores)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
        
        // All standard dictionary operations
        scores.Remove("player1");
        bool hasPlayer = scores.ContainsKey("player2");
        int count = scores.Count;
        scores.Clear();
    }
}
```

### In ScriptableObjects

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config")]
public class GameConfig : ScriptableObject
{
    public SerializedDictionary<string, AudioClip> soundEffects;
    public SerializedDictionary<ItemType, Sprite> itemIcons;
    public SerializedDictionary<string, float> balanceValues;
}
```

### Runtime Modifications

The dictionary keeps both the internal `Dictionary<TKey, TValue>` and the serialized data in sync:

```csharp
// This updates both the runtime dictionary AND the serialized data
void AddScore(string playerName, int score) => scores[playerName] = score;

// This also keeps everything in sync
void RemovePlayer(string playerName) => scores.Remove(playerName);
```

## Supported Key/Value Types

Any type that Unity can serialize:

### Keys
- Primitives: `int`, `float`, `bool`, `string`
- Unity types: `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Rect`, etc.
- Enums
- Serializable custom structs/classes

### Values
- All of the above
- Unity object references: `GameObject`, `Transform`, `ScriptableObject`, `Material`, etc.
- Arrays and Lists (as long as they contain serializable types)
- Nested `SerializedDictionary` instances
- Custom serializable classes

## API Reference

`SerializedDictionary<TKey, TValue>` implements `IDictionary<TKey, TValue>`, providing all standard dictionary methods:

```csharp
// Properties
TValue this[TKey key] { get; set; }
ICollection<TKey> Keys { get; }
ICollection<TValue> Values { get; }
int Count { get; }
bool IsReadOnly { get; }

// Methods
void Add(TKey key, TValue value)
bool Remove(TKey key)
bool ContainsKey(TKey key)
bool TryGetValue(TKey key, out TValue value)
void Clear()
IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()

// Additional
List<SerializedKeyValuePair<TKey, TValue>> Data { get; set; }
```


## Differences from Standard Dictionary

| Feature | `Dictionary<TKey, TValue>` | `SerializedDictionary<TKey, TValue>` |
|---------|---------------------------|-------------------------------------|
| Serialization | ❌ Not serialized | ✅ Fully serialized |
| Inspector | ❌ No support | ✅ Full support |
| Runtime performance | ⚡ O(1) all operations | ⚡ O(1) reads, O(n) writes* |
| Generic usage | ✅ Yes | ✅ Yes |

*Writes are O(n) because the backing list must be updated