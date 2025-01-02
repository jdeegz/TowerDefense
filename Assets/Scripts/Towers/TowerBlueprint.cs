using System.Security;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class TowerBlueprint : Tower
{
    [SerializeField] private GameObject m_blueprintRootObj;
    [SerializeField] private MeshRenderer m_bottomMeshRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public override void SetupTower()
    {
        //Grid
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddBlueprintToList(this);

        //Operational
        gameObject.GetComponent<Collider>().enabled = true;
        m_isBuilt = false;
        m_modelRoot.SetActive(true);

        //Animation
        m_animator.SetTrigger("Construct");

        //Audio
        m_audioSource.PlayOneShot(m_towerData.m_audioBuildClip);

        //VFX
        ObjectPoolManager.SpawnObject(m_towerData.m_towerConstructionPrefab, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);

        GameplayManager.OnGamePlaybackChanged += GameplayPlaybackChanged;
    }

    private Vector3 m_raisedPos = new Vector3(0,0,0);
    private Vector3 m_loweredPos = new Vector3(0,-.54f,0);
    private bool m_isRaised = true;
    private Tween m_curTween;
    private void GameplayPlaybackChanged(GameplayManager.GameSpeed newSpeed)
    {
        float duration;
        switch (newSpeed)
        {
            case GameplayManager.GameSpeed.Paused:
                if (m_isRaised == true) return;
                duration = Random.Range(0.15f, 0.45f);
                m_bottomMeshRenderer.enabled = true;
                if(m_curTween != null && m_curTween.IsPlaying()) m_curTween.Kill();
                m_curTween = m_blueprintRootObj.transform.DOLocalMove(m_raisedPos, duration).SetEase(Ease.OutBack).SetUpdate(true);
                m_isRaised = true;
                break;
            case GameplayManager.GameSpeed.Normal:
                if (m_isRaised == false) return;
                duration = Random.Range(0.15f, 0.45f);
                if(m_curTween != null && m_curTween.IsPlaying()) m_curTween.Kill();
                m_curTween = m_blueprintRootObj.transform.DOLocalMove(m_loweredPos, duration).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() => m_bottomMeshRenderer.enabled = false);
                m_isRaised = false;
                break;
            default:
                break;
        }
    }
    
    public override void RemoveTower()
    {
        GameplayManager.OnGamePlaybackChanged -= GameplayPlaybackChanged;
        base.RemoveTower();
    }


    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;
        return data;
    }

    public override TowerUpgradeData GetUpgradeData()
    {
        Debug.Log($"Blueprint Tower has no Upgrade Data to get.");
        return null;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        Debug.Log($"Blueprint Tower has no Upgrade Data to set.");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GameplayManager.OnGamePlaybackChanged -= GameplayPlaybackChanged;
    }
}
