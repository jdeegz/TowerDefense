using System;
using System.Collections.Generic;
using UnityEngine;

public static class ListPool<T>
{
    private static readonly Queue<List<T>> m_pool = new();
    private const int MAX_POOL_SIZE = 50;
    
    // Static dictionary to track pool sizes per type
    private static readonly Dictionary<Type, int> poolTracker = new();

    public static List<T> Get()
    {
        UpdateTracker();
        return m_pool.Count > 0 ? m_pool.Dequeue() : new List<T>();
    }

    public static void Release(List<T> list)
    {
        list.Clear();
        if (m_pool.Count < MAX_POOL_SIZE)
        {
            m_pool.Enqueue(list);
        }
        UpdateTracker();
    }

    public static void WarmUp(int Count)
    {
        for (int i = 0; i < Count; i++)
        {
            m_pool.Enqueue(new List<T>());
        }

        UpdateTracker();
    }

    public static void Clear()
    {
        m_pool.Clear();
        UpdateTracker();
    }

    private static void UpdateTracker()
    {
        Type type = typeof(T);
        poolTracker[type] = m_pool.Count;
    }

    public static void LogPoolState()
    {
        //Debug.Log($"[ListPool] {typeof(T).Name} - Pool Size: {m_pool.Count}");
    }

    public static Dictionary<Type, int> GetPoolStates()
    {
        return new Dictionary<Type, int>(poolTracker);
    }
    
    public static void DebugPoolContents()
    {
        Type type = typeof(T);
        int totalLists = m_pool.Count;
        int nonEmptyLists = 0;
    
        foreach (var list in m_pool)
        {
            if (list.Count > 0) nonEmptyLists++;
        }

        //Debug.Log($"[ListPool Debug] {type.Name} Pool - Total Lists: {totalLists}, Non-Empty Lists: {nonEmptyLists}");

        // Optional: Log first few non-empty lists for deeper debugging
        int logCount = 0;
        foreach (var list in m_pool)
        {
            if (list.Count > 0)
            {
                //Debug.Log($"[ListPool Debug] {type.Name} Non-Empty List #{logCount + 1}: {string.Join(", ", list)}");
                logCount++;
                if (logCount >= 5) break; // Limit log spam
            }
        }
    }
}

