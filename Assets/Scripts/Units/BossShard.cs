using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BossShard : MonoBehaviour
{
    public float m_moveDuration = 2f;
    private Vector3 m_endPos;
    private Tween m_tweenToSpawner;


    public void SetupBossShard(Vector3 endPos)
    {
        m_endPos = endPos;
        HandleMovement();
    }

    void HandleMovement()
    {
        m_tweenToSpawner = gameObject.transform.DOJump(m_endPos, 2, 1, m_moveDuration).OnComplete(() => Destroy(gameObject));
        m_tweenToSpawner.Play();
    }

    void Update()
    {
    }
}
