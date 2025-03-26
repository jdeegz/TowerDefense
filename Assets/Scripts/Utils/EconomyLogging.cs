using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EconomyLogging : MonoBehaviour
{
    private float m_incomeThisWave;
    private float m_unitHealthThisWave;

    public static EconomyLogging Instance;

    private string m_filePath;
    private string m_directoryPath;
    private string m_sceneName;
    private float m_waveDuration;
    private float m_totalDuration;
    private float m_totalIncome;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameplayManager.OnWaveChanged += LogWaveData;
        m_directoryPath = Application.persistentDataPath;
        m_sceneName = gameObject.scene.name.Replace(" ", "_");
        m_filePath = GetnewLogFileName();
        TierValues tierValues = GameplayManager.Instance.m_gameplayData.GetTierValues();
        string tiervaluesString = $"eLength {tierValues.m_earlyCycleLength}, eCount {tierValues.m_earlyCycleCount}, eMult {tierValues.m_earlyCurveMultiplier} : " +
                                  $"mLength {tierValues.m_midCycleLength}, mCount {tierValues.m_midCycleCount}, mMult {tierValues.m_midCurveMultiplier} : " +
                                  $"lLength {tierValues.m_lateCycleLength}, lMult {tierValues.m_lateCurveMultiplier}";
        WriteToFile(tiervaluesString);
        WriteToFile("Wave, Wave Duration, Total Time, Minute, Unit Health, Wave Income, Total Income");
    }

    void OnDestroy()
    {
        GameplayManager.OnWaveChanged -= LogWaveData;
    }

    void Update()
    {
        m_waveDuration += Time.deltaTime;
    }

    private string GetnewLogFileName()
    {
        Directory.CreateDirectory(m_directoryPath);
        string logPrefix = $"EconLog_{m_sceneName}_";
        string[] existingFiles = Directory.GetFiles(m_directoryPath, logPrefix + "*.csv");

        int highestIndex = 0;
        foreach (string file in existingFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string fileNumber = fileName.Replace(logPrefix, "");

            if (int.TryParse(fileNumber, out int num) && num > highestIndex)
            {
                highestIndex = num;
            }
        }

        int fileIndex = highestIndex + 1;
        string newFileName = $"{logPrefix}{fileIndex}.csv";
        return Path.Combine(m_directoryPath, newFileName);
    }

    private void WriteToFile(string content)
    {
        using (StreamWriter writer = new StreamWriter(m_filePath, true))
        {
            writer.WriteLine(content);
        }
    }

    public void AddToIncomeThisWave(int value)
    {
        m_incomeThisWave += value;
    }

    public void SetUnitHealthThisWave(float value)
    {
        m_unitHealthThisWave = value;
    }

    public void LogWaveData(int wave)
    {
        m_totalDuration += m_waveDuration;
        m_totalIncome += m_incomeThisWave;
        m_unitHealthThisWave = GameplayManager.Instance.m_gameplayData.CalculateHealth(10);
        int minute = GameplayManager.Instance.Minute; 
        string logEntry = $"{wave},{m_waveDuration.ToString("F1")},{m_totalDuration.ToString("F1")},{minute},{m_unitHealthThisWave.ToString("F1")},{m_incomeThisWave},{m_totalIncome}";
        WriteToFile(logEntry);

        m_unitHealthThisWave = 0;
        m_incomeThisWave = 0;
        m_waveDuration = 0;
    }

}
