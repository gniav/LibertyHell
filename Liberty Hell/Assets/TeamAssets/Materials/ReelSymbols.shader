Shader "Liberty Hell/Reel Symbols"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }
            half4 frag(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // The atlas repeats three icons. Give the second cherry and lemon
                // their own colours permanently, so all five outcomes are on the belt.
                float2 uv = frac(input.uv);
                float inside = step(0.276, uv.x) * step(uv.x, 0.724);
                float purpleMask = inside * step(abs(uv.y - 0.4243), 0.056);
                float redMask = inside * step(abs(uv.y - 0.2651), 0.056);
                half fruit = step(0.12h, texel.r) * step(texel.g * 1.2h, texel.r);
                half3 purple = half3(texel.r * 0.65h, texel.g * 0.35h, max(texel.r, texel.b));
                texel.rgb = lerp(texel.rgb, purple, purpleMask * fruit);
                texel.rgb = lerp(texel.rgb, texel.rgb * half3(1, 0.03, 0.2), redMask);
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 illumination = SampleSH(normalWS) + mainLight.color * mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation * saturate(dot(normalWS, mainLight.direction));
                return half4(texel.rgb * _BaseColor.rgb * illumination, texel.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
