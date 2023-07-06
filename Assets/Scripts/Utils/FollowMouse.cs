using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;

public class FollowMouse : MonoBehaviour
{
    [SerializeField] private Camera m_mainCamera;
    [SerializeField] private LayerMask m_layerMask;
    public float m_offset = .2f;

    void Update()
    {
        Ray ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, m_layerMask))
        {
            Vector3 gridPos = raycastHit.collider.transform.position;
            gridPos.y = m_offset;
            gameObject.transform.position = gridPos;
        }
    }
}
