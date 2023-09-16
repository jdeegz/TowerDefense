using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class TestAnimationScript : MonoBehaviour
{
    public RectTransform m_rect;
    public Image m_image;
    public Gradient m_gradient;
    public RectTransform m_endRect;
    public GameObject m_alert;

    public Button m_moveButton, m_stretchButton, m_fadeButton, m_shakeButton, m_punchButton, m_sizeButton;
    private bool m_moved, m_colored, m_faded, m_shaked, m_punched, m_sized;
    private Tween m_moveTween, m_colorTween, m_fadeTween, m_shakeTween, m_punchTween, m_sizeTween, m_spawnTween;
    private Sequence m_tweenSequence;

    // Start is called before the first frame update
    void Start()
    {
        m_moveButton.onClick.AddListener(OnMovePressed);
        m_stretchButton.onClick.AddListener(OnColorPressed);
        m_fadeButton.onClick.AddListener(OnSpawnPressed);
        m_shakeButton.onClick.AddListener(OnShakePressed);
        m_punchButton.onClick.AddListener(OnPunchPressed);
        m_sizeButton.onClick.AddListener(OnSizePressed);
    }

    private void OnSizePressed()
    {
        if (!m_sized)
        {
            m_sizeTween = m_rect.DOSizeDelta(new Vector2(200, 200), 1).SetAutoKill(false);
            m_sizeTween.Play();
            m_sized = true;
        }
        else
        {
            m_sizeTween.PlayBackwards();
            m_sized = false;
        }
    }

    private void OnPunchPressed()
    {
        if (!m_punched)
        {
            m_punchTween = m_rect.DOPunchAnchorPos(new Vector2(200, 200), 1, 4, 1).SetAutoKill(false);
            m_punchTween.Play();
            m_punched = true;
        }
        else
        {
            m_punchTween.PlayBackwards();
            m_punched = false;
        }
    }

    private void OnShakePressed()
    {
        m_shakeTween = m_rect.DOShakeAnchorPos(0.3f, 30f, 10, 90).SetAutoKill(false);
        m_shakeTween.Play();
    }

    private void OnFadePressed()
    {
        if (!m_faded)
        {
            m_fadeTween = m_image.DOFade(.5f, 1).SetAutoKill(false).OnComplete(OnShakePressed);
            m_fadeTween.Play();
            m_faded = true;
        }
        else
        {
            m_fadeTween.PlayBackwards();
            m_faded = false;
        }
    }

    private void OnColorPressed()
    {
        if (!m_colored)
        {
            m_colorTween = m_image.DOGradientColor(m_gradient, 1).SetAutoKill(false);
            m_colorTween.Play();
            m_colored = true;
        }
        else
        {
            m_colorTween.PlayBackwards();
            m_colored = false;
        }
    }

    private void OnMovePressed()
    {
        if (!m_moved)
        {
            m_moveTween = m_rect.DOAnchorPos(new Vector2(500, 0), 1).SetAutoKill(false);
            m_colorTween = m_image.DOGradientColor(m_gradient, 1).SetAutoKill(false);
            m_moveTween.Play();
            m_moved = true;
        }
        else
        {
            m_moveTween.PlayBackwards();
            m_colorTween.PlayBackwards();
            m_moved = false;
        }
    }

    private RectTransform m_target = null;
    private RectTransform m_spawnedObj;
    private void OnSpawnPressed()
    {
        
        GameObject obj = Instantiate(m_alert, transform);
        m_spawnedObj = obj.GetComponent<RectTransform>();
        m_spawnedObj.anchoredPosition = m_rect.anchoredPosition;
        
        UpdateTarget();
        
    }

    void UpdateTarget()
    {
        if (m_target == m_endRect)
        {
            m_target = m_rect;
        }
        else
        {
            m_target = m_endRect;
        }
        Debug.Log($"{m_target} is target.");
    }
    void Update()
    {
        if (m_target != null && m_spawnedObj != null)
        {
            m_moveTween = m_spawnedObj.DOAnchorPos(m_target.anchoredPosition, 1).SetLoops(-1);
        }
        
    }
}