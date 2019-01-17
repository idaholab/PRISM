/* 
 * Copyright 2019 Idaho National Laboratory.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

/// <summary>
/// Represents the meta data of a brick in the compute shader.
/// Size: 64 bytes
/// </summary>
public struct MetaBrick
{
	public Vector3 position;        // 4 x 3 = 12 bytes
	public int size;                // 4 bytes
	public int bufferOffset;         // 4 bytes
	public int bufferIndex;			// 4 bytes
	public int maxZLevel;           // 4 bytes
	public int currentZLevel;       // 4 bytes //Very few of these bytes will be needed. An int is much larger than what we need. It is also very easy and convinient. It also is recognized by the shaders. (byte, short are not). 
	public int id;                  // 4 bytes
	public Vector3 boxMin;          // 4 x 3 = 12 bytes
	public Vector3 boxMax;          // 4 x 3 = 12 bytes
	public uint lastBitMask;		// 4 bytes
}
