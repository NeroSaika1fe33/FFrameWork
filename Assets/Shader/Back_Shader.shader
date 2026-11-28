Shader "Custom/CyberGlitchBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Background Texture", 2D) = "white" {}
        _Color ("UI Tint", Color) = (1, 1, 1, 1)
        _EffectIntensity ("Effect Overlay Intensity", Range(0, 1)) = 0.35
        _TargetCount ("Digital Particle Count", Range(1, 30)) = 15
        _GridCellSize ("Grid Cell Size", Range(0.01, 0.5)) = 0.1
        _GlowIntensity ("Glow Intensity", Range(0.1, 3.0)) = 1.0
        _VoronoiScale ("Voronoi Movement Scale", Range(1.0, 10.0)) = 4.0
        _ColorA ("Color System A (Cyber Green)", Color) = (0.0, 1.0, 0.4, 1.0)
        _ColorB ("Color System B (Digital Blue)", Color) = (0.0, 0.6, 1.0, 1.0)
        _GlitchAmount ("Tan Glitch Amount", Range(0.0, 1.0)) = 0.1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }
        LOD 100

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #define PI 3.14159265359

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            float4 _ClipRect;
            float _EffectIntensity;
            float _TargetCount;
            float _GridCellSize;
            float _GlowIntensity;
            float _VoronoiScale;
            float4 _ColorA;
            float4 _ColorB;
            float _GlitchAmount;

            // 2D 座標から疑似乱数を生成するハッシュ関数
            float2 rand2(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.9730));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // 粒子 ID と時間から、揺らぎとグリッチを含む位置を生成する。
            float2 getVoronoiRandomPos(float particleID, float time, float seedOffset)
            {
                float2 cellID = float2(particleID * 1.71, seedOffset * 3.14);
                float2 baseRand = rand2(cellID);
                
                // sin / cos で位相をずらし、滑らかな揺らぎを加える。
                float2 jitter = float2(
                    sin(time * 1.5 + baseRand.x * 12.56 + seedOffset),
                    cos(time * 1.2 + baseRand.y * 12.56 + particleID)
                );
                
                float2 pos = (baseRand - 0.5) * 1.2 + jitter * 0.25;

                // tan の入力角を PI 周期で折り返し、時間による角度の増大を抑える。
                float tanAngle = fmod(time * 0.5 + baseRand.x * 6.28, PI) - (PI * 0.5);
                
                // tan の出力を -10 ～ 10 に制限し、グリッチによる変位を抑える。
                float rawTan = tan(tanAngle);
                float safeTan = clamp(rawTan, -10.0, 10.0);

                float glitch = safeTan * 0.05 * _GlitchAmount;
                pos += float2(glitch, glitch * 0.5);

                return pos;
            }

            float2 getGridPosition(float2 uv)
            {
                return uv / max(_GridCellSize, 0.001);
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 background = tex2D(_MainTex, i.uv) + _TextureSampleAdd;

                // 1. UV の中心を原点へ移し、画面のアスペクト比を補正する。
                float2 uv = (i.uv - 0.5);
                uv.x *= _ScreenParams.x / _ScreenParams.y;

                // 2. グリッド座標を求め、後続の除算で使う値に下限を設ける。
                float2 gridBoundUV = getGridPosition(uv);
                float2 cellBoundUV = gridBoundUV - round(gridBoundUV);
                cellBoundUV = max(abs(cellBoundUV), 0.005); // 下限を設け、グリッド境界での過剰な発光を抑える。

                float intensityA = 0.0;
                float intensityB = 0.0;

                // 時間を 3600 秒周期で折り返し、長時間再生時の計算値の増大を抑える。
                float time = fmod(_Time.y, 3600.0); 
                int targetCount = (int)clamp(round(_TargetCount), 1.0, 30.0);

                // 3. Voronoi 表現用の疑似乱数位置を使い、発光成分 A を加算する。
                for (int tA = 0; tA < targetCount; ++tA)
                {
                    float f_targetIndex = float(tA);
                    float2 targetPosition = getVoronoiRandomPos(f_targetIndex, time, 1.0);

                    float2 gridBoundTargetPosition = getGridPosition(targetPosition);
                    float2 edgeBoundPosition = gridBoundTargetPosition;

                    float distanceToTarget = length(gridBoundUV - round(gridBoundTargetPosition)) + 
                                             length(gridBoundUV - edgeBoundPosition);

                    float energy = length(_GridCellSize / (max(distanceToTarget, 0.001) * 9.5) / cellBoundUV) * _GridCellSize;
                    intensityA += energy;
                }

                // 4. 速度とシードを変えた位置を使い、発光成分 B を加算する。
                for (int tB = 0; tB < targetCount; ++tB)
                {
                    float f_targetIndex = float(tB);
                    float2 targetPosition = getVoronoiRandomPos(f_targetIndex, time * 0.8, 2.5);

                    float2 gridBoundTargetPosition = getGridPosition(targetPosition);
                    float2 edgeBoundPosition = gridBoundTargetPosition;

                    float distanceToTarget = length(gridBoundUV - round(gridBoundTargetPosition)) + 
                                             distance(gridBoundUV, edgeBoundPosition);

                    float energy = length(_GridCellSize / (max(distanceToTarget, 0.001) * 15.5) / cellBoundUV) * _GridCellSize;
                    intensityB += energy;
                }

                // 発光成分の上限を設け、過剰な明るさを抑える。
                intensityA = clamp(intensityA * _GlowIntensity, 0.0, 10.0);
                intensityB = clamp(intensityB * _GlowIntensity, 0.0, 10.0);

                // 5. 中心の白い発光に、A / B の 2 色を合成する。
                float totalEnergy = intensityA + intensityB;
                float3 col = float3(smoothstep(0.2, 1.0, totalEnergy), 
                                    smoothstep(0.2, 1.0, totalEnergy), 
                                    smoothstep(0.2, 1.0, totalEnergy)) * 0.2;

                col += intensityA * _ColorA.rgb;
                col += intensityB * _ColorB.rgb;

                // 6. ビネットを適用し、画面の周辺を暗くする。
                float vignette = smoothstep(1.2, 0.2, length(uv));
                col *= vignette;

                // 7. 背景画像と合成し、UI のクリッピングとアルファ切り抜きを適用する。
                col = (background.rgb + col * _EffectIntensity) * i.color.rgb;
                fixed alpha = saturate(background.a * i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
