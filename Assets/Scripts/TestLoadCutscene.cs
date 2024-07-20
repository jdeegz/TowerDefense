using UnityEngine;

public class TestLoadCutscene : MonoBehaviour
{
    public void PlayCutscene()
    {
        GameManager.Instance.RequestAdditiveSceneLoad("TestCutScene");
    }
}
