using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

public class TowerAura : Tower
{
    [SerializeField] private List<DOTweenAnimation> m_gearTweens;
    
    private float m_timeUntilFire;
    private List<GameObject> m_targetsTracked;
    private VisualEffect m_auraVFX;
    private float m_curDissolve = 1f;
    private bool m_searchForTargets;
    private int lastTrackedCount = 0;
    
    void OnEnable()
    {
        m_timeUntilFire = 1f / m_towerData.m_fireRate;
        m_targetsTracked = ListPool<GameObject>.Get();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_isBuilt && m_auraVFX == null)
        {
            StartDome();
        }
        
        if (IsReadyToFire() && m_searchForTargets)
        {
            SetTargets();
            //Debug.Log($"Searching for targets.");
        }
        
        Reload();
    }
    
    public override void RequestTowerDisable()
    {
        DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 1f, .2f)
            .OnUpdate(() => m_auraVFX.SetFloat("Dissolve", m_curDissolve))
            .OnComplete(() =>
            {
                m_auraVFX.Stop();
                ObjectPoolManager.ReturnObjectToPool(m_auraVFX.gameObject, ObjectPoolManager.PoolType.ParticleSystem);
                m_auraVFX = null;
                base.RequestTowerDisable();
            });
    }
    
    public override void RequestTowerEnable()
    {
        base.RequestTowerEnable();

        StartDome();
    }

    private List<GameObject> m_copyTargetsTracked;
    private void SetTargets()
    {
        //Restart Reload Timer
        m_timeUntilFire = 0;
        
        //Create targetsList Copy
        m_copyTargetsTracked = ListPool<GameObject>.Get();
        m_copyTargetsTracked.AddRange(m_targetsTracked);
        
        //Look for Units in sphere.
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_fireRange, m_layerMask.value);
        if (hits.Length > 0)
        {
            //Debug.Log($"Hits: {hits.Length} and Layers: {m_layerMask.value}");
            for (int i = 0; i < hits.Length; ++i)
            {
                //Does copyTargetsTracked contain hits. 
                if (m_copyTargetsTracked.Contains(hits[i].gameObject))
                {
                    //If it does, we have already applied an effect.
                    m_copyTargetsTracked.Remove(hits[i].gameObject);
                }
                else
                {
                    //If not, apply an effect, and add to Tracked list
                    SendEffect(hits[i].GetComponent<EnemyController>());
                    m_targetsTracked.Add(hits[i].gameObject);
                }
            }
        }

        //Remove the effect from items that remain in the Copy of Targets Tracked. They're no longer in the area.
        foreach (GameObject obj in m_copyTargetsTracked)
        {
            m_targetsTracked.Remove(obj);
            obj.GetComponent<EnemyController>().RequestRemoveEffect(gameObject);
        }
        
        ListPool<GameObject>.Release(m_copyTargetsTracked);
        
        if (m_targetsTracked.Count != lastTrackedCount)
        {
            lastTrackedCount = m_targetsTracked.Count;
            
            float newSpeed = lastTrackedCount > 0 ? m_statusEffectData.m_speedModifier : 1;
            foreach (DOTweenAnimation tween in m_gearTweens)
            {
                tween.tween.timeScale = newSpeed;
            }
        }
    }
    

    private void SendEffect(EnemyController enemyController)
    {
        StatusEffect statusEffect = new StatusEffect(gameObject, m_statusEffectData);
        enemyController.ApplyEffect(statusEffect);
        
        // AUDIO
        RequestPlayAudio(m_towerData.m_audioFireClips);
    }
    
    private void Reload()
    {
        m_timeUntilFire += Time.deltaTime;
    }
    
    private bool IsReadyToFire()
    {
        return m_timeUntilFire >= 1f / m_towerData.m_fireRate;
    }

    void StartDome()
    {
        //Start Effects
        var m_auraobj = ObjectPoolManager.SpawnObject(m_towerData.m_projectilePrefab, transform.position, quaternion.identity, m_modelRoot.transform, ObjectPoolManager.PoolType.ParticleSystem);
        m_auraVFX = m_auraobj.GetComponent<VisualEffect>();
        m_auraVFX.Play();
        DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 0f, 3f)
            .OnUpdate(() => m_auraVFX.SetFloat("Dissolve", m_curDissolve));
        
        //Start searching for targets.
        m_searchForTargets = true;
        
        // AUDIO
        RequestPlayAudioLoop(m_towerData.m_audioLoops[0]);
    }

    void StopDome()
    {
        DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 1f, 1f)
            .OnUpdate(() => m_auraVFX.SetFloat("Dissolve", m_curDissolve))
            .OnComplete(() =>
            {
                m_auraVFX.Stop();
                ObjectPoolManager.ReturnObjectToPool(m_auraVFX.gameObject, ObjectPoolManager.PoolType.ParticleSystem);
                m_auraVFX = null;
            });
        
        // AUDIO
        RequestStopAudioLoop();
    }
    
    public override void RemoveTower()
    {
        //Stop the VFX displayed.
        StopDome();
        
        //Stop searching for targets.
        m_searchForTargets = false;
        
        //Remove the effect from targets currently in range.
        foreach (GameObject obj in m_targetsTracked)
        {
            obj.GetComponent<EnemyController>().RequestRemoveEffect(gameObject);
        }

        ListPool<GameObject>.Release(m_targetsTracked);
        
        //Resume virtual function
        base.RemoveTower();
    }

    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;

        //Details string creation.
        string baseDamage = null;
        if (m_towerData.m_baseDamage > 0)
        {
            baseDamage = $"Damage: {m_towerData.m_baseDamage}{data.m_damageIconString}<br>" +
                         $"Fire Rate: {m_towerData.m_fireRate}{data.m_timeIconString}<br>" +
                         $"Range: {m_towerData.m_fireRange}";
        }
        else
        {
            baseDamage = $"Range: {m_towerData.m_fireRange}";
        }

        //If has secondary attack (Eruption)
        string secondaryDamage = null;
        if (m_towerData.m_hasSecondaryAttack)
        {
            secondaryDamage = $"<br>" +
                              $"Eruption Damage: {m_towerData.m_secondaryDamage}{data.m_damageIconString}<br>" +
                              $"Eruption Rate: {m_towerData.m_secondaryfireRate}{data.m_timeIconString}";
        }

        //If tower applies a status effect.
        string statusEffect = null;
        if (m_statusEffectData)
        {
            statusEffect = data.BuildStatusEffectString(m_statusEffectData);
        }

        StringBuilder descriptionBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(baseDamage))
            descriptionBuilder.Append(baseDamage);

        if (!string.IsNullOrEmpty(secondaryDamage))
            descriptionBuilder.Append(secondaryDamage);

        if (!string.IsNullOrEmpty(statusEffect))
            descriptionBuilder.Append(statusEffect);

        data.m_towerDetails = descriptionBuilder.ToString();
        return data;
    }

    public override TowerUpgradeData GetUpgradeData()
    {
        TowerUpgradeData data = new TowerUpgradeData();

        data.m_turretRotation = GetTurretRotation();

        return data;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        SetTurretRotation(data.m_turretRotation);
    }
}
