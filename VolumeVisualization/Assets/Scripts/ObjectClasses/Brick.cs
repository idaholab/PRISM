using System;
using System.IO;
using UnityEngine;

/* Brick | Marko Sterbentz 7/3/2017
 * This class holds the data associated with a single brick of the volume to be rendered.
 */
public class Brick
{
	/* Member variables */
	private GameObject cube;                                        // The cube GameObject that is the rendered representation of this brick.
	private int size;                                               // The dimensions of the data in voxel space. Size is assumed to be a power of 2.
	private int maxZLevel;                                          // The max number of Z-Order levels this brick has.
	private int currentZLevel;                                      // The current Z-Order rendering level for this brick.
	private uint lastBitMask;                                       // The last bit mask used for accessing the data.
	private string filename;                                        // The name of the data file associated with this brick.

	/* Properties */
	public GameObject Cube
	{
		get
		{
			return cube;
		}
		set
		{
			cube = value;
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

	/* Constructors*/
	public Brick()
	{

	}

	public Brick(string _filename, int _size, Vector3 _position, Material mat)
	{
		//Set the name of the data file associated with this brick
		filename = _filename;

		// Initialize a Unity cube to represent the brick
		cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

		// Set the position of the 
		cube.transform.position = _position;

		// Assign the given material to render the cube with
		setMaterial(mat);

		// Set the cube's data size
		updateBrickSize(_size);

		// Set the maximum z level
		updateMaxZLevel(calculateMaxLevels());

		// Set the last bit mask this brick uses
		updateLastBitMask(calculateLastBitMask());

		// Set the default level of detail that this brick renders to
		updateCurrentZLevel(0);
	}

	/*****************************************************************************
	 * UTILITY FUNCTIONS
	 *****************************************************************************/
	// Returns the computed maximum number of z levels this brick can render to.
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

	// Returns the computed last bit mask for this brick.
	public uint calculateLastBitMask()
	{
		int zBits = maxZLevel * 3;
		uint lbm = (uint)1 << zBits;
		return lbm;
	}

	// The file must already have been opened.
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
			cube.GetComponent<Renderer>().material.SetTexture("_VolumeDataTexture", data);

			return data;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}

	}

	// The file must already have been opened.
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
			cube.GetComponent<Renderer>().material.SetTexture("_VolumeDataTexture", data);

			return data;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}

	}

	// Generates a struct of the bricks data that can be analyzed in the compute shader.
	public BrickData getBrickData()
	{
		BrickData newData;

		newData.size = size;
		newData.position = cube.transform.position;
		newData.maxZLevel = maxZLevel;
		newData.currentZLevel = currentZLevel;
		newData.updateData = false;

		return newData;
	}

	/*****************************************************************************
	 * ACCESSORS
	 *****************************************************************************/
	// Returns the Unity game object associated with this brick.
	public GameObject getGameObject()
	{
		return cube;
	}

	// Returns the material of the Unity game object.
	public Material getMaterial()
	{
		return cube.GetComponent<Renderer>().material;
	}

	// Returns the size of the brick.
	public int getSize()
	{
		return size;
	}

	// Returns the maximum number of Z-order levels this brick can render to.
	public int getMaxZLevel()
	{
		return maxZLevel;
	}

	// Returns the current Z-order level that the brick is rendering to.
	public int getCurrentZLevel()
	{
		return currentZLevel;
	}

	// Returns the last bit mask used to access data in hz curve of this brick.
	public uint getLastBitMask()
	{
		return lastBitMask;
	}

	/*****************************************************************************
	 * MUTATOR AND SHADER BUFFERING METHODS
	 *****************************************************************************/
	// Sets this brick's material to the given material.
	public void setMaterial(Material mat)
	{
		cube.GetComponent<Renderer>().material = mat;
	}

	public void updateBrickSize(int _size)
	{
		size = _size;
		cube.GetComponent<Renderer>().material.SetInt("_BrickSize", size);
	}

	// Sets the variable Updates the zRenderLevel on this brick and on the shader.
	public void updateCurrentZLevel(int _currentZlevel)
	{
		// Clamp the input level to be between 0 and maxZLevel, inclusive
		currentZLevel = Math.Min(Math.Max(_currentZlevel, 0), maxZLevel);
		cube.GetComponent<Renderer>().material.SetInt("_CurrentZLevel", currentZLevel);

		// Read in the appropriate amount of data dependent on the current z level to render to
		readRaw8Into3DZLevel();
	}

	public void updateMaxZLevel(int _maxZLevel)
	{
		maxZLevel = _maxZLevel;
		cube.GetComponent<Renderer>().material.SetInt("_MaxZLevel", maxZLevel);
	}

	public void updateLastBitMask(uint _lastBitMask)
	{
		lastBitMask = _lastBitMask;
		cube.GetComponent<Renderer>().material.SetInt("_LastBitMask", (int)lastBitMask);
	}
}

/* BrickData | Marko Sterbentz 7/18/2017
 * This struct mirrors the struct in use by the BrickAnalysis compute shader.
 * Note: Is 28 bytes in size.
 */
public struct BrickData
{
	public int size;                    // 4 bytes							
	public Vector3 position;            // 4 x 3 = 12 bytes
	public int maxZLevel;               // 4 bytes
	public int currentZLevel;           // 4 bytes
	public bool updateData;             // 4 bytes
}