// We have to mute URP's default decal implementation as it would tweak our albedo - which needs to be pure black
// as otherwise default lighting would no be stripped by the shader compiler.
// This mean that decals will use our custom lighting as well.

#ifdef _DBUFFER
    #undef _DBUFFER
    #define _CUSTOMDBUFFER
#endif

//  Support for accurate G-Buffer normals
//#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

#if !defined(SHADERGRAPH_PREVIEW) || defined(UNIVERSAL_LIGHTING_INCLUDED)

//  As we do not have access to the vertex lights we will make the shader always sample add lights per pixel
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
        #undef _ADDITIONAL_LIGHTS_VERTEX
        #define _ADDITIONAL_LIGHTS
    #endif


// LightingPhysicallyBased - without clearcoat
    half3 LightingPhysicallyBased_Lux(BRDFData brdfData,
        half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
        half3 normalWS, half3 viewDirectionWS,
        bool specularHighlightsOff)
    {
        half NdotL = saturate(dot(normalWS, lightDirectionWS));
        half3 radiance = lightColor * (lightAttenuation * NdotL);

        half3 brdf = brdfData.diffuse;
    #ifndef _SPECULARHIGHLIGHTS_OFF
        [branch] if (!specularHighlightsOff)
        {
            brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
        }
    #endif // _SPECULARHIGHLIGHTS_OFF
        return brdf * radiance;
    }
#endif

void Lighting_half(

//  Base inputs
    float3 positionWS,
    half3 viewDirectionWS,

//  Normal inputs    
    half3 normalWS,
    half3 tangentWS,
    half3 bitangentWS,
    bool enableNormalMapping,
    half3 normalTS,

//  Surface description
    half3 albedo,
    half metallic,
    half3 specular,
    half smoothness,
    half occlusion,
    half alpha,

//  Lightmapping
    float2 staticLightmapUV,
    float2 dynamicLightMapUV,

    half3 vertexSH,
    float4 ProbeOcclusion,

//  Final lit color
    out half3 MetaAlbedo,
    out half3 FinalLighting,
    out half3 MetaSpecular, 
    out half  MetaSmoothness,
    out half  MetaOcclusion,
    out half3 MetaNormal
)
{

#if defined(SHADERGRAPH_PREVIEW)
    FinalLighting = albedo;
    MetaAlbedo = half3(0,0,0);
    MetaSpecular = half3(0,0,0);
    MetaSmoothness = 0;
    MetaOcclusion = 0;
    MetaNormal = half3(0,0,1);
#else

//  Real Lighting

//  Has to be zero initialized    
    half3x3 tangentToWorld = (half3x3)0;

    if (enableNormalMapping)
    {
        tangentToWorld = half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz);
        normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    }
    normalWS = NormalizeNormalPerPixel(normalWS);
    viewDirectionWS = SafeNormalize(viewDirectionWS);

//  Reconstruct positionCS - this is positionCS like in the vertex shader
    float4 positionCS = TransformWorldToHClip(positionWS);

    float2 normalizedScreenSpaceUV = positionCS.xy;
    normalizedScreenSpaceUV /= positionCS.w;
    normalizedScreenSpaceUV = normalizedScreenSpaceUV * 0.5f + 0.5f;
    #if UNITY_UV_STARTS_AT_TOP
        normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
    #endif


//  GI Lighting

//  These have to be zero initialized, otherwise decals and debug error!
    half3 bakedGI = (half3)0.0;
    half4 t_shadowMask = (half4)0.0;

    #if defined(DYNAMICLIGHTMAP_ON)
        dynamicLightMapUV = dynamicLightMapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
        bakedGI = SAMPLE_GI(staticLightmapUV, dynamicLightmapUV, vertexSH, diffuseNormalWS);
        staticLightmapUV = staticLightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
        t_shadowMask = SAMPLE_SHADOWMASK(staticLightmapUV);
    #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
        bakedGI = SAMPLE_GI(
            vertexSH,                                               
            GetAbsolutePositionWS(positionWS),
            normalWS,
            viewDirectionWS,
            normalizedScreenSpaceUV * _ScreenSize.xy,
            ProbeOcclusion,
            t_shadowMask 
        );
    #else
        staticLightmapUV = staticLightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
        bakedGI = SAMPLE_GI(staticLightmapUV, vertexSH, normalWS);
        t_shadowMask = SAMPLE_SHADOWMASK(staticLightmapUV);
    #endif

//  Fill standard URP structs so we can use the built in functions
    InputData inputData = (InputData)0;
    {
        inputData.positionWS = positionWS;
        inputData.normalWS = normalWS;
        inputData.viewDirectionWS = viewDirectionWS;
        inputData.bakedGI = bakedGI;
        #if _MAIN_LIGHT_SHADOWS_SCREEN
            inputData.shadowCoord = ComputeScreenPos(positionCS);
        #else
            inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
        #endif
        //  Apply perspective division
        inputData.normalizedScreenSpaceUV = normalizedScreenSpaceUV;
        inputData.shadowMask = t_shadowMask;

        #if defined(DEBUG_DISPLAY)
            inputData.positionCS = positionCS; //?
            #if defined(DYNAMICLIGHTMAP_ON)
                inputData.dynamicLightmapUV = dynamicLightmapUV;
            #endif
            #if defined(LIGHTMAP_ON)
                inputData.staticLightmapUV = staticLightmapUV;
            #else
                inputData.vertexSH = vertexSH;
            #endif
        #endif
    }
    SurfaceData surfaceData = (SurfaceData)0;
    {
        surfaceData.alpha = alpha;
        surfaceData.albedo = albedo;
        surfaceData.metallic = metallic;
        surfaceData.specular = specular;
        surfaceData.smoothness = smoothness;
        surfaceData.occlusion = occlusion;   
    }
//  END: structs

//  Decals
    #if defined(_CUSTOMDBUFFER)
        float2 positionDS = inputData.normalizedScreenSpaceUV * _ScreenSize.xy;
        ApplyDecalToSurfaceData(float4(positionDS, 0, 0), surfaceData, inputData);
    #endif

//  From here on we rely on surfaceData and inputData only! (except debug which outputs the original values)

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

//  Debugging
    #if defined(DEBUG_DISPLAY)
        // half4 debugColor;
        // if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
        // {
        //     FinalLighting = debugColor.rgb;
        //     MetaAlbedo = debugColor.rgb;
        //     MetaSpecular = specular;
        //     MetaSmoothness = smoothness;
        //     MetaOcclusion = occlusion;
        //     MetaNormal = normalTS;
        // }
        FinalLighting = 0;
        MetaAlbedo = albedo;
        MetaSpecular = specular;
        MetaSmoothness = smoothness;
        MetaOcclusion = occlusion;
        MetaNormal = normalTS;
    #else
//  Debugging

    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();

    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    half3 mainLightColor = mainLight.color;

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

//  In order to use probe blending and proper AO we have to use the new GlobalIllumination function
    lightingData.giColor = GlobalIllumination(
        brdfData,
        brdfData,   // brdfDataClearCoat,
        0,          // surfaceData.clearCoatMask
        inputData.bakedGI,
        aoFactor.indirectAmbientOcclusion,
        inputData.positionWS,
        inputData.normalWS,
        inputData.viewDirectionWS,
        inputData.normalizedScreenSpaceUV
    );

#ifdef _SPECULARHIGHLIGHTS_OFF
    bool specularHighlightsOff = true;
#else
    bool specularHighlightsOff = false;
#endif

#if defined(_LIGHT_LAYERS)
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor = LightingPhysicallyBased_Lux(
            brdfData, mainLight.color, mainLight.direction, mainLight.distanceAttenuation * mainLight.shadowAttenuation,
            inputData.normalWS, inputData.viewDirectionWS, specularHighlightsOff
        );
    }

    #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
            {
                FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #if defined(_LIGHT_LAYERS)
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                {
                    lightingData.additionalLightsColor += LightingPhysicallyBased_Lux(
                        brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation,
                        inputData.normalWS, inputData.viewDirectionWS, specularHighlightsOff
                    );
                }
            }
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)    
                Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #if defined(_LIGHT_LAYERS)
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                {
                    lightingData.additionalLightsColor += LightingPhysicallyBased_Lux(
                        brdfData, light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation,
                        inputData.normalWS, inputData.viewDirectionWS, specularHighlightsOff
                    );
                }
        LIGHT_LOOP_END
    #endif

    FinalLighting = CalculateFinalColor(lightingData, surfaceData.alpha).xyz;

//FinalLighting = inputData.normalWS; //mainLight.color; //brdfData.diffuse; //lightingData.mainLightColor;


//  Set Albedo for meta pass
    #if defined(UNIVERSAL_META_PASS_INCLUDED)
        FinalLighting = half3(0,0,0);
        MetaAlbedo = albedo;
        MetaSpecular = specular;
        MetaSmoothness = 0;
        MetaOcclusion = 0;
        MetaNormal = half3(0,0,1);
    #else
        MetaAlbedo = half3(0,0,0);
        MetaSpecular = half3(0,0,0);
        MetaSmoothness = 0;
        MetaOcclusion = 0;
    //  Needed by DepthNormalOnly pass
        MetaNormal = normalTS;
    #endif

//  End Real Lighting ----------

    #endif // end debug

#endif
}

void Lighting_float(

//  Base inputs
    float3 positionWS,
    half3 viewDirectionWS,

//  Normal inputs    
    half3 normalWS,
    half3 tangentWS,
    half3 bitangentWS,
    bool enableNormalMapping,
    half3 normalTS,

//  Surface description
    half3 albedo,
    half metallic,
    half3 specular,
    half smoothness,
    half occlusion,
    half alpha,

//  Lightmapping
    float2 staticLightmapUV,
    float2 dynamicLightMapUV,

    half3 vertexSH,
    float4 ProbeOcclusion,

//  Final lit color
    out half3 MetaAlbedo,
    out half3 FinalLighting,
    out half3 MetaSpecular,
    out half  MetaSmoothness,
    out half  MetaOcclusion,
    out half3 MetaNormal
)
{
    Lighting_half(
        positionWS, viewDirectionWS, normalWS, tangentWS, bitangentWS, enableNormalMapping, normalTS, 
        albedo, metallic, specular, smoothness, occlusion, alpha,
        staticLightmapUV, dynamicLightMapUV, vertexSH, ProbeOcclusion,
        MetaAlbedo, FinalLighting, MetaSpecular, MetaSmoothness, MetaOcclusion, MetaNormal
    );
}