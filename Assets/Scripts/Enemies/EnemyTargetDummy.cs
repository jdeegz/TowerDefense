using UnityEngine;

public class EnemyTargetDummy : EnemyController
{
    //Override Damage Taken
    
    //Send Calculated Hitpoints
    public override void SetupEnemy(bool active)
    {
        base.SetupEnemy(active);
        
        EconomyLogging.Instance.SetUnitHealthThisWave(m_curMaxHealth);
    }

    public override void OnTakeDamage(float dmg)
    {
        //Calculate Damage
        float cumDamage = dmg * m_baseDamageMultiplier * m_lastDamageModifierHigher * m_lastDamageModifierLower;
        
        //EconomyLogging.Instance.AddToDamageDone(cumDamage);
        
        if (gameObject.activeSelf) HitFlash();
    }

    public override void ReachedCastle()
    {
        OnEnemyDestroyed(transform.position);
    }
}
