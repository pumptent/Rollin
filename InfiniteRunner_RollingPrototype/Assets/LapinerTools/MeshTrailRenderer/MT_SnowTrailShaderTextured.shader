Shader "Custom/SnowTrailShaderTextured"
{
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry-75" }
		Offset -1, -1
		ZWrite Off
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		fixed4 _Color;
		
		void vert(inout appdata_full v)
		{
			float3 viewDir = WorldSpaceViewDir(v.vertex);
			float viewDirLength = length(viewDir);
			v.vertex.xyz += normalize(viewDir)*min(viewDirLength*0.75, 0.75);
		}

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color; 
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}
