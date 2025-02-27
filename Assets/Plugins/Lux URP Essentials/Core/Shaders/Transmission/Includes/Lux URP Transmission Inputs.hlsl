#ifndef INPUT_LUXURP_BASE_INCLUDED
#define INPUT_LUXURP_BASE_INCLUDED

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//  defines a bunch of helper functions (like lerpwhiteto)
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"  
//  defines SurfaceData, textures and the functions Alpha, SampleAlbedoAlpha, SampleNormal, SampleEmission
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

//  Must be declared before we can include Lighting.hlsl
    struct AdditionalSurfaceData
    {
        half translucency;
        half mask;
    };

//  defines e.g. "DECLARE_LIGHTMAP_OR_SH"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//  Moved down so we have access to the cbuffer 
    //#include "../Includes/Lux URP Translucent Lighting.hlsl"
    //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

//  Material Inputs
    CBUFFER_START(UnityPerMaterial)

        half4   _BaseColor;
        half    _Smoothness;
        half    _Metallic;
        half4   _SpecColor;

        half    _Cutoff;

        half    _ShadowOffset;

    //  Needed by LitMetaPass
        float4  _BaseMap_ST;
        
        float4  _BumpMap_ST;
        half    _BumpScale;
        float4  _MaskMap_ST;

        half    _Occlusion;

        half    _TranslucencyPower;
        half    _TranslucencyStrength;
        half    _ShadowStrength;
        half    _MaskByShadowStrength;
        half    _Distortion;

        half    _OverrideTransmission;
        half3   _TransmissionColor;

        half    _CustomWrap;

        half4   _RimColor;
        half    _RimPower;
        half    _RimMinPower;
        half    _RimFrequency;
        half    _RimPerPositionFrequency;

        half    _Surface;
            
    CBUFFER_END

    #include "../Includes/Lux URP Translucent Lighting.hlsl"

//  Additional textures
    #if defined(_MASKMAP)
        TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
    #endif

//  Global Inputs

//  DOTS - we only define a minimal set here. The user might extend it to whatever is needed.
    #ifdef UNITY_DOTS_INSTANCING_ENABLED
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
            UNITY_DOTS_INSTANCED_PROP(float , _Surface)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        
        #define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor)
        #define _Surface                UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface)
    #endif

//  Structs
    struct VertexInput
    {
        float3 positionOS                   : POSITION;
        float3 normalOS                     : NORMAL;
        float4 tangentOS                    : TANGENT;
        float2 texcoord                     : TEXCOORD0;
        float2 lightmapUV                   : TEXCOORD1;
   //   half4 color                         : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    struct VertexOutput
    {
        float4 positionCS                   : SV_POSITION;

        #if defined(_MASKMAP)
            float4 uv                       : TEXCOORD0;
        #else
            float2 uv                       : TEXCOORD0;
        #endif

        #if !defined(UNITY_PASS_SHADOWCASTER) && !defined(DEPTHONLYPASS)
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
            #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
                float3 positionWS           : TEXCOORD2;
            #endif
            float3 normalWS                 : TEXCOORD3;
            float3 viewDirWS                : TEXCOORD4;
            #if defined(_NORMALMAP)
                float4 tangentWS            : TEXCOORD5;
            #endif
            half4 fogFactorAndVertexLight   : TEXCOORD6;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord          : TEXCOORD7;
            #endif
        #endif

        #ifdef USE_APV_PROBE_OCCLUSION
            float4 probeOcclusion           : TEXCOORD10;
        #endif

        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    struct SurfaceDescription
    {
        half3 albedo;
        half alpha;
        half3 normalTS;
        half3 emission;
        half metallic;
        half3 specular;
        half smoothness;
        half occlusion;

        half translucency;
        half mask;
    };

#endif