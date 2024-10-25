using System;
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
        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);
        
        Vector2 endPos = new Vector2(m_objRectTransform.anchoredPosition.x, m_objRectTransform.anchoredPosition.y + 100f);
        sequence.Append(m_objRectTransform.DOAnchorPos(endPos, m_lifeTime));

        sequence.AppendInterval(.5f);

        sequence.OnComplete(RemoveObject);
    }

    void RemoveObject()
    {
        m_objRectTransform.anchoredPosition = new Vector2(0, 0); // Reset for next use.
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.GameObject);
    }

    public void SetLabelText(string text, Color color)
    {
        m_label.SetText(text);
        m_label.color = color;
    }
}