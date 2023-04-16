// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "PlanetaryTerrain/PlanetSurfaceShaderBump" {
	Properties {
		
		_Tex1 ("Texture 0 (Beach)", 2D) = "white" {}
		_Nor1 ("Normal", 2D) = "bump" {}
		_Color1 ("Color", Color) = (1,1,1,1)
		_Glossiness1 ("Smoothness", Range(0,1)) = 0.5
		_Metallic1 ("Metallic", Range(0,1)) = 0.0
		_TexScale1 ("Texture Scale", Float) = 1
		
		_Tex2 ("Texture 1", 2D) = "white" {}
		_Nor2 ("Normal", 2D) = "bump" {}
		_Color2 ("Color", Color) = (1,1,1,1)
		_Glossiness2 ("Smoothness", Range(0,1)) = 0.5
		_Metallic2 ("Metallic", Range(0,1)) = 0.0
		_TexScale2 ("Texture Scale", Float) = 1

		_Tex3 ("Texture 2", 2D) = "white" {}
		_Nor3 ("Normal", 2D) = "bump" {}
		_Color3 ("Color", Color) = (1,1,1,1)
		_Glossiness3 ("Smoothness", Range(0,1)) = 0.5
		_Metallic3 ("Metallic", Range(0,1)) = 0.0
		_TexScale3 ("Texture Scale", Float) = 1

		_Tex4 ("Texture 3", 2D) = "white" {}
		_Nor4 ("Normal", 2D) = "bump" {}
		_Color4 ("Color 4", Color) = (1,1,1,1)
		_Glossiness4 ("Smoothness", Range(0,1)) = 0.5
		_Metallic4 ("Metallic", Range(0,1)) = 0.0
		_TexScale4 ("Texture Scale", Float) = 1

		_Tex5 ("Texture 4", 2D) = "white" {}
		_Nor5 ("Normal", 2D) = "bump" {}
		_Color5 ("Color", Color) = (1,1,1,1)
		_Glossiness5 ("Smoothness", Range(0,1)) = 0.5
		_Metallic5 ("Metallic", Range(0,1)) = 0.0
		_TexScale5 ("Texture Scale", Float) = 1

		_Tex6 ("Texture 5 (Mountains)", 2D) = "white" {}
		_Nor6 ("Normal", 2D) = "bump" {}
		_Color6 ("Color", Color) = (1,1,1,1)
		_Glossiness6 ("Smoothness", Range(0,1)) = 0.5
		_Metallic6 ("Metallic", Range(0,1)) = 0.0
		_TexScale6 ("Texture Scale", Float) = 1

		
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows //vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		
		sampler2D _Tex1;
		sampler2D _Tex2;
		sampler2D _Tex3;
		sampler2D _Tex4;
		sampler2D _Tex5;
		sampler2D _Tex6;

		sampler2D _Nor1;
		sampler2D _Nor2;
		sampler2D _Nor3;
		sampler2D _Nor4;
		sampler2D _Nor5;
		sampler2D _Nor6;

		float _TexScale1;
		float _TexScale2;
		float _TexScale3;
		float _TexScale4;
		float _TexScale5;
		float _TexScale6;

		struct Input {

			float2 uv_Tex1;
			float2 uv4_Tex2;
			float4 color: COLOR;
		};

		half _Glossiness1;
		half _Metallic1;
		fixed4 _Color1;

		half _Glossiness2;
		half _Metallic2;
		fixed4 _Color2;

		half _Glossiness3;
		half _Metallic3;
		fixed4 _Color3;

		half _Glossiness4;
		half _Metallic4;
		fixed4 _Color4;

		half _Glossiness5;
		half _Metallic5;
		fixed4 _Color5;

		half _Glossiness6;
		half _Metallic6;
		fixed4 _Color6;
		

		

		
		//UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		//UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
		
			
			
			fixed4 c1 = tex2D (_Tex1, IN.uv_Tex1 * _TexScale1) * _Color1;
			fixed4 c2 = tex2D (_Tex2, IN.uv_Tex1 * _TexScale2) * _Color2;
			fixed4 c3 = tex2D (_Tex3, IN.uv_Tex1 * _TexScale3) * _Color3;
			fixed4 c4 = tex2D (_Tex4, IN.uv_Tex1 * _TexScale4) * _Color4;
			fixed4 c5 = tex2D (_Tex5, IN.uv_Tex1 * _TexScale5) * _Color5;
			fixed4 c6 = tex2D (_Tex6, IN.uv_Tex1 * _TexScale6) * _Color6;
			
			//half c7Intensity = 1 - (IN.color.r + IN.color.g + IN.color.b + IN.color.a + IN.uv4_Tex2.x + IN.uv4_Tex2.y);

			o.Albedo = IN.color.r * c1 + IN.color.g * c2 + IN.color.b * c3 + IN.color.a * c4 + IN.uv4_Tex2.x * c5 + IN.uv4_Tex2.y * c6;

			o.Metallic = IN.color.r * _Metallic1 + IN.color.g * _Metallic2 + IN.color.b * _Metallic3 + IN.color.a * _Metallic4 + IN.uv4_Tex2.x * _Metallic5 + IN.uv4_Tex2.y * _Metallic6;
			o.Smoothness = IN.color.r * _Glossiness1 + IN.color.g * _Glossiness2 + IN.color.b * _Glossiness3 + IN.color.a * _Glossiness4 + IN.uv4_Tex2.x * _Glossiness5 + IN.uv4_Tex2.y * _Glossiness6;

			o.Normal = UnpackNormal(tex2D(_Nor1, IN.uv_Tex1 * _TexScale1)) * IN.color.r + UnpackNormal(tex2D(_Nor2, IN.uv_Tex1 * _TexScale2)) * IN.color.g
			+ UnpackNormal(tex2D(_Nor3, IN.uv_Tex1 * _TexScale3)) * IN.color.b + UnpackNormal(tex2D(_Nor4, IN.uv_Tex1 * _TexScale4)) * IN.color.a
			+ UnpackNormal(tex2D(_Nor5, IN.uv_Tex1 * _TexScale5)) * IN.uv4_Tex2.x + UnpackNormal(tex2D(_Nor6, IN.uv_Tex1 * _TexScale6)) * IN.uv4_Tex2.y;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
