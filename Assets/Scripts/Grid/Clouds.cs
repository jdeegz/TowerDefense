using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Clouds : MonoBehaviour
{
    public Vector2 m_moveSpeed;


    private UniversalAdditionalLightData lightExtantion;

    // Start is called before the first frame update
    void Start()
    {
        lightExtantion = GetComponent<UniversalAdditionalLightData>();
    }

    // Update is called once per frame
    void Update()
    {
        lightExtantion.lightCookieOffset += m_moveSpeed * Time.deltaTime;
    }
}