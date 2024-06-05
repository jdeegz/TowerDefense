using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class MaterialOverTime : MonoBehaviour
{
    private Material m_material;
    private Renderer m_renderer;
    private float m_dissolveValue;
    
    //Scrolling Values
    [Header("Diffuse Scrolling")]
    public bool m_scroll;
    public float m_scrollDelay;
    public Vector2 m_scrollStartSpeed;
    public Vector2 m_scrollEndSpeed;
    public float m_scrollDuration;
    public int m_scrollLoops;
    public LoopType m_scrollLoopType;
    public AnimationCurve m_scrollCurve = AnimationCurve.Linear(0,0,1,1);
    private Tween m_scrollTween;
    
    
    //Dissolve values
    [Header("Dissolve")]
    public bool m_dissolve;
    public float m_dissolveDelay = 0;
    public float m_dissolveStartValue = 1;
    public float m_dissolveEndValue;
    public float m_dissolveDuration;
    public int m_dissolveLoops;
    public LoopType m_dissolveLoopType;
    public AnimationCurve m_dissolveCurve = AnimationCurve.Linear(0,0,1,1);
    
    // Start is called before the first frame update

    void Awake()
    {
        //Get make sure there is a material.
        m_renderer = GetComponent<Renderer>();
        if (m_renderer != null)
        {
            m_material = m_renderer.material;
        }
    }
    
    void OnEnable()
    {
        if (m_dissolve) StartDissolveTween();
        
        if (m_scroll) StartScrollTween();
    }

    private void StartDissolveTween()
    {
        m_renderer.material.SetFloat("_DissolveValue", m_dissolveStartValue);
        
        //m_dissolveValue is temp, replace with Shader's variable.
        DOTween.To(() => m_renderer.material.GetFloat("_DissolveValue"), x => m_renderer.material.SetFloat("_DissolveValue", x), m_dissolveEndValue, m_dissolveDuration)
            .From(m_dissolveStartValue)
            .SetDelay(m_dissolveDelay)
            .SetLoops(m_dissolveLoops, m_dissolveLoopType)
            .SetEase(m_dissolveCurve)
            .OnUpdate(OnDissolveTweenUpdate);
    }
    
    void OnDissolveTweenUpdate()
    {
        if (m_renderer != null)
        {
            // This method will be called every frame while the tween is running
            //float currentShaderValue = GetComponent<Renderer>().material.GetFloat("_DissolveValue");
        }
    }
    
    private void StartScrollTween()
    {
        Vector4 startSpeed = m_scrollStartSpeed;
        Vector4 endSpeed = m_scrollEndSpeed;
        m_renderer.material.SetVector("_BaseScrollSpeed", m_scrollStartSpeed);
        
        //m_dissolveValue is temp, replace with Shader's variable.
        m_scrollTween = DOTween.To(() => m_renderer.material.GetVector("_BaseScrollSpeed"), x => m_renderer.material.SetVector("_BaseScrollSpeed", x), endSpeed, m_scrollDuration)
            .From(startSpeed)
            .SetDelay(m_scrollDelay)
            .SetLoops(m_scrollLoops, m_scrollLoopType)
            .SetEase(m_scrollCurve)
            .OnUpdate(OnScrollTweenUpdate);
    }

    private void OnScrollTweenUpdate()
    {
        // This method will be called every frame while the tween is running
        Debug.Log($"Scroll speed: {m_renderer.material.GetVector("_BaseScrollSpeed")}");
    }


    public float GetDissolveDuration()
    {
        return m_dissolveDelay + m_dissolveDuration;
    }
}
