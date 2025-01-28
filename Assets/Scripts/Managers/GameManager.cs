using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static event Action<GameState> OnGameStateChanged;
    public MissionContainerData m_missionTable;
    public MissionData m_curMission;
    private String m_curScene;
    private String m_curCutScene;
    public GameState m_gameState;
    public TransitionController m_cutSceneTransitionController;
    public TransitionController m_loadingTransitionController;

    public enum GameState
    {
        Initialize,
        Menus,
        Gameplay,
    }

    void Awake()
    {
        Instance = this;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void UpdateGameState(GameState newState)
    {
        Time.timeScale = 1;
        m_gameState = newState;

        switch (m_gameState)
        {
            case GameState.Initialize:
                HandleInitialize();
                break;
            case GameState.Menus:
                break;
            case GameState.Gameplay:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnGameStateChanged?.Invoke(newState);
    }

    void Start()
    {
        UpdateGameState(GameState.Initialize);
    }

    private void HandleInitialize()
    {
        RequestChangeScene("Menus", GameState.Menus);
    }

    public void RequestChangeScene(String sceneName, GameState newState, MenuManager.MenuState? menuState = null)
    {
        Time.timeScale = 1.0f;
        if (sceneName == m_curScene)
        {
            return;
        }

        m_loadingTransitionController.TransitionStart(sceneName, () => StartChangeScene(sceneName, menuState));
        UpdateGameState(newState);
    }

    void StartChangeScene(String sceneName, MenuManager.MenuState? menuState = null)
    {
        StartCoroutine(ChangeSceneAsync(sceneName, menuState));
    }

    public void RequestSceneRestart()
    {
        Time.timeScale = 1.0f;
        String sceneName = SceneManager.GetActiveScene().name;
        m_loadingTransitionController.TransitionStart(sceneName, () => StartChangeScene(sceneName));
    }
    
    private IEnumerator ChangeSceneAsync(String newScene, MenuManager.MenuState? menuState)
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

        if (menuState.HasValue && MenuManager.Instance != null)
        {
            MenuManager.Instance.UpdateMenuState(menuState.Value);
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_curScene));
        m_loadingTransitionController.TransitionEnd();
    }


    //START CUTSCENE MANAGEMENT
    //ADD SCENE
    public void RequestAdditiveSceneLoad(String sceneName)
    {
        m_cutSceneTransitionController.TransitionStart(sceneName, () => StartCutSceneLoad(sceneName));
    }

    public void StartCutSceneLoad(String sceneName)
    {
        GameplayManager.Instance.WatchingCutScene();
        StartCoroutine(AddCutSceneAsync(sceneName));
    }

    private IEnumerator AddCutSceneAsync(String sceneName)
    {
        m_curCutScene = sceneName;

        AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync(m_curCutScene, LoadSceneMode.Additive);
        while (!loadSceneOperation.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_curCutScene));
        m_cutSceneTransitionController.TransitionEnd(); //Reveal new scene
    }

    //REMOVE SCENE
    public void RequestAdditiveSceneUnload(String sceneName)
    {
        m_cutSceneTransitionController.TransitionStart(sceneName, () => StartCutSceneUnload(sceneName));
    }

    public void StartCutSceneUnload(String sceneName)
    {
        StartCoroutine(RemoveCutSceneAsync(sceneName));
    }

    private IEnumerator RemoveCutSceneAsync(String sceneName)
    {
        AsyncOperation loadSceneOperation = SceneManager.UnloadSceneAsync(m_curCutScene);
        while (!loadSceneOperation.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(m_curScene));
        m_cutSceneTransitionController.TransitionEnd(); //Reveal new scene
        GameplayManager.Instance.DoneWatchingLeaveCutScene(); //Return to our last gameplay state.
    }
    //END CUTSCENE MANAGEMENT
}