/* Brick | Marko Sterbentz 7/3/2017 Randall Reese 09/11/2018*/

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
	public Brick(string _filename, int _size, Vector3 _position, Vector3 _boxMin, Vector3 _boxMax, Volume _parentVolume)
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
        
        //Setting the parent of the volume. 
        gameObject.transform.SetParent(parentVolume.VolumeCube.transform);

       

        // Set the cube's data size

        Size = _size;

		// Set the maximum z level
		MaxZLevel = calculateMaxLevels();

		// Set the last bit mask this brick uses
		LastBitMask = calculateLastBitMask();

        // Set the brick's min and max corner positions
        BoxMin = _boxMin;
        BoxMax = _boxMax;

        // Set the default level of detail that this brick renders to
        CurrentZLevel = 0;

        // Disable the mesh of this cube so it doesn't render
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
		int totalLevels = 0;        // NOTE: Starting at 0, instead of -1
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
	/// Returns a struct that contains information about this brick to be passed to the compute shader.
	/// </summary>
	/// <returns></returns>
	public MetaBrick getMetaBrick()
	{
		MetaBrick mb;
		mb.position = gameObject.transform.position;
		mb.size = size;
		mb.bufferOffset = 0;
		mb.bufferIndex = 0;
		mb.maxZLevel = maxZLevel;
		mb.currentZLevel = currentZLevel;
		mb.id = -1;
		mb.boxMin = BoxMin;
		mb.boxMax = BoxMax;
		mb.lastBitMask = 0;
		return mb;
	}

	/*****************************************************************************
	 * DATA READING FUNCTIONS
	 *****************************************************************************/
	/// <summary>
	/// Reads a certain amount of the brick's associated data into a byte array. The amount is dependent on the current z level of the brick.
    /// Packs the data into uints for use in the compute shader.
    /// This may mean that the data would need to be re-packed if you want to change the HZ-level. 
    /// You would need to repack the buffer every time it would seem.
    /// 
    /// This is the issue with trying to dynamically decide upon a "correct" level of detail to render a brick at. 
    /// You have to repack the buffer everytime you want to change the z-level of ANY brick. If one brick changes, you need to adjust the compute buffer holding that brick.
    /// Note, however, that every brick in the given compute buffer also needs to be shifted to accomadate the change in the z-level for the brick in question.  
    /// 
    /// This function is not used when we stream the brick data using SIEVAS. 
    /// However, a very similar function is used in the volume controller and the same comments on repacking still stand. 
	/// </summary>
	/// <returns></returns>
	public uint[] readRaw8Into3DZLevelBufferUint()
	{
		try
		{
			// Get the size of the data based on the currentZLevel rendering level
			int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel... Is this the correct dataSize? Yes. 
            int numBytesRead = -1;
			// Read in the bytes
			BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

			int totalDataSize = dataSize * dataSize * dataSize;
			byte[] buffer = new byte[totalDataSize];
			numBytesRead = reader.Read(buffer, 0, sizeof(byte) * buffer.Length);//sizeof(byte) should just be 1. sizeof() returns the size in bytes. 

            if (numBytesRead != totalDataSize)
            {
                Debug.Log("Error reading the data into UInt Buffer in Brick class.");
                
            }

            reader.Close();

            if((totalDataSize % 4) > 0)
            {
                Debug.Log("The total data size is not divisible by 4.");
            }

			// Pack the bytes that were read into a uint array
			uint[] uintBuffer = new uint[Mathf.CeilToInt(totalDataSize / 4.0f)];//Why divide by 4?
            // A uint is four bytes on a standard architechture. Is this why we divide the totalDataSize by 4?
            //This is why we divide by 4. Since each uint can hold 4 bytes, if we have a byte array (which "buffer" is), then dividing the length of the byte array
            //    by 4 tells us how many uints it would take (i.e. how long of a uint array we need) to hold the byte data.
            
            //Here we take a byte array and pack it into a uint array.

            /*  Byte array         |  0  |  1  |  2  |  3  |  4  |  5 |  6 |  7  |  8  |  9  |
             *  Uint array         |  0                    |  1                  |  2        **********|  
             * 
             * Is there just garbage being shoved into the uint array if the data has a nuber of bytes not perfectly divisible by 4?
             * I would seem that when the uint array is intialized, there is just garbage bits shoved into it. Whatever garabage is sitting in memory. 
             * Note however that the total data size is actually divisible by 8 (at the very least) as long as we are rendering at HZ-level of at least 1.
             * dataSize = 2^currentZLevel. totalDataSize is dataSize^3. Hence totalDataSize must be at least divisible by 8 as long as z > 0.  
             * 
             * Now, the question does still remain: Why even do this odd byte packing into uints in the first place? The shader cannot use a byte intrinsic type. 
             * 
             * */


			int[] byteShifts = { 24, 16, 8, 0 };    // use for big endian
			//int[] byteShifts = { 0, 8, 16, 24 };    // use for little endian
			int maskIndex = 0;
			for (int i = 0; i < buffer.Length; i++)
			{
				uint val = buffer[i];//Gets the ith byte from the buffer array. 

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
	/// Note: This is not currently used, but can be used as a guide when adding 16-bit support.
	/// </summary>
	/// <returns></returns>
	public uint[] readRaw16Into3DZLevelBufferUint()
	{
		try
		{
			// Get the size of the data based on the currentZLevel rendering level
			int dataSize = 1 << currentZLevel; // equivalent to 2^currentZLevel

			// Read in the bytes
			BinaryReader reader = new BinaryReader(new FileStream(filename, FileMode.Open));

			int totalDataSize = dataSize * dataSize * dataSize;
			byte[] buffer = new byte[totalDataSize * 2];
			reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
			reader.Close();

			// Convert the bytes to ushort (Note: Buffer.BlockCopy() assumes data is in little endian format).
			// TODO: Figure out a better way to read in ushort rather than doing this... This may not be available on non-Windows platforms?
			ushort[] ushortBuffer = new ushort[buffer.Length / 2];
			Buffer.BlockCopy(buffer, 0, ushortBuffer, 0, buffer.Length);

			// Pack the ushorts into a uint array
			uint[] uintBuffer = new uint[Mathf.CeilToInt(totalDataSize / 2.0f)];
			int[] byteShifts = { 16, 0 };
			int maskIndex = 0;
			for (int i = 0; i < ushortBuffer.Length; i++)
			{
				uint val = ushortBuffer[i];
				uintBuffer[i / 2] = uintBuffer[i / 2] | (val << byteShifts[maskIndex]);
				maskIndex = (maskIndex + 1) % 2;
			}

			return uintBuffer;
		}
		catch (Exception e)
		{
			Debug.Log("Unable to read in the data file into brick: " + e);
			return null;
		}
	}
}