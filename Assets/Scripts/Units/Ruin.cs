using UnityEngine;

public abstract class Ruin : MonoBehaviour
{
    public AudioSource m_audioSource;
    public abstract RuinTooltipData GetTooltipData();
    public abstract void GathererArrivedAtRuin(GathererController gathererController);
    
    public virtual void  Awake()
    {
        //
    }

    public void RequestPlayAudio(AudioClip clip)
    {
        if (m_audioSource == null) return;
        
        m_audioSource.PlayOneShot(clip);
    }
}

public class RuinTooltipData
{
    public string m_ruinName;
    public string m_ruinDescription;
    public string m_ruinDetails;
}
