// GaussianBlurWithAlpha.shader
Shader "Custom/GaussianBlurWithAlpha"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 0.1)) = 0.01
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.01
        _Darkness ("Darkness", Range(0, 1)) = 0.5
        [Toggle(USE_ALPHA_MASK)] _UseAlphaMask ("Use Alpha Mask", Float) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ USE_ALPHA_MASK
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            float _AlphaCutoff;
            float _Darkness;
            
            // 高斯权重函数
            float gaussianWeight(float x, float sigma)
            {
                return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * UNITY_PI) * sigma);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            // 只对非透明区域进行模糊
            float4 blurWithAlphaPreservation(float2 uv, float blurSize)
            {
                // 高斯核参数
                const float sigma = 1.0;
                const int KERNEL_RADIUS = 4;
                
                float4 accumulatedColor = float4(0, 0, 0, 0);
                float accumulatedWeight = 0.0;
                float accumulatedAlphaWeight = 0.0;
                
                // 预计算权重
                float weights[9];
                for (int i = 0; i <= KERNEL_RADIUS * 2; i++)
                {
                    float x = float(i - KERNEL_RADIUS);
                    weights[i] = gaussianWeight(x, sigma);
                }
                
                // 采样周围像素
                for (int y = -KERNEL_RADIUS; y <= KERNEL_RADIUS; y++)
                {
                    for (int x = -KERNEL_RADIUS; x <= KERNEL_RADIUS; x++)
                    {
                        float2 offset = float2(x, y) * blurSize;
                        float2 sampleUV = uv + offset;
                        
                        // 采样颜色和透明度
                        float4 sampleColor = tex2D(_MainTex, sampleUV);
                        
                        // 获取当前权重
                        float weight = weights[x + KERNEL_RADIUS] * weights[y + KERNEL_RADIUS];
                        
                        // **关键修复：根据Alpha值调整权重**
                        float alphaWeight = weight * sampleColor.a;
                        
                        // 只累加有效颜色的贡献
                        if (sampleColor.a > _AlphaCutoff)
                        {
                            accumulatedColor.rgb += sampleColor.rgb * alphaWeight;
                            accumulatedAlphaWeight += alphaWeight;
                        }
                        
                        // 始终累加Alpha用于最终计算
                        accumulatedColor.a += sampleColor.a * weight;
                        accumulatedWeight += weight;
                    }
                }
                
                // 防止除以零
                if (accumulatedAlphaWeight > 0.001)
                {
                    accumulatedColor.rgb /= accumulatedAlphaWeight;
                }
                else
                {
                    // 如果没有任何有效像素，返回原始像素
                    accumulatedColor = tex2D(_MainTex, uv);
                }
                
                // 归一化Alpha
                if (accumulatedWeight > 0.001)
                {
                    accumulatedColor.a /= accumulatedWeight;
                }
                
                return accumulatedColor;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // 方法1：使用Alpha保护的高斯模糊
                float4 col = blurWithAlphaPreservation(i.uv, _BlurSize);
                
                // 方法2：边缘检测模糊（替代方案）
                // float4 col = edgeAwareBlur(i.uv, _BlurSize);
                
                // 应用变暗效果
                col.rgb *= (1.0 - _Darkness);
                
                // 乘以顶点颜色（如果使用）
                col *= i.color;
                
                // Alpha测试
                #ifdef USE_ALPHA_MASK
                if (col.a < _AlphaCutoff)
                    discard;
                #endif
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}