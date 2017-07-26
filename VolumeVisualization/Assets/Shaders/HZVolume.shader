// Written by stermj﻿

Shader "Custom/HZVolume"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}											// An array of bytes that is the 8-bit raw data. It is essentially a 1D array/texture.
		_VolumeDataTexture("3D Data Texture", 3D) = "" {}
		_Axis("Axes Order", Vector) = (1, 2, 3)											// coordinate i = 0,1,2 in Unity corresponds to coordinate _Axis[i]-1 in the data
		_NormPerRay("Intensity Normalization per Ray" , Float) = 1
		_Steps("Max Number of Steps", Range(1,1024)) = 512
		_TransferFunctionTex("Transfer Function", 2D) = "white" {}
		_ClippingPlaneNormal("Clipping Plane Normal", Vector) = (1, 0, 0)
		_ClippingPlanePosition("Clipping Plane Position", Vector) = (0.5, 0.5, 0.5)
		_ClippingPlaneEnabled("Clipping Plane Enabled", Int) = 0						// A "boolean" for whether the clipping plane is active or not. 0 == false, 1 == true
		_BrickSize("Brick Size", Int) = 0
		_CurrentZLevel("Current Z Render Level", Int) = 0
		_MaxZLevel("Max Z Render Level", Int) = 0
		_LastBitMask("Last Bit Mask", Int) = 0
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
			sampler3D _VolumeDataTexture;
			sampler2D _TransferFunctionTex;
			float3 _Axis;
			float _NormPerRay;
			float _Steps;
			float3 _ClippingPlaneNormal;
			float3 _ClippingPlanePosition;
			int _ClippingPlaneEnabled;
			int _BrickSize;
			int _CurrentZLevel;
			int _MaxZLevel;
			int _LastBitMask;

			//static uint LAST_BIT_MASK = (1 << 24);

			/******************** STRUCTS ********************/

			struct appdata {
				float4 pos : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float3 ray_o : TEXCOORD1;		// ray origin
				float3 ray_d : TEXCOORD2;		// ray direction
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

			/***************************************** HZ CURVING CODE ************************************************/
			uint Compact1By2(uint x)
			{
				x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
				x = (x ^ (x >> 2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
				x = (x ^ (x >> 4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
				x = (x ^ (x >> 8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
				x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
				return x;
			}

			uint DecodeMorton3X(uint code)
			{
				return Compact1By2(code >> 2);
			}

			uint DecodeMorton3Y(uint code)
			{
				return Compact1By2(code >> 1);
			}

			uint DecodeMorton3Z(uint code)
			{
				return Compact1By2(code >> 0);
			}

			uint3 decode(uint c)
			{
				uint3 cartEquiv = uint3(0,0,0);
				c = c << 1 | 1;
				uint i = c | c >> 1;
				i |= i >> 2;
				i |= i >> 4;
				i |= i >> 8;
				i |= i >> 16;

				i -= i >> 1;

				c *= _LastBitMask / i;
				c &= (~_LastBitMask);
				cartEquiv.x = DecodeMorton3X(c);
				cartEquiv.y = DecodeMorton3Y(c);
				cartEquiv.z = DecodeMorton3Z(c);

				return cartEquiv;
			}

			// Expands an 8-bit integer into 24 bits by inserting 2 zeros after each bit
			// Taken from: https://webcache.googleusercontent.com/search?q=cache:699-OSphYRkJ:https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/+&cd=1&hl=en&ct=clnk&gl=us
			uint Part1By2(uint x)
			{
				x &= 0x000003ff;                  // x = ---- ---- ---- ---- ---- --98 7654 3210
				x = (x ^ (x << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
				x = (x ^ (x << 8)) & 0x0300f00f;  // x = ---- --98 ---- ---- 7654 ---- ---- 3210
				x = (x ^ (x << 4)) & 0x030c30c3;  // x = ---- --98 ---- 76-- --54 ---- 32-- --10
				x = (x ^ (x << 2)) & 0x09249249;  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
				return x;
			}

			// Calculates a 24-bit Morton code for the given 3D point located within the unit cube [0, 1]
			// Taken from: https://devblogs.nvidia.com/parallelforall/thinking-parallel-part-iii-tree-construction-gpu/
			uint morton3D(float3 pos)
			{
				// Quantize to the correct resolution
				pos.x = min(max(pos.x * (float) _BrickSize, 0.0f), (float) _BrickSize - 1);
				pos.y = min(max(pos.y * (float)_BrickSize, 0.0f), (float)_BrickSize - 1);
				pos.z = min(max(pos.z * (float)_BrickSize, 0.0f), (float)_BrickSize - 1);

				// Interlace the bits
				uint xx = Part1By2((uint) pos.x);
				uint yy = Part1By2((uint) pos.y);
				uint zz = Part1By2((uint) pos.z);

				return zz << 2 | yy << 1 | xx;
			}

			// Return the index into the hz-ordered array of data given a quantized point within the volume
			uint getHZIndex(uint zIndex)
			{
				uint hzIndex = (zIndex | _LastBitMask);		// set leftmost one
				hzIndex /= hzIndex & -hzIndex;				// remove trailing zeros
				return (hzIndex >> 1);						// remove rightmost one
			}

			float3 texCoord3DFromHzIndex(uint hzIndex, uint texWidth, uint texHeight, uint texDepth)
			{
				float3 texCoord = float3(0,0,0);
				texCoord.z = hzIndex / (texWidth * texHeight);
				hzIndex = hzIndex - (texCoord.z * texWidth * texHeight);
				texCoord.y = hzIndex / texWidth;
				texCoord.x = hzIndex % texHeight;

				// Convert to texture coordinates in [0, 1]
				texCoord.z = texCoord.z / (float)texDepth;
				texCoord.y = texCoord.y / (float)texHeight;
				texCoord.x = texCoord.x / (float)texWidth;

				return texCoord;
			}

			// Returns the masked z index, allowing for the the data to be quantized to a level of detail specified by the _CurrentZLevel.
			uint computeMaskedZIndex(uint zIndex)
			{
				int zBits = _MaxZLevel * 3;
				uint zMask = -1 >> (zBits - 3 * _CurrentZLevel) << (zBits - 3 * _CurrentZLevel);
				return zIndex & zMask;
			}

			/***************************************** END HZ CURVING CODE ************************************************/
			float sampleIntensityHz3D(float3 pos)
			{
				/********* SAMPLING 3D HZ CURVED RAW DATA WITH TEXTURE COORD CALCULATION **********/
				uint zIndex = morton3D(pos);										// Get the Z order index		
				uint maskedZIndex = computeMaskedZIndex(zIndex);					// Get the masked Z index
				uint hzIndex = getHZIndex(maskedZIndex);							// Find the hz order index
				uint dataCubeDimension = 1 << _CurrentZLevel;						// The dimension of the data brick using the current hz level.
				float3 texCoord = texCoord3DFromHzIndex(hzIndex, dataCubeDimension, dataCubeDimension, dataCubeDimension);			// THE DIMENSIONS NEED TO BE THE DATA BRICK SIZE, not the full level _BrickSize
				float data = tex3Dlod(_VolumeDataTexture, float4(texCoord, 0)).a;
				return data;
			}

			float sampleIntensityRaw3D(float3 pos)
			{
				/********* SAMPLING 3D RAW WITH POSITION GIVEN ***********/
				// Get the position in texture coordinates
				float3 posTex = float3(pos[_Axis[0] - 1], pos[_Axis[1] - 1], pos[_Axis[2] - 1]);
				float data = tex3Dlod(_VolumeDataTexture, float4(posTex, 0)).a;
				return data;
			}
			
			// Gets the intensity data value at a given position in the volume.
			// Note: This is a wrapper for the other sampling methods.
			// Note: pos is normalized in [0, 1]
			float4 sampleIntensity(float3 pos) {
				float data = sampleIntensityHz3D(pos);
				//float data = sampleIntensityRaw3D(pos);
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
				//return float4(pNear, 1);		// Test for near intersections
				//return float4(pFar , 1);		// Test for far intersections
				
				// Set up ray marching parameters
				float3 ray_start = pNear;									// The start position of the ray
				float3 ray_stop = pFar;										// The end position of the ray
				float3 ray_pos = ray_start;									// The current position of the ray during the march

				float3 ray_dir = ray_stop - ray_start;						// The direction of the ray (un-normalized)
				float ray_length = length(ray_dir);							// The length of the ray to travel
				ray_dir = normalize(ray_stop - ray_start);					// The direction of the ray (normalized)

				//float3 ray_step = ray_dir * sqrt(3) / _Steps;				// The step size of the ray-march (OLD)
				float step_size = ray_length / (float)_Steps;
				//float step_size = 0.001;
				float3 ray_step = ray_dir * step_size;
				//return float4(abs(ray_stop - ray_start), 1);
				//return float4(ray_length, ray_length, ray_length, 1);

				// Use the clipping plane to clip the volume, if it is enabled
				if (_ClippingPlaneEnabled == 1)
				{
					// Inputs from the application
					float3 plane_norm = normalize(_ClippingPlaneNormal);		// The global normal of the clipping plane
					float3 plane_pos = _ClippingPlanePosition + 0.5; 			// The plane position in model space / texture space

					// Calculate values needed for ray-plane intersection
					float denominator = dot(plane_norm, ray_dir);
					float t = 0.0;
					if (denominator != 0.0)
					{
						t = dot(plane_norm, plane_pos - ray_start) / denominator;		// t == positive, plane is in front of eye | t == negative, plane is behind eye
					}
					
					bool planeFacesForward = denominator > 0.0;

					if ((!planeFacesForward) && (t < 0.0))
						discard;
					if ((planeFacesForward) && (t > ray_length))
						discard;
					if ((t > 0.0) && (t < ray_length))
					{
						if (planeFacesForward)
						{
							ray_start = ray_start + ray_dir * t;
						}
						else
						{
							ray_stop = ray_start + ray_dir * t;
						}
						ray_dir = ray_stop - ray_start;
						ray_pos = ray_start;
						ray_length = length(ray_dir);
						ray_dir = normalize(ray_dir);
					}
				}

				// Perform the ray march
				float4 fColor = 0;
				for (int k = 0; k < _Steps; k++)
				{
					// Determine the value at this point on the current ray
					float4 intensity = sampleIntensity(ray_pos);

					// Sample from the texture generated by the transfer function
					float4 sampleColor = tex2Dlod(_TransferFunctionTex, float4(intensity.a, 0.0, 0.0, 0.0)); 

					// Front to back blending function
					fColor.rgb = fColor.rgb + (1 - fColor.a) * sampleColor.a * sampleColor.rgb;
					fColor.a = fColor.a + (1 - fColor.a) * sampleColor.a;

					// March along the ray
					ray_pos += ray_step;
					
					// Check if we have marched out of the cube
					if (ray_pos.x < 0 || ray_pos.y < 0 || ray_pos.z < 0) break;
					if (ray_pos.x > 1 || ray_pos.y > 1 || ray_pos.z > 1) break;

					// Check if accumulated alpha is greater than 1.0
					//if (fColor.a > 1.0) break;
				}

				return fColor * _NormPerRay;
			}
			ENDCG
		}
	}
	FallBack Off
}
