using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

[RequireComponent(typeof(Button))]
public class MenusButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Objects")]
    [SerializeField] private CanvasGroup m_hoverDisplayCanvasGroup;
    [SerializeField] private RectTransform m_hoverDisplayLabelRect;
    [SerializeField] private Image m_hoverDisplayFrameImage;

    [Header("Values")]
    [SerializeField] private float m_showDuration = 0.2f;
    [SerializeField] private float m_hideDuration = 0.1f;

    private RectTransform m_buttonRect;
    private RectTransform m_hoverDisplayRect;
    private Sequence m_curSequence;

    private float m_buttonWidth;

    void Awake()
    {
        m_buttonRect = GetComponent<RectTransform>();
        m_buttonWidth = m_buttonRect.rect.width;
        m_hoverDisplayRect = m_hoverDisplayCanvasGroup.GetComponent<RectTransform>();
        m_hoverDisplayFrameImage.color = GetComponent<Button>().colors.highlightedColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }

    void OnHoverEnter()
    {
        m_hoverDisplayCanvasGroup.blocksRaycasts = true;

        m_curSequence = DOTween.Sequence();
        m_curSequence.SetUpdate(true);
        m_curSequence.Append(m_hoverDisplayCanvasGroup.DOFade(1, m_showDuration));

        Vector2 endPos = new Vector2(m_buttonWidth, 0);
        Vector2 labelEndPos = new Vector2(m_buttonWidth, 0);

        m_curSequence.Join(m_hoverDisplayRect.DOAnchorPos(endPos, m_showDuration));
        m_curSequence.Join(m_hoverDisplayLabelRect.DOAnchorPos(labelEndPos, m_showDuration * 1.5f).From());

        m_curSequence.Play();
    }

    void OnHoverExit()
    {
        m_hoverDisplayCanvasGroup.blocksRaycasts = false;

        m_curSequence = DOTween.Sequence();
        m_curSequence.SetUpdate(true);
        m_curSequence.Append(m_hoverDisplayCanvasGroup.DOFade(0, m_hideDuration));

        Vector2 endPos = new Vector2(0, 0);
        Vector2 labelEndPos = new Vector2(0, 0);

        m_curSequence.Join(m_hoverDisplayRect.DOAnchorPos(endPos, m_hideDuration));
        m_curSequence.Join(m_hoverDisplayLabelRect.DOAnchorPos(labelEndPos, m_hideDuration));

        m_curSequence.Play();
    }
}