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
	public int currentZLevel;       // 4 bytes //Very few of these bytes will be needed. An int is much larger than what we need. It is also very easy and convinient. 
	public int id;                  // 4 bytes
	public Vector3 boxMin;          // 4 x 3 = 12 bytes
	public Vector3 boxMax;          // 4 x 3 = 12 bytes
	public uint lastBitMask;		// 4 bytes
}
