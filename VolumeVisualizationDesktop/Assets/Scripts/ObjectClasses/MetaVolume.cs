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
/// Represents the meta data of a volume in the compute shader.
/// Size: 64 bytes
/// </summary>
public struct MetaVolume
{
	public Vector3 position;        // 4 x 3 = 12 bytes
	public Vector3 boxMin;          // 4 x 3 = 12 bytes
	public Vector3 boxMax;          // 4 x 3 = 12 bytes
	public Vector3 scale;           // 4 x 3 = 12 bytes
	public int numBricks;           // 4 bytes
	public int isHz;                // 4 bytes
	public int numBits;             // 4 bytes
	public int maxGlobalSize;		// 4 bytes
}
