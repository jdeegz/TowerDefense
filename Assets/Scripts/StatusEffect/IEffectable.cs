public interface IEffectable
{
    void ApplyEffect(StatusEffect data);
    
    void HandleEffect(StatusEffect statusEffect);

    void RemoveEffect(StatusEffect statusEffect);
}
