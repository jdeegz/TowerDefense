using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static event Action<GameState> OnGameStateChanged;
    public ScriptableMissionContainerObject m_MissionContainer;
    private String m_curScene;
    public GameObject m_loadingView;
    public GameState m_gameState;

    public enum GameState
    {
        Initialize,
        Menus,
        Gameplay,
    }

    void Awake()
    {
        Instance = this;
    }

    public void UpdateGameState(GameState newState)
    {
        //if (newState == m_gameState){ return;}

        m_gameState = newState;

        switch (m_gameState)
        {
            case GameState.Initialize:
                HandleInitialize();
                break;
            case GameState.Menus:
                RequestChangeScene("MenusTestScene");
                break;
            case GameState.Gameplay:
                RequestChangeScene("GameplayTestScene");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGameStateChanged?.Invoke(newState);
        Debug.Log("Game State:" + newState);
    }

    void Start()
    {
        UpdateGameState(GameState.Menus);
    }

    public void RequestChangeScene(String sceneName)
    {
        if (sceneName == m_curScene)
        {
            return;
        }

        {
            m_loadingView.SetActive(true);
            StartCoroutine(ChangeSceneAsync(sceneName));
            Debug.Log("Scene Loading: " + sceneName);
        }
    }

    private IEnumerator ChangeSceneAsync(String newScene)
    {
        if (m_curScene != null)
        {
            AsyncOperation unloadSceneOperation = SceneManager.UnloadSceneAsync(m_curScene);


            while (!unloadSceneOperation.isDone)
            {
                yield return null;
            }
        }


        m_curScene = newScene;

        AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync(m_curScene, LoadSceneMode.Additive);
        while (!loadSceneOperation.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_curScene));

        m_loadingView.SetActive(false);

        Debug.Log("Scene Loaded: " + m_curScene);
    }

    private void HandleInitialize()
    {
        //do stuff
    }
}