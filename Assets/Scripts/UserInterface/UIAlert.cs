using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

public class UIAlert : MonoBehaviour // TO DO This should become a derived class to allow for more alert variation.
{
    [SerializeField] private TextMeshProUGUI m_label;

    [SerializeField] private float m_lifeTime;

    private RectTransform m_objRectTransform;
    private CanvasGroup m_canvasGroup;


    public void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetupAlert(Vector2 pos)
    {
        m_objRectTransform = gameObject.GetComponent<RectTransform>();
        m_objRectTransform.anchoredPosition = pos;
        
        Vector3 newPosition = m_objRectTransform.localPosition;
        newPosition.z = 0;
        m_objRectTransform.localPosition = newPosition;
        
        BeginTween();
    }

    void BeginTween()
    {
        m_canvasGroup.alpha = 0;
        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);
        
        Vector2 midPos = new Vector2(m_objRectTransform.anchoredPosition.x, m_objRectTransform.anchoredPosition.y + 50f);
        Vector2 endPos = new Vector2(midPos.x, midPos.y + 15f);

        float showDuration = m_lifeTime * .165f;
        float idleDuration = m_lifeTime * .66f;
        float hideDuration = m_lifeTime * .165f;
        
        sequence.Append(m_canvasGroup.DOFade(1, showDuration));
        sequence.Join(m_objRectTransform.DOAnchorPos(midPos, showDuration));

        sequence.AppendInterval(idleDuration);
        
        sequence.Append(m_objRectTransform.DOAnchorPos(endPos, hideDuration));
        sequence.Join(m_canvasGroup.DOFade(0, hideDuration));

        sequence.OnComplete(RemoveObject);
    }

    void RemoveObject()
    {
        m_objRectTransform.anchoredPosition = new Vector2(0, 0); // Reset for next use.
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.GameObject);
    }

    public void SetLabelText(string text, Color color, bool tint = true)
    {
        m_label.SetText(text);
        if(tint) m_label.color = color;
        LayoutRebuilder.MarkLayoutForRebuild(m_objRectTransform);
    }
}