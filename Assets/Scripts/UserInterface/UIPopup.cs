using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPopup : MonoBehaviour
{
    [Header("UI Popup Settings")]
    [SerializeField] private Button m_closeButton;
    [SerializeField] private bool m_closeOnEscape = true;
    [SerializeField] private bool m_closeOnOutsideClick = false;
    [SerializeField] private Button m_closeOnOutsideButton;
    [SerializeField] private bool m_isModal = true;
    [SerializeField] private float m_autoCloseTime = 0f; // 0 means no auto-close
    
    [Header("UI Animation Settings")]
    [SerializeField] private float m_showPopupFadeDuration = 0.15f;
    [SerializeField] private float m_hidePopupFadeDuration = 0.15f;

    public event Action OnPopupOpen;
    public event Action OnPopupClose;
    public bool CloseOnEscape => m_closeOnEscape;

    protected CanvasGroup m_canvasGroup; 
    protected AudioSource m_audioSource;
    
    protected virtual void Awake()
    {
        if (m_closeButton != null)
        {
            m_closeButton.onClick.AddListener(RequestClose);
        }

        if (m_closeOnOutsideButton != null && m_closeOnOutsideClick)
        {
            m_closeOnOutsideButton.onClick.AddListener(RequestClose);
        }

        m_canvasGroup = GetComponent<CanvasGroup>();
        m_audioSource = GetComponent<AudioSource>();
    }

    protected virtual void OnEnable()
    {
        OnPopupOpen?.Invoke();
        if (m_autoCloseTime > 0) StartCoroutine(AutoCloseRoutine());
    }

    protected virtual void Update()
    {
        /*if (m_closeOnOutsideClick && Input.GetMouseButtonDown(0))
        {
            
            if (!IsPointerOverUIElement(gameObject))
            {
                RequestClose();
            }
        }*/
    }
    
    /*private bool IsPointerOverUIElement(GameObject popup)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
    
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == popup || result.gameObject.transform.IsChildOf(popup.transform))
            {
                return true; // Clicked inside the popup
            }
        }

        return false; // Clicked outside
    }*/


    public virtual void HandleShow()
    {
        Debug.Log($"Showing {this.name}");
        gameObject.SetActive(true);
        m_canvasGroup.interactable = true;
        m_canvasGroup.blocksRaycasts = true;
        m_canvasGroup.alpha = 0;
        m_canvasGroup.DOFade(1, m_showPopupFadeDuration).SetUpdate(true);
    }

    public virtual void RequestClose()
    {
        Debug.Log($"Closing {this.name}");
        UIPopupManager.Instance.ClosePopup(this);
    }
    
    public virtual void HandleClose()
    {
        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;
        m_canvasGroup.DOFade(0, m_showPopupFadeDuration).SetUpdate(true).OnComplete(() => CompleteClose());
    }

    public virtual void CompleteClose()
    {
        if (this is IDataPopup dataPopup)
        {
            dataPopup.ResetData();
        }
        OnPopupClose?.Invoke();
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.UI);
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(m_autoCloseTime);
        HandleClose();
    }

    protected virtual void OnDestroy()
    {
        if (m_closeButton != null)
        {
            m_closeButton.onClick.RemoveListener(HandleClose);
        }
    }
}