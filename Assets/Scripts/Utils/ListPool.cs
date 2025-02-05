using System.Collections.Generic;
using UnityEngine;

public static class ListPool<T>
{
    private static readonly Queue<List<T>> m_pool = new();
    private const int MAX_POOL_SIZE = 50;

    public static List<T> Get()
    {
        return m_pool.Count > 0 ? m_pool.Dequeue() : new List<T>();
    }

    public static void Release(List<T> list)
    {
        list.Clear();
        if (m_pool.Count < MAX_POOL_SIZE)
        {
            m_pool.Enqueue(list);
        }
    }

    public static void WarmUp(int Count)
    {
        for (int i = 0; i < Count; i++)
        {
            m_pool.Enqueue(new List<T>());
        }
    }
}