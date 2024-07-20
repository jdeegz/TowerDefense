using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIIngameMeter : MonoBehaviour
{
    [SerializeField] private Track3dObject m_track3dObject;
    [SerializeField] private Image m_fillImage;
    [SerializeField] private RectTransform m_rootRectTransform;
    private float m_progress;

    public void SetupMeter(GameObject obj, float yOffset)
    {
        m_fillImage.fillAmount = 0f;
        m_track3dObject.SetupTracking(obj, GetComponent<RectTransform>(), yOffset);
    }

    public void SetProgress(float f)
    {
        //Debug.Log($"Setting progress to {f}");
        m_progress = f;
    }

    void Update()
    {
        m_fillImage.fillAmount = Mathf.Lerp(m_fillImage.fillAmount, m_progress, 10 * Time.deltaTime);
    }
}
