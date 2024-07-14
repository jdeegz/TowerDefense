using UnityEngine;

public class TestLoadCutscene : MonoBehaviour
{
    public void PlayCutscene()
    {
        GameplayManager.Instance.RequestCutSceneState();
        GameManager.Instance.RequestAdditiveSceneLoad("TestCutScene");
    }
}
