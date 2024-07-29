using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class ProjectileBeam : MonoBehaviour
{
    [SerializeField] private VisualEffect m_visualEffect;
    [SerializeField] private Transform m_pos0;
    [SerializeField] private Transform m_pos1;
    [SerializeField] private Transform m_pos2;
    [SerializeField] private Transform m_pos3;
    [SerializeField] private float m_pos1Speed = .5f;
    [SerializeField] private float m_pos2Speed = .5f;

    private Transform m_target;
    private Transform m_muzzlePoint;
    private float m_curDissolve = 1f;

    void Awake()
    {
        m_curDissolve = 1f;
    }

    void OnEnable()
    {
    }

    void Update()
    {
        UpdateBeamPositions();
    }

    public void StartBeam(Transform target, Transform muzzle)
    {
        //Set Data
        m_target = target;
        m_muzzlePoint = muzzle;
        
        //Set Pos 3
        m_pos3.transform.position = m_target.position;
        
        //Set Pos 0
        m_pos0.position = m_muzzlePoint.position;
        
        //Set Pos 2
        m_pos2.transform.position = Vector3.Lerp(m_pos0.position, m_pos3.position, 0.66f);
        
        //Set Pos 1
        m_pos1.transform.position = Vector3.Lerp(m_pos0.position, m_pos3.position, 0.33f);
        
        //Start Effects
        m_visualEffect.Play();
        DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 0f, 1f)
            .OnUpdate(() => m_visualEffect.SetFloat("Dissolve", m_curDissolve));
    }

    public void StopBeam()
    {
        //Release Data
        m_target = m_pos3;
        m_muzzlePoint = m_pos0;
        
        //Stop Effects
        m_visualEffect.Stop();
        DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 1f, 1f)
            .OnUpdate(() => m_visualEffect.SetFloat("Dissolve", m_curDissolve))
            .OnComplete(() => ObjectPoolManager.OrphanObject(gameObject, 3f, ObjectPoolManager.PoolType.Projectile));
    }

    private void UpdateBeamPositions()
    {
        //Set Pos 3
        m_pos3.transform.position = m_target.position;
        
        //Set Pos 0
        m_pos0.position = m_muzzlePoint.position;

        //Set Pos 2
        Vector3 absolutePos2 = Vector3.Lerp(m_pos0.position, m_pos3.position, 0.66f);
        m_pos2.transform.position = Vector3.Lerp(m_pos2.transform.position, absolutePos2, Time.deltaTime * m_pos2Speed);

        //Set Pos 1
        Vector3 absolutePos1 = Vector3.Lerp(m_pos0.position, m_pos3.position, 0.33f);
        m_pos1.transform.position = Vector3.Lerp(m_pos1.transform.position, absolutePos1, Time.deltaTime * m_pos1Speed);
        
    }
    
}