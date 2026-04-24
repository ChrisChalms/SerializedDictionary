using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CC.SerializedCollections
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        public List<SerializedKeyValuePair<TKey, TValue>> Data
        {
            get => data;
            set => data = value ?? new List<SerializedKeyValuePair<TKey, TValue>>();
        }
        
        public TValue this[TKey key]
        {
            get { EnsureDict(); return dict[key]; }
            set
            {
                EnsureDict();
                dict[key] = value;
                var idx = data.FindIndex(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
                if (idx >= 0)
                {
                    data[idx] = new SerializedKeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    data.Add(new SerializedKeyValuePair<TKey, TValue>(key, value));
                }
            }
        }

        [SerializeField] private List<SerializedKeyValuePair<TKey, TValue>> data = new();
        [NonSerialized] private Dictionary<TKey, TValue> dict;
        
        public SerializedDictionary() => dict = new Dictionary<TKey, TValue>();
        public SerializedDictionary(int capacity, IEqualityComparer<TKey> comparer = null) => dict = new Dictionary<TKey, TValue>(capacity, comparer ?? EqualityComparer<TKey>.Default);

        private void EnsureDict() => dict ??= new Dictionary<TKey, TValue>();
        
        #region ISerializationCallbackReceiver
        
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            EnsureDict();
            dict.Clear();

            if (data == null)
            {
                return;
            }

            foreach (var kvp in data)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        #endregion
        
        #region IDictionary
        
        public ICollection<TKey> Keys { get { EnsureDict(); return dict.Keys; } }
        public ICollection<TValue> Values { get { EnsureDict(); return dict.Values; } }
        public int Count { get { EnsureDict(); return dict.Count; } }
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            EnsureDict();
            dict.Add(key, value);
            data.Add(new SerializedKeyValuePair<TKey, TValue>(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            EnsureDict();
            return dict.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            EnsureDict();
            if (dict.Remove(key) == false)
            {
                return false;
            }

            data.RemoveAll(kvp => EqualityComparer<TKey>.Default.Equals(kvp.Key, key));
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            EnsureDict();
            return dict.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            EnsureDict();
            dict.Clear();
            data.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            EnsureDict();
            return dict.TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            EnsureDict();
            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            EnsureDict();
            return Contains(item) && dict.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            EnsureDict();
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        #endregion
    }
}