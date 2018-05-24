// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/* Volume Visualization Shader | Marko Sterbentz
 *﻿ This shader is for barebones volume visualization. It renders only small, RAW data files.
 * NOTE: This shader is no longer recommended for use.
 */
Shader "Custom/VolumeVisualization"
{
	Properties
	{
		_VolumeDataTexture("Data Texture", 3D) = "" {}						// The volume data
		_NormPerStep("Intensity Normalization per Step", Float) = 1
		_NormPerRay("Intensity Normalization per Ray" , Float) = 1
		_Steps("Max Number of Steps", Range(1,1024)) = 128
	}

	SubShader
	{
		Tags
		{ 
			"Queue" = "Transparent"	   /* Allow transparent surfaces to render. */
		}

		Blend SrcAlpha OneMinusSrcAlpha	// Needed for rendering transparent surfaces.
		Cull Off
		ZTest LEqual
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			/********************* DATA *********************/
			sampler3D _VolumeDataTexture;
			float _NormPerStep;
			float _NormPerRay;
			float _Steps;

			/******************** STRUCTS ********************/

			struct appdata {
				float4 pos : POSITION;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 ray_o : TEXCOORD1; // ray origin
				float3 ray_d : TEXCOORD2; // ray direction
			};

			/******************* FUNCTIONS *******************/

			// Calculates intersection between a ray and a box
			bool IntersectBox(float3 ray_o, float3 ray_d, float3 boxMin, float3 boxMax, out float tNear, out float tFar)
			{
				// Compute intersection of ray with all six bbox planes
				float3 invR = 1.0 / ray_d;
				float3 tBot = invR * (boxMin.xyz - ray_o);
				float3 tTop = invR * (boxMax.xyz - ray_o);
				
				// Re-order intersections to find smallest and largest on each axis
				float3 tMin = min(tTop, tBot);
				float3 tMax = max(tTop, tBot);
				
				// Find the largest tMin and the smallest tMax
				float2 t0 = max(tMin.xx, tMin.yz);
				float largest_tMin = max(t0.x, t0.y);
				t0 = min(tMax.xx, tMax.yz);
				float smallest_tMax = min(t0.x, t0.y);
				
				// Check for hit
				bool hit = (largest_tMin <= smallest_tMax);
				tNear = largest_tMin;
				tFar = smallest_tMax;
				return hit;
			}



			// vertex program
			v2f vert(appdata i)
			{
				v2f o;

				o.pos = UnityObjectToClipPos(i.pos);
				o.ray_d = -ObjSpaceViewDir(i.pos);
				o.ray_o = i.pos.xyz - o.ray_d;

				return o;
			}

			// Gets the intensity data value at a given position in the 3D texture
			// note: pos is normalized in [0, 1]
			float4 sampleIntensity(float3 pos) {
				float data = tex3Dlod(_VolumeDataTexture, float4(pos,0)).a;
				return float4(data, data, data, data);
			}

			// fragment program
			float4 frag(v2f i) : COLOR
			{
				i.ray_d = normalize(i.ray_d);

				// calculate eye ray intersection with cube bounding box
				float3 boxMin = float3(-0.5, -0.5, -0.5);
				float3 boxMax = float3(0.5, 0.5, 0.5);
				float tNear, tFar;
				bool hit = IntersectBox(i.ray_o, i.ray_d, boxMin, boxMax, tNear, tFar);

				if (!hit) 
					discard;
				if (tNear < 0.0) 
					tNear = 0.0;

				// Calculate intersection points with the cube
				float3 pNear = i.ray_o + (i.ray_d*tNear);
				float3 pFar = i.ray_o + (i.ray_d*tFar);

				// Convert to texture space
				pNear = pNear + 0.5;
				pFar = pFar + 0.5;
				//return float4(pNear, 1);
				//return float4(pFar , 1);
				
				// Set up ray marching parameters
				float3 ray_pos = pNear;
				float3 ray_dir = pFar - pNear;

				float3 ray_step = normalize(ray_dir) * sqrt(3) / _Steps;
				//return float4(abs(ray_dir), 1);
				//return float4(length(ray_dir), length(ray_dir), length(ray_dir), 1);

				// Perform the ray march
				float4 fColor = 0;
				float4 ray_col = 0;
				for (int k = 0; k < _Steps; k++)
				{
					float4 intensity = sampleIntensity(ray_pos);
					intensity.a = _NormPerStep * length(ray_step);

					// Front to back blending function
					fColor.rgb = fColor.rgb + (1 - fColor.a) * intensity.a * intensity.rgb;
					fColor.a = fColor.a + (1 - fColor.a) * intensity.a;

					// March along the ray
					ray_pos += ray_step;
					
					// Check if we have marched out of the cube
					if (ray_pos.x < 0 || ray_pos.y < 0 || ray_pos.z < 0) break;
					if (ray_pos.x > 1 || ray_pos.y > 1 || ray_pos.z > 1) break;
				}
				return fColor * _NormPerRay;
			}
			ENDCG
		}
	}
	FallBack Off
}
