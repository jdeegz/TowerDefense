public interface IEffectable
{
    void ApplyEffect(StatusEffectData data);
    
    void HandleEffect(StatusEffect statusEffect);

    void RemoveEffect(StatusEffect statusEffect);
}
