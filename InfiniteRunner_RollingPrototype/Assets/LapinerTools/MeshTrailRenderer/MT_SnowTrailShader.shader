Shader "Custom/SnowTrailShader"
{
	Properties
	{
		_Color ("Main Color", Color) = (.5,.5,.5,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue" = "Geometry-75" }
		Offset -1, -1
		//ZWrite Off
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert

		#include "UnityCG.cginc"

		float4 _Color;
		
		void vert(inout appdata_full v)
		{
			float3 viewDir = WorldSpaceViewDir(v.vertex);
			float viewDirLength = length(viewDir);
			v.vertex.xyz += normalize(viewDir)*min(viewDirLength*0.75, 0.75);
		}

		struct Input
		{
			float noEmptyStruct;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
}
