Shader "AlleyStealth/VisionOverlay"
{
    Properties { _Color ("Cone color", Color) = (1, 0.12, 0.18, 0.4) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
