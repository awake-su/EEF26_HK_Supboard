// Based on the default created with:
// Create > Shader > HDRP PostProcess

Shader "Kamgam/UGUI/HDRP/Blur Shader"
{
    Properties
    {
        // This property is necessary to make the CommandBuffer.Blit bind the source texture to _MainTex
        _MainTex("Main Texture", 2D) = "white" {}
        _BlurOffset("Blur Offset", Vector) = (1.0, 1.0, 0)
        [KeywordEnum(Low, Medium, High)] _Samples("Sample Amount", Float) = 1
        _AdditiveColor("Additive Color", Color) = (0, 0, 0, 0)
    }

    HLSLINCLUDE

    #if _SAMPLES_LOW

        #define SAMPLES 10

    #elif _SAMPLES_MEDIUM

        #define SAMPLES 30

    #else

        #define SAMPLES 100

    #endif

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma multi_compile _SAMPLES_LOW _SAMPLES_MEDIUM _SAMPLES_HIGH

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float3 _BlurOffset;
    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    float4 _AdditiveColor;

    float4 _MainTex_TexelSize;

    // Based on linear sampling on the GPU.
    // Weights from this excellent article:
    // https://www.rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/

    static const float offset[3] = { 0.0, 1.3846153846, 3.2307692308 };
    static const float weight[3] = { 0.2270270270, 0.3162162162, 0.0702702703 };

    float4 BlurHorizontal(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        //float2 uv = ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy);
        float2 uv = input.texcoord.xy;

        // See: https://forum.unity.com/threads/_maintex_texelsize-whats-the-meaning.110278/
        // For a 1024 x 1024 texture this will be 1 / 1024.
        float2 uv2px = _MainTex_TexelSize.xy;

        // star form, blur with a sample for every step
        half4 color;
        float sampleDiv = float(SAMPLES - 1.0);
        float invSampleDiv = 1.0 / sampleDiv;
        float weightSum = 0;
        for (float i = 0; i < SAMPLES; i++)
        {
            // Linear kernel weight interpolation
            float weight = 0.5 + (0.5 - abs(i * invSampleDiv - 0.5));
            weightSum += weight;

            // x
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2((i * invSampleDiv - 0.5) * _BlurOffset.x, 0.0) * uv2px) * weight;
        }
        color /= max(weightSum, 0.0001); // PlayStation 5 Fix
        color.a = 1;

        return color;
    }

    float4 BlurVertical(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        //float2 uv = ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy);
        float2 uv = input.texcoord.xy;

        // See: https://forum.unity.com/threads/_maintex_texelsize-whats-the-meaning.110278/
        // For a 1024 x 1024 texture this will be 1 / 1024.
        float2 uv2px = _MainTex_TexelSize.xy;

        // star form, blur with a sample for every step
        half4 color;
        float sampleDiv = float(SAMPLES - 1.0);
        float invSampleDiv = 1.0 / sampleDiv;
        float weightSum = 0;
        for (float i = 0; i < SAMPLES; i++)
        {
            // Linear kernel weight interpolation
            float weight = 0.5 + (0.5 - abs(i * invSampleDiv - 0.5));
            weightSum += weight;

            // y
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, (i * invSampleDiv - 0.5) * _BlurOffset.y) * uv2px) * weight;
        }
        color /= max(weightSum, 0.0001); // PlayStation 5 Fix
        color.a = 1;

        color += _AdditiveColor;

        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Blur Horizontal"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment BlurHorizontal
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vertical"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment BlurVertical
            ENDHLSL
        }
    }
    Fallback Off
}