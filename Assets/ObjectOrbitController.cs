using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ObjectOrbitController : MonoBehaviour
{
    [Header("Grainwraith Settings")]
    public EnemyData m_enemyData;
    [SerializeField] private bool m_canPitch = true;
    [SerializeField] private Transform m_modelRoot;
    

    private float m_moveSpeed = 2f;
    private float m_turnSpeed = 90f;

    private Transform m_centerTransform;
    private Transform m_targetTransform;
    private Vector3 m_targetPos;
    private Vector3 m_targetPosYOffset;
    private Transform m_rotationRootTransform;

    private bool m_clockwise = true;
    private bool m_isAlive;

    private float m_stoppingDistance = 0.2f;
    private float m_initialHeight;
    private float m_desiredHeight;
    private float m_angleBetween;
    private float m_arcTravelled;
    private float m_remainingAngleBetween;
    private float m_progress;
    private float m_startBlend;

    private Vector3 m_startDir;
    private Vector3 m_targetDir;
    private Vector3 m_localCenter;

    private AudioSource m_audioSource;


    public void SetupObject(Transform centerTransform, Transform targetTransform, Transform rotationRootTransform, bool clockwise)
    {
        m_moveSpeed = m_enemyData.m_moveSpeed;
        m_turnSpeed = m_enemyData.m_lookSpeed;

        m_centerTransform = centerTransform;
        m_targetTransform = targetTransform;
        m_targetPosYOffset = new (0f, Random.Range(.25f, 1.75f), 0f);
        m_targetPos = m_targetTransform.position + m_targetPosYOffset;
        m_rotationRootTransform = rotationRootTransform;
        m_clockwise = clockwise;

        Vector3 toStart = transform.position - m_centerTransform.position;
        Vector3 flatToStart = new Vector3(toStart.x, 0, toStart.z).normalized;

        Vector3 tangentDir = m_clockwise
            ? Vector3.Cross(Vector3.up, flatToStart)
            : Vector3.Cross(flatToStart, Vector3.up);

        transform.forward = tangentDir;
        

        // Add small random rotation noise
        float yNoise = Random.Range(-30f, 30f);
        float xNoise = Random.Range(-30f, 30f);
        transform.rotation *= Quaternion.Euler(xNoise, yNoise, 0f);

        m_initialHeight = transform.position.y;
        m_desiredHeight = m_targetPos.y;

        Vector3 localStart = m_rotationRootTransform.InverseTransformPoint(transform.position);
        localStart.y = 0;
        Vector3 localTarget = m_rotationRootTransform.InverseTransformPoint(m_targetPos);
        localTarget.y = 0;
        m_localCenter = m_rotationRootTransform.InverseTransformPoint(m_centerTransform.position);
        m_localCenter.y = 0;

        m_startDir = (localStart - m_localCenter);
        m_targetDir = (localTarget - m_localCenter);

        float signedAngle = Vector3.SignedAngle(m_startDir, m_targetDir, Vector3.up);
        if (m_clockwise)
        {
            m_angleBetween = (360f + signedAngle) % 360f;
        }
        else
        {
            m_angleBetween = (360f - signedAngle) % 360f;
        }

        m_startBlend = Mathf.Lerp(0.2f, 0.85f, m_angleBetween / 360f);

        m_isAlive = true;
        m_audioSource = GetComponent<AudioSource>();
        RequestPlayAudio(m_enemyData.m_audioSpawnClips, m_audioSource);
        ObjectPoolManager.SpawnObject(m_enemyData.m_teleportArrivalVFX, transform.position, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
    }

    void Update()
    {
        if (!m_isAlive) return;

        m_targetPos = m_targetTransform.position + m_targetPosYOffset;
        
        Vector3 localCurrent = m_rotationRootTransform.InverseTransformPoint(transform.position);
        localCurrent.y = 0;
        m_localCenter = m_rotationRootTransform.InverseTransformPoint(m_centerTransform.position);
        m_localCenter.y = 0;

        Vector3 currentDir = (localCurrent - m_localCenter);
        m_remainingAngleBetween = Vector3.SignedAngle(m_startDir, currentDir, Vector3.up);
        if (m_clockwise)
        {
            m_remainingAngleBetween = (360f + m_remainingAngleBetween) % 360f;
        }
        else
        {
            m_remainingAngleBetween = (360f - m_remainingAngleBetween) % 360f;
        }

        // Calculate the progress
        float absRemainingAngle = Mathf.Abs(m_remainingAngleBetween);
        m_progress = Mathf.Clamp01(absRemainingAngle / m_angleBetween);

        float currentY = Mathf.Lerp(m_initialHeight, m_desiredHeight, m_progress);

        // Tangent direction
        Vector3 toCenter = transform.position - m_centerTransform.position;
        Vector3 flatToCenter = new Vector3(toCenter.x, 0, toCenter.z).normalized;
        Vector3 tangentDir = m_clockwise
            ? Vector3.Cross(Vector3.up, flatToCenter).normalized
            : Vector3.Cross(flatToCenter, Vector3.up).normalized;

        // Homing influence
        Vector3 targetLookDir = (m_targetPos - transform.position).normalized;

        float targetInfluence = Mathf.InverseLerp(m_startBlend, 1f, m_progress);
        Vector3 blendedDir = Vector3.Slerp(tangentDir, targetLookDir, targetInfluence).normalized;
        Quaternion targetRot = Quaternion.LookRotation(blendedDir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, m_turnSpeed * Time.deltaTime);

        if (!m_canPitch)
        {
            Vector3 modelEuler = m_modelRoot.localEulerAngles;
            modelEuler.x = -transform.eulerAngles.x;
            m_modelRoot.localEulerAngles = modelEuler;
        }
        
        // Move forward and apply height
        Vector3 move = transform.forward * m_moveSpeed * Time.deltaTime;
        Vector3 nextPos = transform.position + move;
        //nextPos.y = currentY;
        transform.position = nextPos;

        // Stop when close
        if (Vector3.Distance(transform.position, m_targetPos) < m_stoppingDistance)
        {
            m_initialHeight = 0;
            m_desiredHeight = 0;
            m_angleBetween = 0;
            m_arcTravelled = 0f;
            m_remainingAngleBetween = 0;
            m_progress = 0;
            ReachedTarget();
        }
    }

    public void RequestPlayAudio(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (clips[0] == null) return;

        if (audioSource == null) audioSource = m_audioSource;
        int i = Random.Range(0, clips.Count);
        audioSource.PlayOneShot(clips[i]);
    }

    public void ReachedTarget()
    {
        m_isAlive = false;
        if (m_enemyData.m_attackSpireVFXPrefab)
        {
            Quaternion rotation = Quaternion.LookRotation(-transform.forward);
            ObjectPoolManager.SpawnObject(m_enemyData.m_attackSpireVFXPrefab, transform.position, rotation, transform.parent, ObjectPoolManager.PoolType.ParticleSystem);
        }

        RemoveObject();
    }

    public void RemoveObject()
    {
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
    }
}