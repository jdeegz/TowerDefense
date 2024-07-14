using System;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

public class TransitionController : MonoBehaviour
{
    public CanvasGroup m_canvasGroup;
    public float m_transitionStartDuration = 0.2f;
    public float m_transitionEndDuration = 0.2f;

    private Tween m_curTween;

    public void TransitionStart(String sceneName, Action onComplete)
    {
        m_curTween = m_canvasGroup.DOFade(1, m_transitionStartDuration).OnComplete(() => onComplete.Invoke());
        m_curTween.Play().SetUpdate(true);
    }
    
    public void TransitionEnd()
    {
        m_curTween = m_canvasGroup.DOFade(0, m_transitionEndDuration);
        m_curTween.Play().SetUpdate(true);
    }
}
