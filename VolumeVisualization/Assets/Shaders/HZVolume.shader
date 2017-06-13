﻿Shader "Custom/HZVolume"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}								// An array of bytes that is the 8-bit raw data. It is essentially a 1D array/texture.
		_Axis("Axes Order", Vector) = (1, 2, 3)								// coordinate i = 0,1,2 in Unity corresponds to coordinate _Axis[i]-1 in the data
		_NormPerStep("Intensity Normalization per Step", Float) = 1
		_NormPerRay("Intensity Normalization per Ray" , Float) = 1
		_Steps("Max Number of Steps", Range(1,1024)) = 128
		_HZRenderLevel("HZ Render Level", Int) = 1
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
			sampler2D _MainTex;
			float3 _Axis;
			float _NormPerStep;
			float _NormPerRay;
			float _Steps;
			int _HZRenderLevel;
			uint lastBitMask = 0x40000000;			// a 1 bit in the second most significant bit

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

				o.pos = mul(UNITY_MATRIX_MVP, i.pos);
				o.ray_d = -ObjSpaceViewDir(i.pos);
				o.ray_o = i.pos.xyz - o.ray_d;

				return o;
			}

			//int3 quantizePoint(float3 cartesianPoint)
			//{
			//	return int3(floor(cartesianPoint.x), floor(cartesianPoint.y), floor(cartesianPoint.z));
			//}

			// Expands an 8-bit integer into 24 bits by inserting 2 zeros after each bit
			uint expandBits(uint v)
			{
				v = (v * 0x00010001u) & 0xFF0000FFu;
				v = (v * 0x00000101u) & 0x0F00F00Fu;
				v = (v * 0x00000011u) & 0xC30C30C3u;
				v = (v * 0x00000005u) & 0x49249249u;
				return v;
			}

			// Calculates a 24-bit Morton code for the given 3D point located within the unit cube [0, 1]
			// Taken from: https://devblogs.nvidia.com/parallelforall/thinking-parallel-part-iii-tree-construction-gpu/
			uint morton3D(float3 pos)
			{
				// Quantize the position
				pos.x = min(max(pos.x * 1024.0f, 0.0f), 1023.0f);
				pos.y = min(max(pos.y * 1024.0f, 0.0f), 1023.0f);
				pos.z = min(max(pos.z * 1024.0f, 0.0f), 1023.0f);

				// Interlace the bits
				uint xx = expandBits((uint) pos.x);
				uint yy = expandBits((uint) pos.y);
				uint zz = expandBits((uint) pos.z);
				return xx * 4 + yy * 2 + zz;
			}

			// Return the index into the hz-ordered array of data given a quantized point within the volume
			uint getHZIndex(uint zIndex)
			{
				int hzIndex = zIndex | lastBitMask;		// set leftmost one
				hzIndex /= hzIndex & -hzIndex;			// remove trailing zeros
				return (hzIndex >> 1);					// remove rightmost one
			}

			// Returns the texture coordinate of the hzIndex into a texture of the given size
			// Assumption: Texture2D has (0,0) in bottom left, (1,1) in top right.
			float2 textureCoordFromHzIndex(uint hzIndex, uint texWidth, uint texHeight)
			{
				uint xCoord = hzIndex % texWidth;
				uint yCoord = hzIndex / texWidth;
				float2 texCoord = float2(
					hzIndex % texWidth,		// x coord
					hzIndex / texWidth		// y coord
					);

				// Convert to texture coordinates in [0, 1]
				texCoord = (texCoord - 0.5) + 0.5;

				return texCoord;
			}

			// Gets the intensity data value at a given position in the 3D texture
			// note: pos is normalized in [0, 1]
			float4 sampleIntensity(float3 pos) {
				// Get the position in texture coordinates
				//float3 posTex = float3(pos[_Axis[0] - 1],pos[_Axis[1] - 1],pos[_Axis[2] - 1]);						// IS THIS STILL NEEDED?
				//posTex = (posTex - 0.5) + 0.5;

				// Get the Z order index
				uint zIndex = morton3D(pos);

				// find the hz order index
				int hzIndex = getHZIndex(zIndex);

				// Convert the index to the right texture coordinate for sampling in the 4096 x 4096 texture
				float2 texCoord = textureCoordFromHzIndex(hzIndex, 4096, 4096);

				// sample the color from the main texture that holds the data 
				float data = tex2Dlod(_MainTex, float4(texCoord.xy, 0, 0)).a;
				//float data = tex3Dlod(_VolumeDataTexture, float4(posTex,0)).a;

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
					// Determine the value at this point on the current ray
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
