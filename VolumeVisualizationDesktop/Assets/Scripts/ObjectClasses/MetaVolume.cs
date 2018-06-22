using UnityEngine;

/// <summary>
/// Represents the meta data of a volume in the compute shader.
/// Size: 60 bytes
/// </summary>
public struct MetaVolume
{
	public Vector3 position;        // 4 x 3 = 12 bytes
	public Vector3 boxMin;          // 4 x 3 = 12 bytes
	public Vector3 boxMax;          // 4 x 3 = 12 bytes
	public Vector3 scale;           // 4 x 3 = 12 bytes
	public int numBricks;           // 4 bytes
	public int isHz;                // 4 bytes
	public int numBits;				// 4 bytes
}
