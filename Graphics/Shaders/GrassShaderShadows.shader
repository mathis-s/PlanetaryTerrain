// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

//------------MODIFIED------------//
// Modified to work with spherical surfaces and added basic lighting
// Special Thanks to Adrian Kors for implementing shadows!

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Low Poly Shader developed as part of World of Zero: http://youtube.com/worldofzerodevelopment
// Based upon the example at: http://www.battlemaze.com/?p=153

Shader "PlanetaryTerrain/Grass Geometry Shader (with shadows)" {
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _GrassHeight("Grass Height", Float) = 0.25
        _GrassWidth("Grass Width", Float) = 0.25
        _AlphaClip("Alpha Clipping", Range(0,1)) = 0.25
        _WindStength("Wind Strength", Float) = 0.25
        _WindSpeed("Wind Speed", Range(0,500)) = 1.0
        _Angle("Angle", Range(0,359)) = 0
    }
        SubShader
        {
            LOD 200
            Pass
            {
                Tags{ "LightMode" = "ForwardBase" "RenderType" = "Opaque" }
                CULL OFF
                CGPROGRAM
                #include "UnityCG.cginc"
                #include "UnityLightingCommon.cginc" // for _LightColor0
                #pragma vertex vert
                #pragma fragment frag
                #pragma geometry geom
                #define USE_THREE_QUADS
                // compile shader into multiple variants, with and without shadows
                // (we don't care about any lightmaps yet, so skip these variants)
                #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
                // shadow helper functions and macros
                #include "AutoLight.cginc"
                // Use shader model 4.0 target, we need geometry shader support
                #pragma target 4.0
                sampler2D _MainTex;
                struct v2g
                {
                    float4 pos : SV_POSITION;
                    float3 norm : NORMAL;
                    float2 uv : TEXCOORD0;
                    float3 color : TEXCOORD1;
                };
                struct g2f
                {
                    float4 pos : SV_POSITION;
                    float3 norm : NORMAL;
                    float2 uv : TEXCOORD0;
                    fixed4 diff : COLOR0; // diffuse lighting color
                    fixed3 ambient : COLOR1;
                    SHADOW_COORDS(2) // put shadow data into TEXCOORD2
                };
                half _Glossiness;
                half _Metallic;
                fixed4 _Color;
                half _GrassHeight;
                half _GrassWidth;
                half _AlphaClip;
                half _WindStength;
                half _WindSpeed;
                half _Angle;
                v2g vert(appdata_full v)
                {
                    float3 v0 = v.vertex.xyz;
                    v2g OUT;
                    OUT.pos = v.vertex;
                    OUT.norm = v.normal;
                    OUT.uv = v.texcoord;
                    OUT.color = tex2Dlod(_MainTex, v.texcoord).rgb;
                    return OUT;
                }
                [maxvertexcount(24)] void geom(point v2g IN[1], inout TriangleStream<g2f> triStream) {
                float3 lightPosition = _WorldSpaceLightPos0;
                float3 perpendicularAngle;
                float3 v0 = IN[0].pos.xyz;
                float3 v1 = IN[0].pos.xyz + IN[0].norm * _GrassHeight;
                float3 wind = float3(sin(_Time.x * _WindSpeed + v0.x) + sin(_Time.x * _WindSpeed + v0.z * 2) + sin(_Time.x * _WindSpeed * 0.1 + v0.x), 0,
                                     cos(_Time.x * _WindSpeed + v0.x * 2) + cos(_Time.x * _WindSpeed + v0.z));
                v1 += wind * _WindStength;
                float diffuseLighting = dot(IN[0].norm, mul((float3x3)unity_WorldToObject, normalize(_WorldSpaceLightPos0.xyz))); //normalize(_WorldSpaceLightPos0.xyz)
                float3 down = -IN[0].norm;
                if (abs(down.x) > 0.1)
                {
                    perpendicularAngle = normalize(float3(-down.y / down.x, 1, 0));
                }
                else if (abs(down.y) > 0.1)
                {
                    perpendicularAngle = normalize(float3(1, -down.x / down.y, 0));
                }
                else
                {
                    perpendicularAngle = normalize(float3(1, 0, -down.x / down.z));
                }
                float3 faceNormal = cross(perpendicularAngle, IN[0].norm);
                half3 worldNormal = UnityObjectToWorldNormal(IN[0].norm);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                float3 perpendicularAngle90 = cross(down, perpendicularAngle);
                #ifdef USE_THREE_QUADS
                perpendicularAngle90 *= 0.866025;
                float3 onetwenty = (-0.5 * perpendicularAngle) + perpendicularAngle90;
                float3 twoforty = (-0.5 * perpendicularAngle) - perpendicularAngle90;
                #else
                float3 onetwenty = perpendicularAngle90; //use only two quads per grass, angle between quads has to be 90 not 120
                #endif
                //void buildQuad(inout TriangleStream<g2f> triStream, float3 points[4], float3 color, half nl, half3 worldNormal)
                //{
                    g2f OUT;
                    /*
                    float3 faceNormal = cross(points[1] - points[0], points[2] - points[0]);
                    for (int i = 0; i < 4; ++i)
                    {
                        OUT.pos = UnityObjectToClipPos(points[i]);
                        OUT.norm = faceNormal;
                        OUT.diffuseColor = color;
                        OUT.uv = float2(i % 2, (int)i / 2);
                        OUT.diff = nl * _LightColor0;
                        OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                        OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                        TRANSFER_SHADOW(OUT);
                        triStream.Append(OUT);
                    }
                    triStream.RestartStrip();
                    */
                // Quad 1
                OUT.pos = UnityObjectToClipPos(v0 + perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(1, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 + perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(1, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 - perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0 - perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                //Quad 2
                OUT.pos = UnityObjectToClipPos(v0 + onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(1, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 + onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(1, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 - onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0 - onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                //third quad only rendered if three quads are used
                #ifdef USE_THREE_QUADS
                OUT.pos = UnityObjectToClipPos(v0 + twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(1, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 + twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(1, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 - twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0 - twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 0);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                //OUT.diffuseLighting = diffuseLighting;
                OUT.uv = float2(0.5, 1);
                OUT.diff = nl * _LightColor0;
                OUT.diff.rgb += ShadeSH9(half4(worldNormal, 1));
                OUT.ambient = ShadeSH9(half4(worldNormal, 1));
                TRANSFER_SHADOW(OUT);
                triStream.Append(OUT);
                #endif
                }
                half4 frag(g2f IN) : COLOR
                {
                    fixed4 c = tex2D(_MainTex, IN.uv);
                    clip(c.a - _AlphaClip);
                    // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                    fixed shadow = SHADOW_ATTENUATION(IN);
                    // darken light's illumination with shadow, keep ambient intact
                    fixed3 lighting = IN.diff * shadow + IN.ambient;
                    c.rgb *= lighting;
                    c *= IN.diff;
                    return c;
                }
                ENDCG
            }
            Pass
            {
                Tags{ "LightMode" = "ShadowCaster" }
                CULL OFF
                CGPROGRAM
                #include "UnityCG.cginc"
                #include "UnityLightingCommon.cginc" // for _LightColor0
                #pragma vertex vert
                #pragma fragment frag
                #pragma geometry geom
                // Use shader model 4.0 target, we need geometry shader support
                #pragma target 4.0
                sampler2D _MainTex;
                struct v2g
                {
                    float4 pos : SV_POSITION;
                    float3 norm : NORMAL;
                    float2 uv : TEXCOORD0;
                    float3 color : TEXCOORD1;
                };
                struct g2f
                {
                    float4 pos : SV_POSITION;
                    float3 norm : NORMAL;
                    float2 uv : TEXCOORD0;
                };
                half _GrassHeight;
                half _GrassWidth;
                half _AlphaClip;
                half _WindSpeed;
                half _WindStength;
                v2g vert(appdata_full v)
                {
                    float3 v0 = v.vertex.xyz;
                    v2g OUT;
                    OUT.pos = v.vertex;
                    OUT.norm = v.normal;
                    OUT.uv = v.texcoord;
                    return OUT;
                }
                [maxvertexcount(24)] void geom(point v2g IN[1], inout TriangleStream<g2f> triStream) {
                float3 lightPosition = _WorldSpaceLightPos0;
                float3 perpendicularAngle;
                float3 v0 = IN[0].pos.xyz;
                float3 v1 = IN[0].pos.xyz + IN[0].norm * _GrassHeight;
                float3 wind = float3(sin(_Time.x * _WindSpeed + v0.x) + sin(_Time.x * _WindSpeed + v0.z * 2) + sin(_Time.x * _WindSpeed * 0.1 + v0.x), 0,
                                     cos(_Time.x * _WindSpeed + v0.x * 2) + cos(_Time.x * _WindSpeed + v0.z));
                v1 += wind * _WindStength;
                float diffuseLighting = dot(IN[0].norm, mul((float3x3)unity_WorldToObject, normalize(_WorldSpaceLightPos0.xyz))); //normalize(_WorldSpaceLightPos0.xyz)
                float3 down = -IN[0].norm;
                if (abs(down.x) > 0.1)
                {
                    perpendicularAngle = normalize(float3(-down.y / down.x, 1, 0));
                }
                else if (abs(down.y) > 0.1)
                {
                    perpendicularAngle = normalize(float3(1, -down.x / down.y, 0));
                }
                else
                {
                    perpendicularAngle = normalize(float3(1, 0, -down.x / down.z));
                }
                float3 faceNormal = cross(perpendicularAngle, IN[0].norm);
                float3 perpendicularAngle90 = cross(down, perpendicularAngle);
                #ifdef USE_THREE_QUADS
                perpendicularAngle90 *= 0.866025;
                float3 onetwenty = (-0.5 * perpendicularAngle) + perpendicularAngle90;
                float3 twoforty = (-0.5 * perpendicularAngle) - perpendicularAngle90;
                #else
                float3 onetwenty = perpendicularAngle90; //use only two quads per grass, angle between quads has to be 90 not 120
                #endif
                g2f OUT;
                // Quad 1
                OUT.pos = UnityObjectToClipPos(v0 + perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(1, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 + perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(1, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 - perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(0, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0 - perpendicularAngle * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(0, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 1);
                triStream.Append(OUT);
                //Quad 2
                OUT.pos = UnityObjectToClipPos(v0 + onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(1, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 + onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(1, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 - onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(0, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0 - onetwenty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(0, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 1);
                triStream.Append(OUT);
                //third quad only rendered if three quads are used
                #ifdef USE_THREE_QUADS
                OUT.pos = UnityObjectToClipPos(v0 + twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(1, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 + twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(1, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1 - twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(0, 1);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0 - twoforty * 0.5 * _GrassHeight);
                OUT.norm = faceNormal;
                OUT.uv = float2(0, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v0);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 0);
                triStream.Append(OUT);
                OUT.pos = UnityObjectToClipPos(v1);
                OUT.norm = faceNormal;
                OUT.uv = float2(0.5, 1);
                triStream.Append(OUT);
                #endif
            }
                half4 frag(g2f IN) : COLOR
                {
                    fixed4 c = tex2D(_MainTex, IN.uv);
                    clip(c.a - _AlphaClip);
                    return c;
                }
                    ENDCG
            }
        }
}