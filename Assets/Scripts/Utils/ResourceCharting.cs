using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceCharting : MonoBehaviour
{
    public int m_storageCount;
    public float time;
    public List<ChartData> m_chartData;
    public string m_chartDataString;
    
    void Start()
    {
        ResourceManager.UpdateWoodBank += WoodAdded;
    }

    private void WoodAdded(int bankTotal, int amountAdded)
    {
        if (amountAdded != 1) return;
        ++m_storageCount;
        float timeAdded = (float)Math.Round(time, 1);
        ChartData newData = new ChartData(timeAdded);
        m_chartData.Add(newData);
        string dataString = $"{timeAdded}, ";
        m_chartDataString += dataString;
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
    }
}

[Serializable]
public class  ChartData
{
    public float m_timeStored;

    public ChartData( float time)
    {
        m_timeStored = time;
    }
}
