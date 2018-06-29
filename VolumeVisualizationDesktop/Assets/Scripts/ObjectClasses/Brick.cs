/* Brick | Marko Sterbentz 7/3/2017 */

using System;
using System.IO;
using UnityEngine;

/// <summary>
/// This class holds the data associated with a single brick of the volume to be rendered.
/// </summary>
public class Brick
{
	/* Member variables */
	private GameObject gameObject;                                  // The cube GameObject that is the rendered representation of this brick.
	private int size;                                               // The dimensions of the data in voxel space. Size is assumed to be a power of 2.
	private int maxZLevel;                                          // The max number of Z-Order levels this brick has.
	private int currentZLevel;                                      // The current Z-Order rendering level for this brick.
	private uint lastBitMask;                                       // The last bit mask used for accessing the data.
	private string filename;                                        // The name of the data file associated with this brick.
    private Volume parentVolume;                                    // The volume that this brick is associated with.
	private Vector3 boxMin;											// The position of the brick's front, bottom, left corner
	private Vector3 boxMax;											// The position of the brick's back, top, right corner

	/* Properties */
	public GameObject GameObject
	{
		get
		{
			return gameObject;
		}
		set
		{
			gameObject = value;
		}
	}

	public int Size
	{
		get
		{
			return size;
		}
		set
		{
			size = value;
		}
	}

	public int MaxZLevel
	{
		get
		{
			return maxZLevel;
		}
		set
		{
			maxZLevel = value;
		}
	}

	// Note: This clamps the current Z-order level to be between 0 and the max Z-order level possible for this brick.
	public int CurrentZLevel
	{
		get
		{
			return currentZLevel;
		}
		set
		{
			currentZLevel = Math.Min(Math.Max(value, 0), maxZLevel);
		}
	}

	public uint LastBitMask
	{
		get
		{
			return lastBitMask;
		}
		set
		{
			lastBitMask = value;
		}
	}

	public string Filename
	{
		get
		{
			return filename;
		}
		set
		{
			filename = value;
		}
	}

    public Volume ParentVolume
    {
        get
        {
            return parentVolume;
        }
        set
        {
            parentVolume = value;
        }
    }

	public Vector3 BoxMin
	{
		get
		{
			return boxMin;
		}
		set
		{
			boxMin = value;
		}
	}

	public Vector3 BoxMax
	{
		get
		{
			return boxMax;
		}
		set
		{
			boxMax = value;
		}
	}

	/* Constructors*/
	/// <summary>
	/// Creates a new instance of a data brick.
	/// </summary>
	public Brick()
	{

	}

	/// <summary>
	/// Creates a new instance of a data brick.
	/// </summary>
	/// <param name="_filename"></param>
	/// <param name="_size"></param>
	/// <param name="_position"></param>
	/// <param name="mat"></param>
	public Brick(string _filename, int _size, Vector3 _position, Material mat, Vector3 _boxMin, Vector3 _boxMax, Volume _parentVolume)
	{
		// Set the name of the data file associated with this brick
		filename = _filename;

        // Set the volume that this brick is associated with
        parentVolume = _parentVolume;

		// Initialize a Unity cube to represent the brick
		gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = Path.GetFileName(filename);

        // Set the position of the brick
        gameObject.transform.position = _position;
        gameObject.transform.parent = parentVolume.VolumeCube.transform;

		// Assign the given material to render the cube with
		setMaterial(mat);

		// Set the cube's data size
		updateBrickSize(_size);

		// Set the maximum z level
		updateMaxZLevel(calculateMaxLevels());

		// Set the last bit mask this brick uses
		updateLastBitMask(calculateLastBitMask());

		// Set the brick's min and max corner positions
		BoxMin = _boxMin;
		BoxMax = _boxMax;

		// Set the default level of detail that this brick renders to
		updateCurrentZLevel(0);

		gameObject.GetComponent<MeshRenderer>().enabled = false;
	}

	/*****************************************************************************
	 * UTILITY FUNCTIONS
	 *****************************************************************************/
	/// <summary>
	/// Returns the computed maximum number of z levels this brick can render to.
	/// </summary>
	/// <returns></returns>
	public int calculateMaxLevels()
	{
		int totalLevels = 0;                                                                            // NOTE: Starting at 0, instead of -1
		int tempBrickSize = size;

		while (Convert.ToBoolean(tempBrickSize >>= 1))
		{
			totalLevels++;
		}

		return totalLevels;
	}

	/// <summary>
	/// Returns the computed last bit mask for this brick.
	/// </summary>
	/// <returns></returns>
	public uint calculateLastBitMask()
	{
		int zBits = maxZLevel * 3;
		uint lbm = (uint)1 << zBits;
		return lbm;
	}

	/// <summary>
	/// Generates a struct of the bricks data that can be analyzed in the compute shader.
	/// </summary>
	/// <returns></returns>
	public BrickData getBrickData()
	{
		BrickData newData;

		newData.size = size;
		newData.position = gameObject.transform.position;
		newData.maxZLevel = maxZLevel;
		newData.currentZLevel = currentZLevel;
		newData.updateData = false;

		return newData;
	}

	/*****************************************************************************
	 * DATA READING FUNCTIONS
	 *****************************************************************************/
	/// <summary>
	/// The file must already have been opened.
	/// </summary>
	/// <returns></returns>
	public Texture3D readRaw8Into3D()
	{
		try
		{
			// Open the file associated with this brick
			FileStream file = new FileStream(filename, FileMode.Open);

			// Read in the bytes
			BinaryReader reader = new BinaryReader(file);
			byte[] buffer = new byte[size * size * size];
			reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
			reader.Close();

			// Scale the scalar values to [0, 1]
			Color[] scalars;
			scalars = new Color[buffer.Length];
			for (int i = 0; i < buffer.Length; i++)
			{
				scalars[i] = new Color(0, 0, 0, ((float)buffer[i] / byte.MaxValue));
			}

			// Put the intensity scalar values into the Texture3D
			Texture3D data = new Texture3D(size, size, size, TextureFormat.Alpha8, false);
			data.filterMode = FilterMode.Point;
			data.SetPixels(scalars);
			data.Apply();

			// Send the intensity scalar values to the shader
			gameObject.GetComponent<Renderer>().material.SetTexture("_VolumeDataTexture", data);

			return data;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}

	}

	/// <summary>
	/// The file must already have been opened.
	/// </summary>
	/// <returns></returns>
	public Texture3D readRaw8Into3DZLevel()
	{
		try
		{
			// Get the size of the data based on the currentZLevel rendering level
			int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel

            // Read in the bytes
            BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

			byte[] buffer = new byte[dataSize * dataSize * dataSize];
			reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
			reader.Close();

            // Scale the scalar values to [0, 1]
            Color[] scalars;
			scalars = new Color[buffer.Length];
			for (int i = 0; i < buffer.Length; i++)
			{
				scalars[i] = new Color(0, 0, 0, ((float)buffer[i] / byte.MaxValue));
			}

            // Put the intensity scalar values into the Texture3D
            Texture3D data = new Texture3D(dataSize, dataSize, dataSize, TextureFormat.Alpha8, false);
			data.filterMode = FilterMode.Point;
			data.SetPixels(scalars);
			data.Apply();

			// Send the intensity scalar values to the shader
			gameObject.GetComponent<Renderer>().material.SetTexture("_VolumeDataTexture", data);

			return data;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}

	}

    /// <summary>
    /// The file must already have been opened.
    /// </summary>
    /// <returns></returns>
    public Texture3D readRaw16Into3DZLevel()
    {
        try
        {
            // Get the size of the data based on the currentZLevel rendering level
            int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel

            // Read in the bytes
            BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

            byte[] buffer = new byte[dataSize * dataSize * dataSize * 2];
            reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
            reader.Close();

            // Convert the big endian input data to be little endian for the Buffer.BlockCopy()
			if (parentVolume.Endianness == "BIG_ENDIAN")
			{
				for (int i = 0; i < buffer.Length; i += 2)
				{
					byte temp = buffer[i];
					buffer[i] = buffer[i + 1];
					buffer[i + 1] = temp;
				}
			}

            // Convert the bytes to ushort (Note: Buffer.BlockCopy() assumes data is in little endian format.
            // TODO: Figure out a better way to read in ushort rather than doing this... This may not be available on non-Windows platforms?
            ushort[] ushortBuffer = new ushort[buffer.Length / 2];
            Buffer.BlockCopy(buffer, 0, ushortBuffer, 0, buffer.Length);

            // Scale the scalar values to [0, 1]
            Color[] scalars;
            scalars = new Color[ushortBuffer.Length];
            for (int i = 0; i < ushortBuffer.Length; i++)
            {
                scalars[i] = new Color(((float)ushortBuffer[i] / ushort.MaxValue), 0, 0, 0);
            }

            // Put the intensity scalar values into the Texture3D
            Texture3D data = new Texture3D(dataSize, dataSize, dataSize, TextureFormat.RHalf, false);
            data.filterMode = FilterMode.Point;
            data.SetPixels(scalars);
            data.Apply();

            // Send the intensity scalar values to the shader
            gameObject.GetComponent<Renderer>().material.SetTexture("_VolumeDataTexture", data);

            return data;
        }
        catch (Exception e)
        {
            Debug.Log("Unable to read in the data file into brick: " + e);
            return null;
        }
    }

	/// <summary>
	/// Reads a certain amount of the brick's associated data into a byte array. The amount is dependent on the current z level of the brick.
	/// </summary>
	/// <returns></returns>
	public byte[] readRaw8Into3DZLevelBuffer()
	{
		try
		{
			// Get the size of the data based on the currentZLevel rendering level
			int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel

			// Read in the bytes
			BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

			byte[] buffer = new byte[dataSize * dataSize * dataSize];
			reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
			reader.Close();

			return buffer;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}
	}

	/// <summary>
	/// Reads a certain amount of the brick's associated data into a byte array. The amount is dependent on the current z level of the brick.
	/// </summary>
	/// <returns></returns>
	public uint[] readRaw8Into3DZLevelBufferUint()
	{
		try
		{
			// Get the size of the data based on the currentZLevel rendering level
			int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel

			// Read in the bytes
			BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

			int totalDataSize = dataSize * dataSize * dataSize;
			byte[] buffer = new byte[totalDataSize];
			reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
			reader.Close();

			//// Don't pack bytes
			//uint[] uintBuffer = new uint[buffer.Length];
			//for (int i = 0; i < uintBuffer.Length; i++)
			//{
			//	uintBuffer[i] = buffer[i];
			//}

			// Pack the bytes that were read into a uint array
			uint[] uintBuffer = new uint[Mathf.CeilToInt(totalDataSize / 4.0f)];
			//int[] byteShifts = { 24, 16, 8, 0};	// use for big endian
			int[] byteShifts = { 0, 8, 16, 24 };    // use for little endian
			int maskIndex = 0;
			for (int i = 0; i < buffer.Length; i++)
			{
				uint val = buffer[i];
				uintBuffer[i / 4] = uintBuffer[i / 4] | (val << byteShifts[maskIndex]);
				maskIndex = (maskIndex + 1) % 4;
			}

			return uintBuffer;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}
	}

	/// <summary>
	/// Reads a certain amount of the brick's associated data into a byte array. The amount is dependent on the current z level of the brick.
	/// </summary>
	/// <returns></returns>
	public ushort[] readRaw16Into3DZLevelBuffer()
	{
		try
		{
			// Get the size of the data based on the currentZLevel rendering level
			int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel

			// Read in the bytes
			BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

			byte[] buffer = new byte[dataSize * dataSize * dataSize * 2];
			reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
			reader.Close();

			// Convert the big endian input data to be little endian for the Buffer.BlockCopy()
			if (parentVolume.Endianness == "BIG_ENDIAN")
			{
				for (int i = 0; i < buffer.Length; i += 2)
				{
					byte temp = buffer[i];
					buffer[i] = buffer[i + 1];
					buffer[i + 1] = temp;
				}
			}

			// Convert the bytes to ushort (Note: Buffer.BlockCopy() assumes data is in little endian format.
			// TODO: Figure out a better way to read in ushort rather than doing this... This may not be available on non-Windows platforms?
			ushort[] ushortBuffer = new ushort[buffer.Length / 2];
			Buffer.BlockCopy(buffer, 0, ushortBuffer, 0, buffer.Length);

			return ushortBuffer;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}
	}

	/*****************************************************************************
	 * ACCESSORS
	 *****************************************************************************/
	/// <summary>
	/// Returns the material of the Unity game object.
	/// </summary>
	/// <returns></returns>
	public Material getMaterial()
	{
		return gameObject.GetComponent<Renderer>().material;
	}

	/*****************************************************************************
	 * MUTATOR AND SHADER BUFFERING METHODS
	 *****************************************************************************/
	/// <summary>
	/// Sets this brick's material to the given material.
	/// </summary>
	/// <param name="mat"></param>
	public void setMaterial(Material mat)
	{
		gameObject.GetComponent<Renderer>().material = mat;
	}

	/// <summary>
	/// Sets the size of the brick on both the CPU and in the shader.
	/// </summary>
	/// <param name="_size"></param>
	public void updateBrickSize(int _size)
	{
		Size = _size;
		gameObject.GetComponent<Renderer>().material.SetInt("_BrickSize", size);
	}

	/// <summary>
	/// Sets the current Z-Order rendering level both on the CPU and in the shader.
	/// </summary>
	/// <param name="_currentZlevel"></param>
	public void updateCurrentZLevel(int _currentZlevel)
	{
		// Clamp the input level to be between 0 and maxZLevel, inclusive
		CurrentZLevel = _currentZlevel;
		gameObject.GetComponent<Renderer>().material.SetInt("_CurrentZLevel", currentZLevel);

        // Read in the appropriate amount of data dependent on the current z level to render to
        //if (parentVolume.BytePerPixel == 1)
        //    readRaw8Into3DZLevel();
        //else if (parentVolume.BytePerPixel == 2)
        //    readRaw16Into3DZLevel();
	}

	/// <summary>
	/// Sets the maximum Z-Order rendering level both on the CPU and in the shader.
	/// </summary>
	/// <param name="_maxZLevel"></param>
	public void updateMaxZLevel(int _maxZLevel)
	{
		MaxZLevel = _maxZLevel;
		gameObject.GetComponent<Renderer>().material.SetInt("_MaxZLevel", maxZLevel);
	}

	/// <summary>
	/// Sets the last bit mask both on the CPU and in the shader.
	/// </summary>
	/// <param name="_lastBitMask"></param>
	public void updateLastBitMask(uint _lastBitMask)
	{
		LastBitMask = _lastBitMask;
		gameObject.GetComponent<Renderer>().material.SetInt("_LastBitMask", (int)lastBitMask);
	}

	/// <summary>
	/// Returns a struct that contains information about this brick to be passed to the compute shader.
	/// </summary>
	/// <returns></returns>
	public MetaBrick getMetaBrick()
	{
		MetaBrick mb;
		mb.position = gameObject.transform.position;
		mb.size = size;
		mb.bufferIndex = -1;
		mb.maxZLevel = maxZLevel;
		mb.currentZLevel = 0;
		mb.id = -1;
		mb.boxMin = BoxMin;
		mb.boxMax = BoxMax;
		mb.lastBitMask = 0;
		return mb;
	}
}

/* BrickData | Marko Sterbentz 7/18/2017 */

/// <summary>
/// This struct mirrors the struct in use by the BrickAnalysis compute shader.
/// This struct is 28 bytes in size.
/// </summary>
public struct BrickData
{
	// DEPRECATED: DO NOT USE
	public int size;                    // 4 bytes							
	public Vector3 position;            // 4 x 3 = 12 bytes
	public int maxZLevel;               // 4 bytes
	public int currentZLevel;           // 4 bytes
	public bool updateData;             // 4 bytes
}