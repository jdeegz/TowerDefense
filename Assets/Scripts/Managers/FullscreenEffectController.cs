using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using System.Collections;
using UnityEngine;

public class FullscreenEffectController : MonoBehaviour
{
    [Header("Post Processing")]
    public Volume m_ppVolume; // Assign this in the Inspector
    private Vignette m_vignette;
    [SerializeField] private float m_vignetteIntensityMax;
    [SerializeField] private float m_vignetteSmoothnessMax;
    [SerializeField] private Vector2 m_vignettePositionMax;
    private float m_vignetteIntensityMin;
    private float m_vignetteSmoothnessMin;
    private Vector2 m_vignettePositionMin;

    [Header("Castle Damaged")]
    [SerializeField] private ScriptableRendererFeature m_rfCastleDamaged;
    [SerializeField] private Material m_matCastleDamaged;
    public float m_castleDamageAppearDuration = .3f;
    public float m_castleDamageDissolveDuration = 1f;

    [Header("Castle Max Damaged")]
    [SerializeField] private ScriptableRendererFeature m_rfCastleMaxDamaged;
    [SerializeField] private Material m_matCastleMaxDamaged;
    public float m_castleMaxDamageAppearDuration = .3f;
    public float m_castleMaxDamageDissolveDuration = 1f;

    [Header("Tower Upgraded")]
    [SerializeField] private ScriptableRendererFeature m_rfTowerUpgraded;
    [SerializeField] private Material m_matTowerUpgraded;
    public float m_towerUpgradedAppearDuration = .3f;
    public float m_towerUpgradedDissolveDuration = 1f;

    [Header("Obelisk Complete")]
    [SerializeField] private ScriptableRendererFeature m_rfObeliskComplete;
    [SerializeField] private Material m_matObeliskComplete;
    public float m_obeliskCompleteAppearDuration = .3f;
    public float m_obeliskCompleteDissolveDuration = 1f;

    [Header("Mission Complete - Victory")]
    [SerializeField] private ScriptableRendererFeature m_rfMissionCompleteVictory;
    [SerializeField] private Material m_matMissionCompleteVictory;
    public float m_missionCompleteVictoryAppearDuration = .3f;
    public float m_missionCompleteVictoryDissolveDuration = 1f;

    [Header("Mission Complete - Defeat")]
    [SerializeField] private ScriptableRendererFeature m_rfMissionCompleteDefeat;
    [SerializeField] private Material m_matMissionCompleteDefeat;
    public float m_missionCompleteDefeatAppearDuration = .3f;
    public float m_missionCompleteDefeatDissolveDuration = 1f;

    private Coroutine m_curDamageCoroutine;
    private Coroutine m_curMaxDamageCoroutine;
    private Coroutine m_curTowerUpgradeCoroutine;
    private Coroutine m_curObeliskCompleteCoroutine;
    private Coroutine m_curMissionCompleteDefeatCoroutine;
    private Coroutine m_curMissionCompleteVictoryCoroutine;

    private float m_curDamageDissolve;
    private float m_curMaxDamageDissolve;
    private float m_curTowerUpgradeDissolve;
    private float m_curObeliskCompleteDissolve;
    private float m_curGossamerHealedDissolve;
    private float m_curSpireDestroyedDissolve;

    void Start()
    {
        //Subscribe to events we need to trigger effects for.
        GameplayManager.Instance.m_castleController.UpdateHealth += CastleDamaged;
        GameplayManager.Instance.m_castleController.UpdateMaxHealth += CastleMaxDamaged;
        GameplayManager.OnTowerUpgrade += TowerUpgraded;
        GameplayManager.OnObelisksCharged += ObeliskCompleted;
        GameplayManager.OnObelisksCharged += ObeliskCompleted;
        GameplayManager.OnSpireDestroyed += SpireDestroyed;
        GameplayManager.OnGossamerHealed += GossamerHealed;

        m_rfCastleDamaged.SetActive(false);
        m_rfCastleMaxDamaged.SetActive(false);
        m_rfTowerUpgraded.SetActive(false);
        m_rfObeliskComplete.SetActive(false);
        m_rfMissionCompleteVictory.SetActive(false);
        m_rfMissionCompleteDefeat.SetActive(false);

        if (m_ppVolume.profile.TryGet(out m_vignette))
        {
            m_vignetteIntensityMin = m_vignette.intensity.value;
            m_vignetteSmoothnessMin = m_vignette.smoothness.value;
            m_vignettePositionMin = m_vignette.center.value;
        }
    }

    private void SpireDestroyed(bool value)
    {
        if (m_curMissionCompleteDefeatCoroutine != null) StopCoroutine(m_curMissionCompleteDefeatCoroutine);
        if (value)
        {
            m_curMissionCompleteDefeatCoroutine = StartCoroutine(ShowSpireDestroyed());
        }
        else
        {
            m_curMissionCompleteDefeatCoroutine = StartCoroutine(HideSpireDestroyed());
        }
    }

    private void GossamerHealed(bool value)
    {
        if (m_curMissionCompleteVictoryCoroutine != null) StopCoroutine(m_curMissionCompleteVictoryCoroutine);
        if (value)
        {
            m_curMissionCompleteVictoryCoroutine = StartCoroutine(ShowGossamerHealed());
        }
        else
        {
            m_curMissionCompleteVictoryCoroutine = StartCoroutine(HideGossamerHealed());
        }
    }

    // RESPONSES

    private void CastleDamaged(int i)
    {
        if (i > 0) return;
        if (m_curDamageCoroutine != null) StopCoroutine(m_curDamageCoroutine);

        m_curDamageCoroutine = StartCoroutine(CastleDamageEffect());
    }

    private void CastleMaxDamaged(int i)
    {
        if (i > 0) return;
        if (m_curMaxDamageCoroutine != null) StopCoroutine(m_curMaxDamageCoroutine);

        m_curMaxDamageCoroutine = StartCoroutine(CastleMaxDamageEffect());
    }

    private void TowerUpgraded()
    {
        if (m_curTowerUpgradeCoroutine != null) StopCoroutine(m_curTowerUpgradeCoroutine);

        m_curTowerUpgradeCoroutine = StartCoroutine(TowerUpgradedEffect());
    }

    private void ObeliskCompleted(int obelisksCharged, int obeliskCount)
    {
        if (m_curObeliskCompleteCoroutine != null) StopCoroutine(m_curObeliskCompleteCoroutine);

        m_curObeliskCompleteCoroutine = StartCoroutine(ObeliskChargedEffect());
    }

    // COROUTINES

    IEnumerator CastleDamageEffect()
    {
        // SETUP
        m_curDamageDissolve = .5f;
        m_matCastleDamaged.SetFloat("_Dissolve", m_curDamageDissolve);
        m_rfCastleDamaged.SetActive(true);

        // APPEAR
        while (m_curDamageDissolve > 0)
        {
            m_curDamageDissolve -= Time.unscaledDeltaTime / m_castleDamageAppearDuration;
            if (m_curDamageDissolve < 0) m_curDamageDissolve = 0;
            m_matCastleDamaged.SetFloat("_Dissolve", m_curDamageDissolve);
            yield return null;
        }

        // DISSOLVE
        while (m_curDamageDissolve < 1f)
        {
            m_curDamageDissolve += Time.unscaledDeltaTime / m_castleDamageDissolveDuration;
            if (m_curDamageDissolve > 1f) m_curDamageDissolve = 1;
            m_matCastleDamaged.SetFloat("_Dissolve", m_curDamageDissolve);
            yield return null;
        }

        // RETURN
        m_matCastleDamaged.SetFloat("_Dissolve", 1f);
        yield return null;

        m_rfCastleDamaged.SetActive(false);
        m_curDamageCoroutine = null;
    }

    IEnumerator ShowGossamerHealed()
    {
        // SETUP
        m_curGossamerHealedDissolve = 1f;
        m_matMissionCompleteVictory.SetFloat("_Dissolve", m_curGossamerHealedDissolve);
        m_rfMissionCompleteVictory.SetActive(true);

        // APPEAR
        while (m_curGossamerHealedDissolve > 0)
        {
            m_curGossamerHealedDissolve -= Time.unscaledDeltaTime / m_missionCompleteVictoryAppearDuration;
            if (m_curGossamerHealedDissolve < 0) m_curGossamerHealedDissolve = 0;
            m_matMissionCompleteVictory.SetFloat("_Dissolve", m_curGossamerHealedDissolve);

            m_vignette.intensity.value = Mathf.Lerp(m_vignetteIntensityMax, m_vignetteIntensityMin, m_curGossamerHealedDissolve);
            m_vignette.smoothness.value = Mathf.Lerp(m_vignetteSmoothnessMax, m_vignetteSmoothnessMin, m_curGossamerHealedDissolve);
            m_vignette.center.value = Vector2.Lerp(m_vignettePositionMax, m_vignettePositionMin, m_curGossamerHealedDissolve);

            yield return null;
        }

        m_curMissionCompleteVictoryCoroutine = null;
    }


    IEnumerator HideGossamerHealed()
    {
        // DISSOLVE
        while (m_curGossamerHealedDissolve < 1f)
        {
            m_curGossamerHealedDissolve += Time.unscaledDeltaTime / m_missionCompleteVictoryDissolveDuration;
            if (m_curGossamerHealedDissolve > 1f) m_curGossamerHealedDissolve = 1;
            m_matMissionCompleteVictory.SetFloat("_Dissolve", m_curGossamerHealedDissolve);

            m_vignette.intensity.value = Mathf.Lerp(m_vignetteIntensityMax, m_vignetteIntensityMin, m_curGossamerHealedDissolve);
            m_vignette.smoothness.value = Mathf.Lerp(m_vignetteSmoothnessMax, m_vignetteSmoothnessMin, m_curGossamerHealedDissolve);
            m_vignette.center.value = Vector2.Lerp(m_vignettePositionMax, m_vignettePositionMin, m_curGossamerHealedDissolve);

            yield return null;
        }

        // RETURN
        m_matMissionCompleteVictory.SetFloat("_Dissolve", 1f);
        yield return null;

        m_rfMissionCompleteVictory.SetActive(false);
        m_curMissionCompleteVictoryCoroutine = null;
    }

    IEnumerator ShowSpireDestroyed()
    {
        // SETUP
        m_curSpireDestroyedDissolve = 1f;
        m_matMissionCompleteDefeat.SetFloat("_Dissolve", m_curSpireDestroyedDissolve);
        m_rfMissionCompleteDefeat.SetActive(true);

        // APPEAR
        while (m_curSpireDestroyedDissolve > 0)
        {
            m_curSpireDestroyedDissolve -= Time.unscaledDeltaTime / m_missionCompleteDefeatAppearDuration;
            if (m_curSpireDestroyedDissolve < 0) m_curSpireDestroyedDissolve = 0;
            m_matMissionCompleteDefeat.SetFloat("_Dissolve", m_curSpireDestroyedDissolve);
            yield return null;
        }

        m_curMissionCompleteDefeatCoroutine = null;
    }

    IEnumerator HideSpireDestroyed()
    {
        // DISSOLVE
        while (m_curSpireDestroyedDissolve < 1f)
        {
            m_curSpireDestroyedDissolve += Time.unscaledDeltaTime / m_missionCompleteDefeatDissolveDuration;
            if (m_curSpireDestroyedDissolve > 1f) m_curSpireDestroyedDissolve = 1;
            m_matMissionCompleteDefeat.SetFloat("_Dissolve", m_curSpireDestroyedDissolve);
            yield return null;
        }

        // RETURN
        m_matMissionCompleteDefeat.SetFloat("_Dissolve", 1f);
        yield return null;

        m_rfMissionCompleteDefeat.SetActive(false);
        m_curMissionCompleteDefeatCoroutine = null;
    }

    IEnumerator CastleMaxDamageEffect()
    {
        // SETUP
        m_curMaxDamageDissolve = .5f;
        m_matCastleMaxDamaged.SetFloat("_Dissolve", m_curMaxDamageDissolve);
        m_rfCastleMaxDamaged.SetActive(true);

        // APPEAR
        while (m_curMaxDamageDissolve > 0)
        {
            m_curMaxDamageDissolve -= Time.unscaledDeltaTime / m_castleMaxDamageAppearDuration;
            if (m_curMaxDamageDissolve < 0) m_curMaxDamageDissolve = 0;
            m_matCastleMaxDamaged.SetFloat("_Dissolve", m_curMaxDamageDissolve);
            yield return null;
        }

        // DISSOLVE
        while (m_curMaxDamageDissolve < 1f)
        {
            m_curMaxDamageDissolve += Time.unscaledDeltaTime / m_castleMaxDamageDissolveDuration;
            if (m_curMaxDamageDissolve > 1f) m_curMaxDamageDissolve = 1;
            m_matCastleMaxDamaged.SetFloat("_Dissolve", m_curMaxDamageDissolve);
            yield return null;
        }

        // RETURN
        m_matCastleMaxDamaged.SetFloat("_Dissolve", 1f);
        yield return null;

        m_rfCastleMaxDamaged.SetActive(false);
        m_curMaxDamageCoroutine = null;
    }

    IEnumerator TowerUpgradedEffect()
    {
        // SETUP
        m_curTowerUpgradeDissolve = .5f;
        m_matTowerUpgraded.SetFloat("_Dissolve", m_curTowerUpgradeDissolve);
        m_rfTowerUpgraded.SetActive(true);

        // APPEAR
        while (m_curTowerUpgradeDissolve > 0)
        {
            m_curTowerUpgradeDissolve -= Time.unscaledDeltaTime / m_towerUpgradedAppearDuration;
            if (m_curTowerUpgradeDissolve < 0) m_curTowerUpgradeDissolve = 0;
            m_matTowerUpgraded.SetFloat("_Dissolve", m_curTowerUpgradeDissolve);
            yield return null;
        }

        // DISSOLVE
        while (m_curTowerUpgradeDissolve < 1f)
        {
            m_curTowerUpgradeDissolve += Time.unscaledDeltaTime / m_towerUpgradedDissolveDuration;
            if (m_curTowerUpgradeDissolve > 1f) m_curTowerUpgradeDissolve = 1;
            m_matTowerUpgraded.SetFloat("_Dissolve", m_curTowerUpgradeDissolve);
            yield return null;
        }

        // RETURN
        m_matTowerUpgraded.SetFloat("_Dissolve", 1f);
        yield return null;

        m_rfTowerUpgraded.SetActive(false);
        m_curTowerUpgradeCoroutine = null;
    }

    IEnumerator ObeliskChargedEffect()
    {
        // SETUP
        m_curObeliskCompleteDissolve = .5f;
        m_matObeliskComplete.SetFloat("_Dissolve", m_curObeliskCompleteDissolve);
        m_rfObeliskComplete.SetActive(true);

        // APPEAR
        while (m_curObeliskCompleteDissolve > 0)
        {
            m_curObeliskCompleteDissolve -= Time.unscaledDeltaTime / m_obeliskCompleteAppearDuration;
            if (m_curObeliskCompleteDissolve < 0) m_curObeliskCompleteDissolve = 0;
            m_matObeliskComplete.SetFloat("_Dissolve", m_curObeliskCompleteDissolve);
            yield return null;
        }

        // DISSOLVE
        while (m_curObeliskCompleteDissolve < 1f)
        {
            m_curObeliskCompleteDissolve += Time.unscaledDeltaTime / m_obeliskCompleteDissolveDuration;
            if (m_curObeliskCompleteDissolve > 1f) m_curObeliskCompleteDissolve = 1;
            m_matObeliskComplete.SetFloat("_Dissolve", m_curObeliskCompleteDissolve);
            yield return null;
        }

        // RETURN
        m_matObeliskComplete.SetFloat("_Dissolve", 1f);
        yield return null;

        m_rfObeliskComplete.SetActive(false);
        m_curObeliskCompleteCoroutine = null;
    }

    void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Z))
        {
            CastleDamaged(0);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            CastleMaxDamaged(0);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            TowerUpgraded();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ObeliskCompleted(1, 1);
        }*/
    }

    private void OnDestroy()
    {
        m_rfCastleDamaged.SetActive(false);
        m_rfCastleMaxDamaged.SetActive(false);
        m_rfTowerUpgraded.SetActive(false);
        m_rfObeliskComplete.SetActive(false);
        m_rfMissionCompleteVictory.SetActive(false);
        m_rfMissionCompleteDefeat.SetActive(false);

        GameplayManager.Instance.m_castleController.UpdateHealth -= CastleDamaged;
        GameplayManager.Instance.m_castleController.UpdateMaxHealth -= CastleMaxDamaged;
        GameplayManager.OnTowerUpgrade -= TowerUpgraded;
        GameplayManager.OnObelisksCharged -= ObeliskCompleted;
        GameplayManager.OnSpireDestroyed -= SpireDestroyed;
        GameplayManager.OnGossamerHealed -= GossamerHealed;
    }
}