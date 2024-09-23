using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class UIAlert : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_label;

    [SerializeField] private float m_lifeTime;

    private RectTransform m_objRectTransform;

    void Start()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);
        m_objRectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 endPos = new Vector2(m_objRectTransform.anchoredPosition.x, m_objRectTransform.anchoredPosition.y + 100f);
        sequence.Append(m_objRectTransform.DOAnchorPos(endPos, m_lifeTime));

        sequence.AppendInterval(.5f);

        sequence.OnComplete(OnDestroy);
    }

    void OnDestroy()
    {
        Destroy(gameObject);
    }

    public void SetLabelText(string text, Color color)
    {
        m_label.SetText(text);
        m_label.color = color;
    }
}