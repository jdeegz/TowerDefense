using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameUtil;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;

public class ObeliskDrain : MonoBehaviour
{
    [Header("Spire")]
    public VisualEffect m_activeBeam;
    public VisualEffect m_shutdownBeam;
    
    [Header("Obelisks")]
    public Renderer m_meter;
    public float m_duration;
    public VisualEffect m_meterShutdownVFX;

    public Renderer m_secondaryMeter;
    public float m_secondaryDuration;
    public VisualEffect m_secondaryMeterShutdownVFX;

    public Renderer m_smoke;
    public VisualEffect m_smokeVFX;
    public Renderer m_secondarySmoke;
    public VisualEffect m_secondarySmokeVFX;
    
    [Header("Screen Effect")]
    [SerializeField] private ScriptableRendererFeature m_rfGrainwraithTear;
    [SerializeField] private Material m_matGrainwraithTear;
    public float m_grainwraithTearDissolveDuration = 1f;

    private float m_timeElapsed;
    private float m_secondaryTimeElapsed;
    private Material m_meterMaterial;
    private Material m_smokeMaterial;
    private Material m_secdonaryMeterMaterial;
    private Material m_secdonarySmokeMaterial;

    void Start()
    {
        m_meterMaterial = m_meter.material;
        m_secdonaryMeterMaterial = m_secondaryMeter.material;

        m_meterMaterial.SetFloat("_DissolveValue", 0);
        m_secdonaryMeterMaterial.SetFloat("_DissolveValue", 0);
        
        m_smokeMaterial = m_smoke.material;
        m_secdonarySmokeMaterial = m_secondarySmoke.material;

        m_rfGrainwraithTear.SetActive(false);
        //TriggerObeliskDrain();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            TriggerObeliskDrain();

            Timer.DelayAction(4f, () => TriggerSecondObeliskDrain());
        }
    }


    public void TriggerObeliskDrain()
    {
        
        m_meterShutdownVFX.Play();
        
        //First meter
        m_meterMaterial.SetFloat("_DissolveValue", 0);
        DOTween.To(
            () => m_meterMaterial.GetFloat("_DissolveValue"),
            value => m_meterMaterial.SetFloat("_DissolveValue", value),
            1f,
            m_duration);
       

        //Smoke
        m_smokeMaterial.SetFloat("_DissolveValue", 0);
        DOTween.To(
            () => m_smokeMaterial.GetFloat("_DissolveValue"),
            value => m_smokeMaterial.SetFloat("_DissolveValue", value),
            1f,
            m_duration).OnComplete( () => m_smokeVFX.Stop());
        
        
    }

    public void TriggerSecondObeliskDrain()
    {
        
        m_secondaryMeterShutdownVFX.Play();
        
        //Second meter
        m_secdonaryMeterMaterial.SetFloat("_DissolveValue", 0);
        DOTween.To(
            () => m_secdonaryMeterMaterial.GetFloat("_DissolveValue"),
            value => m_secdonaryMeterMaterial.SetFloat("_DissolveValue", value),
            1f,
            m_secondaryDuration).OnComplete( () => DisableSpireBeam());
        
        //Second Smoke
        m_secdonarySmokeMaterial.SetFloat("_DissolveValue", 0);
        DOTween.To(
            () => m_secdonarySmokeMaterial.GetFloat("_DissolveValue"),
            value => m_secdonarySmokeMaterial.SetFloat("_DissolveValue", value),
            1f,
            m_duration).OnComplete( () => m_secondarySmokeVFX.Stop());
    }

    public void DisableSpireBeam()
    {
        m_shutdownBeam.Play();

        m_activeBeam.gameObject.SetActive(false);
        
        DOTween.To(
            () => m_shutdownBeam.GetFloat("Shutdown"),
            value => m_shutdownBeam.SetFloat("Shutdown", value),
            0f,
            3f).OnComplete( () => GrainwraithTearEffect());
    }

    public void GrainwraithTearEffect()
    {
        m_rfGrainwraithTear.SetActive(true);
        
        m_matGrainwraithTear.SetFloat("_Dissolve", 1);
        
        DOTween.To(
            () => m_matGrainwraithTear.GetFloat("_Dissolve"),
            value => m_matGrainwraithTear.SetFloat("_Dissolve", value),
            0f,
            m_grainwraithTearDissolveDuration);
        
        m_matGrainwraithTear.SetFloat("_VignettePower", 4);
        
        DOTween.To(
            () => m_matGrainwraithTear.GetFloat("_VignettePower"),
            value => m_matGrainwraithTear.SetFloat("_VignettePower", value),
            0f,
            m_grainwraithTearDissolveDuration).SetEase(Ease.InQuad);
    }
}