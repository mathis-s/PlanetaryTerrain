Shader "PlanetaryTerrain/Water" {
	Properties {
		_MainCol ("Color(RGB), Specular(A)", Color) = (1,1,1,0.5)
		_Power ("Specular Power", Range(0.01,1)) = 0.5
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Normals ("NormalMap", 2D) = "white" {}
		_WaveMap ("WaveMap", 2D) = "white" {}
		_RefractTex("InternalRefraction", 2D) = "grey" {}
		_ReflectTex("Water", 2D) = "grey" {}
		_Parallax ("Refraction Distort", Range (0.005, 0.08)) = 0.02
		_WaveHeight ("Wave Height", Range (0.01, 0.08)) = 0.08
		_FresnelColor("Alpha(for non-refractive water)", Color) = (0,0,0,1)
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
		ZWrite on Cull off
		LOD 0
		
		CGPROGRAM
		#pragma surface surf BlinnPhong
		#pragma target 3.0
		
		fixed4 _MainCol;
		sampler2D _Normals;
		sampler2D _WaveMap;
		sampler2D _RefractTex;
		sampler2D _ReflectTex;
		half _Parallax;
		half _WaveHeight;
		half _Power;
		half4 _FresnelColor;
		
		struct Input {
			float2 uv_Normals;
			float2 uv_WaveMap;
			float2 uv_RefractTex;
			float2 uv_ReflectTex;
			float4 screenPos;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			//relief mapping
			half h = tex2D (_Normals, IN.uv_Normals).w;
			half u = tex2D (_Normals, IN.uv_ReflectTex).w;
			half v = tex2D (_WaveMap, IN.uv_WaveMap).w;
			half2 ripple = ParallaxOffset (h, _Parallax, IN.viewDir);
			half2 ripple2 = ParallaxOffset (u, _Parallax, IN.viewDir);
			half2 wave = ParallaxOffset (v, _WaveHeight, IN.viewDir);
			float4 col = tex2D (_ReflectTex, IN.uv_ReflectTex + (ripple + wave));
			float4 col2 = tex2D (_ReflectTex,IN.uv_Normals + (ripple + wave));
			
			//fresnel, color & normals
			o.Albedo = _MainCol.rgb;
			o.Gloss = _MainCol.a;
			o.Specular = _Power;
			o.Normal = UnpackNormal (tex2D (_WaveMap, IN.uv_WaveMap)  - tex2D(_Normals, IN.uv_Normals)/3);
			
//			refraction & color
			float2 screenUV = (IN.screenPos.xy / IN.screenPos.w);
			screenUV += (ripple + ripple2) + wave;
			screenUV *= float2(1,1);
			o.Albedo *= tex2D(_RefractTex, screenUV) * 10;
			o.Albedo *= col.rgb + col2.rgb;
			if (Luminance(o.Normal.xyz) <= 0.5){
			o.Emission = lerp(o.Albedo.rgb,_FresnelColor,Luminance(o.Normal.xyz));
			}
	    	}	
		ENDCG
	} 
}
