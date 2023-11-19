using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ObeliskSoul : MonoBehaviour
{
    public float m_moveDuration = 2f;
    private Vector3 m_endPos;
    private Obelisk m_obelisk;
    private Tween m_tweenToObelisk;


    public void SetupSoul(Vector3 endPos, Obelisk obelisk)
    {
        m_endPos = endPos;
        m_obelisk = obelisk;
        HandleMovement();
    }

    void HandleMovement()
    {
        m_tweenToObelisk = gameObject.transform.DOJump(m_endPos, 2, 1, m_moveDuration).OnComplete(RequestObeliskCharge);
        m_tweenToObelisk.Play();
    }

    void RequestObeliskCharge()
    {
        m_obelisk.IncreaseObeliskCharge(1);
        Destroy(gameObject, 2);
    }
}
