using System;
using System.Collections.Generic;

/// <summary>
/// Represents a cache of key/value pairs where the least recently used
/// elements are ejected when the capacity is exceeded.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
class LRUCache<TKey, TValue>
{
    private int _capacity;

    private Dictionary<TKey, TValue> _cache;

    private LinkedList<TKey> _lru;

    private Action<TKey, TValue> _eject;

    private TryOut<TKey, TValue> _miss;

    /// <summary>
    /// Gets or sets the maximum number of elements allowed in the LRUCache at one time.
    /// This could potentially eject some objects if the capacity is reduced.
    /// </summary>
    public int Capacity
    {
        get { return _capacity; }
        set
        {
            _capacity = value;
            EnforceCapacity();
        }
    }

    /// <summary>
    /// Gets the number of items currently in the LRUCache.
    /// </summary>
    public int Count
    {
        get { return _cache.Count; }
    }

    /// <summary>
    /// Sets the method to use when an element is ejected from the LRUCache.
    /// </summary>
    public Action<TKey, TValue> OnEject
    {
        set { _eject = value; }
    }

    /// <summary>
    /// Sets the method to use when an element is requested and not found in the LRUCache.
    /// </summary>
    public TryOut<TKey, TValue> OnMiss
    {
        set { _miss = value; }
    }

    /// <summary>
    /// Gets an enumeration of the keys in the cache.
    /// </summary>
    public IEnumerable<TKey> CachedKeys
    {
        get { return _lru; }
    }

    /// <summary>
    /// Gets or sets the value for a specified key.
    /// Getting a value may invoke the OnMiss method if the key is not currently present in the cache.
    /// Getting may also invoke OnEject if OnMiss returns true and adds a new element going over capcitiy.
    /// Setting a value may eject an element if it adds a new element and goes over capicity.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key if it is in the cache
    /// or if OnMiss returns true. If it is not found then a KeyNotFoundException is thrown.</returns>
    public TValue this[TKey key]
    {
        get
        {
            TValue value;
            if (!TryGetValue(key, out value))
            {
                throw new KeyNotFoundException();
            }
            return value;
        }
        set
        {
            if (!_cache.ContainsKey(key))
            {
                _lru.AddFirst(key);
            }
            _cache[key] = value;
            EnforceCapacity();
        }
    }

    /// <summary>
    /// Initializes a new LRUCache with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of elements allowed in the LRUCache at one time.</param>
    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<TKey, TValue>(capacity);
        _lru = new LinkedList<TKey>();
        _eject = null;
        _miss = null;
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// This action may eject an element if the cache goes over capicity as a result.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Add(TKey key, TValue value)
    {
        _cache.Add(key, value);
        _lru.AddFirst(key);
        EnforceCapacity();
    }

    /// <summary>
    /// Attempts to retrieve an element's value by key. If the key is not currently
    /// found in the Cache, the OnMiss method will be invoked.
    /// </summary>
    /// <param name="key">The key of the element.</param>
    /// <param name="value">The gotten value of the element.
    /// The value is undefined if the key is not found.</param>
    /// <returns>True if the key is in the Cache or if the OnMiss method returns true;
    /// otherwise, false.</returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        bool found = TryGetCachedValue(key, out value);
        if (!found && _miss != null)
        {
            found = _miss(key, out value);
            if (found)
            {
                Add(key, value);
            }
        }
        return found;
    }

    /// <summary>
    /// Attempts to retrieve an element's value by key from the Cache.
    /// This will not invoke the OnMiss method if the key is not found.
    /// </summary>
    /// <param name="key">The key of the element.</param>
    /// <param name="value">The gotten value of the element.
    /// The value is undefined if the key is not found.</param>
    /// <returns>True if the key is in the Cache; otherwise, false.</returns>
    public bool TryGetCachedValue(TKey key, out TValue value)
    {
        if (_cache.TryGetValue(key, out value))
        {
            SetMostRecentlyUsed(key);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Promotes the specified key to the MostRecentlyUsed position.
    /// The will delay the ejection of this item from EjectLeastRecentlyUsed calls.
    /// </summary>
    /// <param name="key">The key of the item to promote.</param>
    public void SetMostRecentlyUsed(TKey key)
    {
        var node = _lru.Find(key);
        if (node != null)
        {
            _lru.Remove(node);
            _lru.AddFirst(node);
        }
        else
        {
            throw new KeyNotFoundException();
        }
    }

    /// <summary>
    /// Ejects the element from the cache that was least recently
    /// accessed from the cache.
    /// </summary>
    /// <returns>Returns true if an element was ejected.</returns>
    public bool EjectLeastRecentlyUsed()
    {
        if (_lru.Count > 0)
        {
            Eject(_lru.Last);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Ejects an element from the cache with the specified key.
    /// </summary>
    /// <param name="key">The key of the element to eject.</param>
    /// <returns>True if an element with the specifieid key was found
    /// and ejected.</returns>
    public bool Eject(TKey key)
    {
        var node = _lru.Find(key);
        if (node != null)
        {
            Eject(node);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Ejects all elements in the cache.
    /// </summary>
    public void EjectAll()
    {
        foreach (TKey key in _lru)
        {
            TValue value = _cache[key];
            _cache.Remove(key);
            if (_eject != null)
            {
                _eject(key, value);
            }
        }
        _lru.Clear();
    }

    private void EnforceCapacity()
    {
        while (Count > Capacity)
        {
            EjectLeastRecentlyUsed();
        }
    }

    private void Eject(LinkedListNode<TKey> node)
    {
        TKey key = node.Value;
        TValue value = _cache[key];

        _lru.Remove(node);
        _cache.Remove(key);

        if (_eject != null)
        {
            _eject(key, value);
        }
    }
}