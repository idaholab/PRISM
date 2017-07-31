/* Volume Controller | Marko Sterbentz 7/3/2017
 * This class is the medium between the data and the volume rendering, and handles requests between the two.
 */
using System;
using System.IO;
using UnityEngine;
using SimpleJSON;
using Vectrosity;
using System.Collections.Generic;

public class VolumeController : MonoBehaviour {

	// The material the bricks render with
	private Material volumeMaterial;

	// Transfer function for modifying visuals
	private TransferFunction transferFunction;

	// Brick data
	private Brick[] bricks;
	private int bitsPerPixel;
	private int bytesPerPixel;
	private int totalBricks;
	private int minLevel;
	private int maxLevel;
	private int[] globalSize;

	// Clipping plane
	private ClippingPlane clippingPlane;

	// Brick Analysis compute shader
	public ComputeShader brickAnalysisShader;
	int analysisKernelID;

	// Main camera
	public Camera mainCamera;

	// Objects to draw for debugging purposes
	public GameObject clippingPlaneCube;
	private VectorLine boundingBoxLine;

	// The data to be loaded into the renderer
	//private string dataPath = "Assets/Data/VisMaleHz2/";
	//private string dataPath = "Assets/Data/fourSkullHz/";
	private string dataPath = "Assets/Data/BoxHz/";

	// Use this for initialization. This will ensure that the global variables needed by other objects are initialized first.
	private void Awake()
	{
		// 0. Set the default material
		Shader hzShader = Shader.Find("Custom/HZVolume");
		volumeMaterial = new Material(hzShader);

		// 1. Load the volume
		loadVolume(dataPath, "metadata.json");

		// 2. Set up the transfer function
		int isovalueRange = calculateIsovalueRange(bitsPerPixel);
		transferFunction = new TransferFunction(isovalueRange);

		// 3. Set up the clipping plane
		clippingPlane = new ClippingPlane(new Vector3(0,0,0), new Vector3(1, 0 ,0), false);
		updateClippingPlaneAll();

		// Creating a wireframe bounding box cube
		boundingBoxLine = new VectorLine("boundingCube", new List<Vector3>(24), 2.0f);
		boundingBoxLine.MakeCube(new Vector3(0.5f, 0.5f, 0.5f), 1, 1, 1);               // Note: Places the corner at (0, 0, 0)

		// Create a box for the plane, if debugging
		//clippingPlaneCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//clippingPlaneCube.transform.position = clippingPlane.Position;
		//clippingPlaneCube.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
		//clippingPlaneCube.transform.position = clippingPlane.Position;
		//clippingPlaneCube.transform.localScale = new Vector3(2.1f, 2.1f, 0.01f);
		//clippingPlaneCube.transform.rotation = Quaternion.LookRotation(clippingPlane.Normal);

		// Load the compute shader kernel
		analysisKernelID = brickAnalysisShader.FindKernel("BrickAnalysis");
	}

	// Update is called once per frame
	private void Update ()
	{
		// Draw the 1 x 1 x 1 bounding box for the volume.
		//boundingBoxLine.Draw();

		// TESTING: Running brickAnalysis compute shader

		// TODO: change z rendering level depending on camera view, z-buffer, etc.
		// BrickData[] analyzedData = runBrickAnalysis();

		//// Use the analyzed data
		//for (int i = 0; i < analyzedData.Length; i++)
		//{
		//	Debug.Log("Size: " + analyzedData[i].size);
		//	Debug.Log("Position: " + analyzedData[i].position);
		//	Debug.Log("MaxZLevel: " + analyzedData[i].maxZLevel);
		//	Debug.Log("CurrentZLevel: " + analyzedData[i].currentZLevel);
		//	Debug.Log("Update Data: " + analyzedData[i].updateData);
		//}

		// END TESTING

		// TESTING: Code for changing the z render level with the number keys
		if (Input.GetKeyDown("0"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(0);
		}
		if (Input.GetKeyDown("1"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(1);
		}
		if (Input.GetKeyDown("2"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(2);
		}
		if (Input.GetKeyDown("3"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(3);
		}
		if (Input.GetKeyDown("4"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(4);
		}
		if (Input.GetKeyDown("5"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(5);
		}
		if (Input.GetKeyDown("6"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(6);
		}
		if (Input.GetKeyDown("7"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(7);
		}
		if (Input.GetKeyDown("8"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(8);
		}
		if (Input.GetKeyDown("9"))
		{
			for (int i = 0; i < bricks.Length; i++)
				bricks[i].updateCurrentZLevel(9);
		}

		// END TESTING
	}

	/*****************************************************************************
	 * COMPUTE METHODS
	 *****************************************************************************/
	public BrickData[] runBrickAnalysis()
	{
		// Create the brick data
		BrickData[] computeData = new BrickData[bricks.Length];
		for (int i = 0; i < bricks.Length; i++)
		{
			computeData[i] = bricks[i].getBrickData();
		}

		// Put the brick data into a compute buffer
		ComputeBuffer buffer = new ComputeBuffer(computeData.Length, 28);                                                                           // TODO: Stop hardcoding the struct size?
		buffer.SetData(computeData);

		// Send the compute buffer data to the GPU
		brickAnalysisShader.SetBuffer(analysisKernelID, "dataBuffer", buffer);

		// Send the camera's position to the GPU
		brickAnalysisShader.SetVector("cameraPosition", new Vector4(mainCamera.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z, 0.0f));

		// Run the kernel
		brickAnalysisShader.Dispatch(analysisKernelID, computeData.Length, 1, 1);

		// Retrieve the data
		BrickData[] analyzedData = new BrickData[bricks.Length];
		buffer.GetData(analyzedData);
		buffer.Dispose();

		// Return the analyzed data
		return analyzedData;
	}

	/*****************************************************************************
	 * VOLUME CREATION METHODS
	 *****************************************************************************/
	// Loads in the volume specified by the given metadata file
	public void loadVolume(string filePath, string metadataFileName)
	{
		// 1. Read in the metadata file
		loadMetadata(filePath, metadataFileName);
		
	}

	// Loads the data from the given metadata file path directly to the member variables of VolumeController
	private void loadMetadata(string filePath, string metadataFileName)
	{
		try
		{
			// Read the JSON file specified by the path
			StreamReader reader = new StreamReader(filePath + metadataFileName);
			string textFromFile = reader.ReadToEnd();
			reader.Close();

			// Create a dictionary to store the values of the JSON file
			JSONNode N = JSON.Parse(textFromFile);

			// Assign the data from the file to the member variables of VolumeController
			minLevel = N["minLevel"].AsInt;
			maxLevel = N["maxLevel"].AsInt;
			totalBricks = N["totalBricks"].AsInt;
			bytesPerPixel = N["bytesPerPixel"].AsInt;
			bitsPerPixel = bytesPerPixel * 8;
			globalSize = new int[]{  N["globalSize"][0].AsInt,
									 N["globalSize"][1].AsInt,
									 N["globalSize"][2].AsInt };
			bricks = new Brick[totalBricks];

			// Create and position all of the bricks within a 1 x 1 x 1 cube
			for (int i = 0; i < bricks.Length; i++)
			{
				MetadataBrick newBrickData = new MetadataBrick();
				newBrickData.filename = filePath + N["bricks"][i]["filename"];
				newBrickData.size = N["bricks"][i]["size"];
				newBrickData.position = new Vector3(0, 0, 0);
				bricks[i] = new Brick(newBrickData, volumeMaterial);

				// Generate boundingVolumeCorner and boundingVolumeCenter in world space
				float maxGlobalSize = Mathf.Max(globalSize);
				Vector3 boundingVolumeCenter = new Vector3(maxGlobalSize, maxGlobalSize, maxGlobalSize) / 2.0f;
				Vector3 boundingVolumeCorner = new Vector3(0, 0, 0);
				Vector3 b = boundingVolumeCenter - boundingVolumeCorner;

				// Find volumeCenter and volumeCorner of the data in world space
				Vector3 volumeCenter = new Vector3(globalSize[0], globalSize[1], globalSize[2]) / 2.0f;
				Vector3 volumeCorner = new Vector3(0, 0, 0);
				Vector3 c = volumeCenter - volumeCorner;

				// Calculate vector a: the bottom left corner of the volume in world space
				Vector3 volumeCornerWorldSpace = b - c;

				// p is the position of the brick in pixel space
				Vector3 p = new Vector3( N["bricks"][i]["position"][0].AsInt, 
										 N["bricks"][i]["position"][1].AsInt,
										 N["bricks"][i]["position"][2].AsInt); 

				// pHat is the position of the brick in world space
				Vector3 pHat = p / maxGlobalSize;

				// Calculate the position for each brick in world space
				Vector3 brickPosition = volumeCornerWorldSpace + p;

				// Calculate the brick offset
				Vector3 brickOffset = new Vector3(bricks[i].getSize(), bricks[i].getSize(), bricks[i].getSize()) / 2.0f;

				// Calculate final position for the brick
				bricks[i].getGameObject().transform.position += (brickPosition + brickOffset) / maxGlobalSize;

				// Scale the bricks to the correct size
				bricks[i].getGameObject().transform.localScale = new Vector3(bricks[i].getSize(), bricks[i].getSize(), bricks[i].getSize()) / maxGlobalSize;
			}

			Debug.Log("Metadata read.");	
		}
		catch (Exception e)
		{
			Debug.Log("Failed to read the metadata file: " + e);
		}
	}

	// Generates the isovalue range given the number of bits per pixels (i.e. format of the data)
	private int calculateIsovalueRange(int bitsPerPixel)
	{
		return ((int) Mathf.Pow(2.0f, bitsPerPixel)) - 1;
	}

	/*****************************************************************************
	 * SHADER BUFFERING METHODS
	 *****************************************************************************/
	public void updateMaterialPropFloat(string propName, float val, int brickIndex)
	{
		bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetFloat(propName, val);
	}

	public void updateMaterialPropFloatAll(string propName, float val)
	{
		// Update the volumeMaterial
		volumeMaterial.SetFloat(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			bricks[i].getGameObject().GetComponent<Renderer>().material.SetFloat(propName, val);
		}
	}

	public void updateMaterialPropFloat3(string propName, float[] val, int brickIndex)
	{
		bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetFloatArray(propName, val);
	}

	public void updateMaterialPropFloat3All(string propName, float[] val)
	{
		// Update the volumeMaterial
		volumeMaterial.SetFloatArray(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			bricks[i].getGameObject().GetComponent<Renderer>().material.SetFloatArray(propName, val);
		}
	}

	public void updateMaterialPropInt(string propName, int val, int brickIndex)
	{
		bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetInt(propName, val);
	}

	public void updateMaterialPropIntAll(string propName, int val)
	{
		// Update the volumeMaterial
		volumeMaterial.SetInt(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			bricks[i].getGameObject().GetComponent<Renderer>().material.SetInt(propName, val);
		}
	}

	public void updateMaterialPropTexture2D(string propName, Texture2D tex, int brickIndex)
	{
		bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
	}

	public void updateMaterialPropTexture2DAll(string propName, Texture2D tex)
	{
		// Update the volumeMaterial
		volumeMaterial.SetTexture(propName, tex);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			bricks[i].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
		}
	}

	public void updateMaterialPropTexture3D(string propName, Texture3D tex, int brickIndex)
	{
		bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
	}

	public void updateMaterialPropTexture3DAll(string propName, Texture3D tex)
	{
		// Update the volumeMaterial
		volumeMaterial.SetTexture(propName, tex);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			bricks[i].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
		}
	}

	public void updateMaterialPropVector3(string propName, Vector3 val, int brickIndex)
	{
		bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetVector(propName, val);
	}

	public void updateMaterialPropVector3All(string propName, Vector3 val)
	{
		// Update the volumeMaterial
		volumeMaterial.SetVector(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			bricks[i].getGameObject().GetComponent<Renderer>().material.SetVector(propName, val);
		}
	}

	/*****************************************************************************
	 * SHADER BUFFERING METHODS WRAPPERS
	 *****************************************************************************/
	// Sends the values in the VolumeController's ClippingPlane to all bricks' materials.
	public void updateClippingPlaneAll()
	{
		for (int i = 0; i < bricks.Length; i++)
		{
			// Transform the clipping plane from world to local space of the current brick, accounting for scale
			Vector3 localClippingPlanePosition = bricks[i].getGameObject().transform.InverseTransformPoint(clippingPlane.Position); 

			// Send the transformed clipping plane location to the current brick's material shader
			updateMaterialPropVector3("_ClippingPlanePosition", localClippingPlanePosition, i);;
		}

		// Update the clipping plane's normal for all bricks 
		updateMaterialPropVector3All("_ClippingPlaneNormal", clippingPlane.Normal);

		// Update the clipping plane's enabled status for all bricks
		updateMaterialPropIntAll("_ClippingPlaneEnabled", Convert.ToInt32(clippingPlane.Enabled));
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
	public TransferFunction getTransferFunction()
	{
		return transferFunction;
	}

	public Material getVolumeMaterial()
	{
		return volumeMaterial;
	}

	public ClippingPlane getClippingPlane()
	{
		return clippingPlane;
	}
}

/* Brick | Marko Sterbentz 7/3/2017
 * This class holds the data associated with a single brick of the volume to be rendered.
 */ 
public class Brick
{
	//private FileStream file;										// The FileStream where the data for this brick is stored.
	private GameObject cube;										// The cube GameObject that is the rendered representation of this brick.
	private int size;												// The dimensions of the data in voxel space. Size is assumed to be a power of 2.
	private int maxZLevel;                                         // The max number of Z-Order levels this brick has.
	private int currentZLevel;                                     // The current Z-Order rendering level for this brick.
	private uint lastBitMask;                                       // The last bit mask used for accessing the data.
	private string filename;

	public Brick()
	{

	}

	public Brick (MetadataBrick brickData, Material mat)
	{
		// Open the data file associated with this brick
		//file = new FileStream(brickData.filename, FileMode.Open);
		filename = brickData.filename;

		// Initialize a Unity cube to represent the brick
		cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

		// Set the position of the 
		cube.transform.position = new Vector3(brickData.position[0], brickData.position[1], brickData.position[2]);

		// Assign the given material to render the cube with
		setMaterial(mat);

		// Set the cube's data size
		updateBrickSize(brickData.size);

		// Set the maximum z level
		updateMaxZLevel(calculateMaxLevels());

		// Set the last bit mask this brick uses
		updateLastBitMask(calculateLastBitMask());

		// Set the current level of detail that this brick renders to
		//updateCurrentZLevel(maxZLevel);                                                               // TODO: Set it to another default value
		updateCurrentZLevel(0);
	}

	/*****************************************************************************
	 * UTILITY FUNCTIONS
	 *****************************************************************************/
	// Returns the computed maximum number of z levels this brick can render to.
	public int calculateMaxLevels()
	{
		int totalLevels = 0;																			// NOTE: Starting at 0, instead of -1
		int tempBrickSize = size;

		while(Convert.ToBoolean(tempBrickSize >>= 1))
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

	// Returns the maximum number of zz levels this brick can render to.
	public int getMaxZLevel()
	{
		return maxZLevel;
	}

	// Returns the current zz level that the brick is rendering to.
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

/* Metadata | Marko Sterbentz 7/5/2017
 * This class/struct stores the data provided by a metadata file.
 */
[Serializable]
public class Metadata
{
	public int minLevel;
	public int maxLevel;
	public MetadataBrick[] bricks;
	public int totalBricks;
	public int bytesPerPixel;
	public int[] globalSize;
}

/* Metadata Brick | Marko Sterbentz 7/5/2017
 * This class/struct acts as an intermediary to store the metadata of a brick.
 */ 
[Serializable]
public class MetadataBrick
{
	public string filename;
	public int size;
	public Vector3 position;
}

/* BrickData | Marko Sterbentz 7/18/2017
 * This struct mirrors the struct in use by the BrickAnalysis compute shader.
 * Note: Is 28 bytes in size.
 */ 
public struct BrickData
{
	public int size;					// 4 bytes							
	public Vector3 position;			// 4 x 3 = 12 bytes
	public int maxZLevel;				// 4 bytes
	public int currentZLevel;			// 4 bytes
	public bool updateData;				// 4 bytes
}
/* Clipping Plane | Marko Sterbentz 7/11/2017
 * This class contains the data for a plane that can be used to clip the volume.
 */ 
public class ClippingPlane
{
	/* Member variables */
	private Vector3 position;
	private Vector3 normal;
	private bool enabled;

	/* Properties */
	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			position = value;
		}
	}

	public Vector3 Normal
	{
		get
		{
			return normal;
		}
		set
		{
			normal = value;
		} 
	}

	public bool Enabled
	{
		get
		{
			return enabled;
		}
		set
		{
			enabled = value;
		}
	}

	/* Constructors */
	public ClippingPlane()
	{
		position = new Vector3(0.5f, 0.5f, 0.5f);
		normal = new Vector3(1, 0, 0);
		enabled = false;
	}

	public ClippingPlane(Vector3 _position, Vector3 _normal, bool _enabled)
	{
		position = _position;
		normal = _normal;
		enabled = _enabled;
	}
}
