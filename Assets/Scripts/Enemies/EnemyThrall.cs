using UnityEngine;

public class EnemyThrall : EnemyController
{
    private EnemyHost m_enemyHost;
    
    public void SetHost(EnemyHost enemyHost)
    {
        m_enemyHost = enemyHost;
    }
    
    //HANDLING DAMAGE
    public override void OnTakeDamage(float dmg)
    {
        
        //Send damage to enemyHost
        m_enemyHost.OnTakeDamage(dmg);

        //Audio
        int i = Random.Range(0, m_enemyData.m_audioDamagedClips.Count);
        m_audioSource.PlayOneShot(m_enemyData.m_audioDamagedClips[i]);

        //VFX
    }

    public void HostHit()
    {
        //Hit Flash
        if (m_allRenderers == null || !gameObject.activeInHierarchy) return;
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }
        m_hitFlashCoroutine = StartCoroutine(HitFlash());
    }
    //
    
    //REDIRECT EFFECTS TO HOST.
    public override void RequestRemoveEffect(GameObject sender)
    {
        m_enemyHost.RequestRemoveEffect(sender);
    }
    
    public override void ApplyEffect(StatusEffect statusEffect)
    {
        m_enemyHost.ApplyEffect(statusEffect);
    }
    //
    
    //APPLY EFFECTS SENT FROM HOST
    public void HostApplyEffect(StatusEffect statusEffect)
    {
        base.ApplyEffect(statusEffect);
    }
    
    public void HostRemoveEffect(GameObject sender)
    {
        base.RequestRemoveEffect(sender);
    }
    //
    
    //REMOVING BASE FUNCTIONALITY
    public override void SetupUI()
    {
        return;
    }
    
    public override void AddToGameplayList()
    {
        return;
    }
    
    public override void RemoveFromGameplayList()
    {
        return;
    }
    //

}
