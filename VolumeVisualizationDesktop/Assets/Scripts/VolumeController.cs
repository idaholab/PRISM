/* Volume Controller | Marko Sterbentz 7/3/2017 */

using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This class is the medium between the data and the volume rendering, and handles requests between the two.
/// </summary>
public class VolumeController : MonoBehaviour {
	
	private Volume currentVolume;						// The data volume that will be rendered
	private TransferFunction transferFunction;			// Transfer function for modifying visuals
	private ClippingPlane clippingPlane;				// Clipping plane for modifying viewable portion of the volume
	public Camera mainCamera;							// Main camera in the scene		
	public string dataPath = "Assets/Data/BoxHz/";      // The path to the data to be loaded into the renderer

	// Objects to draw for debugging purposes
	public GameObject clippingPlaneCube;
	private LineRenderer boundingBoxLine;
    public bool drawBoundingBox = false;

	// Objects for rendering with the compute shader
	private int rendererKernelID;
	private ComputeBuffer metaBrickBuffer;
	private ComputeBuffer metaVolumeBuffer;

	private CustomRenderTexture _target;
	[Tooltip("The compute shader file that contains the volume rendering kernel.")]
	public ComputeShader renderingShader;
	[Tooltip("The material used to draw the render texture to the screen.")]
	public Material renderMaterial;
	[Tooltip("Max size of a single RWStructuredBuffer. (in bytes)"), Range(10000000,300000000)]
	public int maxBufferSize = 200000000;
	[Tooltip("The maximum number of data buffers to use."), Range(1, 10)]
	public int numberOfBuffers = 2;

	private int maxNumberOfBuffers = 3;

	private ComputeBuffer[] dataBuffers;
	private Dictionary<int, string> dataBufferNames = new Dictionary<int, string>{
		{ 0, "_DataBufferZero"},
		{ 1, "_DataBufferOne" },
		{ 2, "_DataBufferTwo" }
	};

	/// <summary>
	/// Initialization function for the VolumeController. Ensures that the global variables needed by other objects are initialized first.
	/// </summary>
	private void Awake()
	{
        // 0. Create the data volume and its rendering material
        currentVolume = new Volume(dataPath, "metadata.json");

        // 1. Set up the transfer function
        int isovalueRange = currentVolume.calculateIsovalueRange();
		transferFunction = new TransferFunction(isovalueRange);

		// 2. Set up the clipping plane
		clippingPlane = new ClippingPlane(new Vector3(0,0,0), new Vector3(1, 0 ,0), false);
		updateClippingPlaneAll();

		// 3. Set up the target for the camera controls
		CameraControls cameraControls = (CameraControls)mainCamera.GetComponent(typeof(CameraControls));
		cameraControls.target = currentVolume.VolumeCube;

		// 4. Initialize the render texture, compute shader, and other objects needed for rendering
		initializeComputeRenderer();

		// DEBUG: Creating a wireframe bounding box cube
		if (drawBoundingBox)
        {
            boundingBoxLine = createBoundingBoxLineRenderer();
        }
	}

	/// <summary>
	/// Updates the VolumeController once per frame.
	/// </summary>
	private void Update ()
	{

	}

	/// <summary>
	/// Dispose of all compute buffers when the application quits
	/// </summary>
	private void OnApplicationQuit()
	{
		metaBrickBuffer.Dispose();
		metaVolumeBuffer.Dispose();
		for (int i = 0; i < dataBuffers.Length; i++)
		{
			dataBuffers[i].Dispose();
		}
	}

	/*****************************************************************************
	 * UTILITY METHODS
	 *****************************************************************************/
	/*****************************************************************************
	 * SHADER BUFFERING METHODS WRAPPERS
	 *****************************************************************************/
	/// <summary>
	/// Sends the values in the VolumeController's ClippingPlane to all bricks' materials.
	/// </summary>
	public void updateClippingPlaneAll()
	{
		for (int i = 0; i < currentVolume.Bricks.Length; i++)
		{
			// Transform the clipping plane from world to local space of the current brick, accounting for scale
			Vector3 localClippingPlanePosition = currentVolume.Bricks[i].GameObject.transform.InverseTransformPoint(clippingPlane.Position);

			// Send the transformed clipping plane location to the current brick's material shader
			currentVolume.updateMaterialPropVector3("_ClippingPlanePosition", localClippingPlanePosition, i);;
		}

		// Update the clipping plane's normal for all bricks 
		currentVolume.updateMaterialPropVector3All("_ClippingPlaneNormal", clippingPlane.Normal);

		// Update the clipping plane's enabled status for all bricks
		currentVolume.updateMaterialPropIntAll("_ClippingPlaneEnabled", Convert.ToInt32(clippingPlane.Enabled));
	}

	/// <summary>
	/// Creates a bounding box Line Renderer to be used for debugging purposes.
	/// </summary>
	/// <returns></returns>
	private LineRenderer createBoundingBoxLineRenderer()
	{
		GameObject myLine = new GameObject();
		myLine.name = "Bounding Box Line Renderer";
		myLine.transform.position = new Vector3(0, 0, 0);
		myLine.AddComponent<LineRenderer>();
		LineRenderer lr = myLine.GetComponent<LineRenderer>();
		lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
		lr.startColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		lr.endColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		lr.SetWidth(0.01f, 0.01f);
		lr.sortingOrder = 100; // sort it to draw on top of most other objects

		lr.positionCount = 16;
		Vector3[] vertices =
		{
			new Vector3(0,0,0),
			new Vector3(0,1,0),
			new Vector3(1,1,0),
			new Vector3(1,0,0),
			new Vector3(0,0,0),
			new Vector3(0,0,1),
			new Vector3(0,1,1),
			new Vector3(0,1,0),
			new Vector3(0,1,1),
			new Vector3(1,1,1),
			new Vector3(1,1,0),
			new Vector3(1,1,1),
			new Vector3(1,0,1),
			new Vector3(0,0,1),
			new Vector3(1,0,1),
			new Vector3(1,0,0),
		};
		lr.SetPositions(vertices);

		return lr;
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
	/// <summary>
	/// Returns the transfer function used by the volume controller.
	/// </summary>
	/// <returns></returns>
	public TransferFunction getTransferFunction()
	{
		return transferFunction;
	}

	/// <summary>
	/// Returns the current volume used by the volume controller.
	/// </summary>
	/// <returns></returns>
	public Volume getCurrentVolume()
	{
		return currentVolume;
	}

	/// <summary>
	/// Returns the clipping plane used by the volume controller.
	/// </summary>
	/// <returns></returns>
	public ClippingPlane getClippingPlane()
	{
		return clippingPlane;
	}

	/// <summary>
	/// Returns a reference to the rendering compute shader used by the volume controller.
	/// </summary>
	/// <returns></returns>
	public ComputeShader getRenderingComputeShader()
	{
		return renderingShader;
	}

	/// <summary>
	/// Returns the id of the renderer kernel in the compute shader.
	/// </summary>
	/// <returns></returns>
	public int getRendererKernelID()
	{
		return rendererKernelID;
	}

	/*****************************************************************************
	* NEW: COMPUTE RENDERING METHODS
	*****************************************************************************/
	/// <summary>
	/// Creates a compute shader for rendering the volume.
	/// Note: Assumes that the volume has already been initialized.
	/// </summary>
	private void initializeComputeRenderer()
	{
		// Compile and create the compute shader
		rendererKernelID = renderingShader.FindKernel("CSMain");

		// Initialize the brickBuffer (the metadata buffer)
		MetaBrick[] metaBricks = initMetaBrickBuffer();

		// Initialize the volumeBuffer (the metadata buffer)
		MetaVolume[] metaVolumes = initMetaVolumeBuffer();

		// BEGIN NEW TEST CODE
		initDataBuffers(metaBricks);

		// Set the necessary parameters in the compute shader
		metaBrickBuffer = new ComputeBuffer(metaBricks.Length, 64);       // n MetaBricks with a size of 64 bytes each
		metaBrickBuffer.SetData(metaBricks);

		metaVolumeBuffer = new ComputeBuffer(1, 64);                      // 1 MetaVolume with a size of 64 bytes each
		metaVolumeBuffer.SetData(metaVolumes);

		renderingShader.SetBuffer(rendererKernelID, "_MetaBrickBuffer", metaBrickBuffer);
		renderingShader.SetBuffer(rendererKernelID, "_MetaVolumeBuffer", metaVolumeBuffer);

		// Set other rendering properties
		renderingShader.SetInt("_Steps", 128);
		// END NEW TEST CODE

		// BEGIN ORIGINAL CODE
		//// Initialize the dataBuffer (contains the actual data)
		//int totalData = 0;
		//for (int i = 0; i < metaBricks.Length; i++)
		//	totalData += getMetaBrickDataSize(metaBricks[i]);
		////dataBufferZero = new ComputeBuffer(totalData, 32);
		//dataBufferZero = new ComputeBuffer( (int) Math.Ceiling(totalData / 4.0), 32);

		//int currentBufferIndex = 0;

		//if (currentVolume.BitsPerPixel == 8)
		//{
		//	//uint[] rawData = new uint[totalData];
		//	uint[] rawData = new uint[(int) Math.Ceiling(totalData / 4.0)];

		//	for (int i = 0; i < currentVolume.Bricks.Length; i++)
		//	{
		//		// Read the data and load it into the data buffer
		//		uint[] newData = currentVolume.Bricks[i].readRaw8Into3DZLevelBufferUint();
		//		//Buffer.BlockCopy(newData, 4 * 0, rawData, 4 * currentBufferIndex, newData.Length * 4);//4 * numData[i]);	// CRITICAL NOTE: These indices are byte offsets, not index offsets. Must multiply each offset by the size of the data being copied (i.e. 32 bit int = mulit by 4 bytes)

		//		// Copy the packed new data to the data buffer
		//		for (int j = 0; j < newData.Length; j++)
		//		{
		//			rawData[currentBufferIndex + j] = newData[j];
		//		}

		//		// Set the metaBricks' bufferIndices
		//		metaBricks[i].bufferOffset = currentBufferIndex;

		//		// Move the currentIndex to the end of the data that was just read in
		//		currentBufferIndex += newData.Length; // numData[i];
		//	}

		//	dataBufferZero.SetData(rawData);
		//}

		//// Set the necessary parameters in the compute shader (the two metadata buffers, the data buffer, camera matrices, etc.) 
		//metaBrickBuffer = new ComputeBuffer(metaBricks.Length, 64);       // n MetaBricks with a size of 60 bytes each
		//metaBrickBuffer.SetData(metaBricks);

		//metaVolumeBuffer = new ComputeBuffer(1, 64);                      // 1 MetaVolume with a size of 64 bytes each
		//metaVolumeBuffer.SetData(metaVolumes);

		//renderingShader.SetBuffer(rendererKernelID, "_MetaBrickBuffer", metaBrickBuffer);
		//renderingShader.SetBuffer(rendererKernelID, "_MetaVolumeBuffer", metaVolumeBuffer);
		//renderingShader.SetBuffer(rendererKernelID, "_DataBufferZero", dataBufferZero);
		// END ORIGINAL CODE
	}

	/// <summary>
	/// Create the array of brick metadata for use in the compute shader based on the bricks in the currentVolume.
	/// </summary>
	/// <returns></returns>
	private MetaBrick[] initMetaBrickBuffer()
	{
		MetaBrick[] metaBricks = new MetaBrick[currentVolume.Bricks.Length];
		for (int i = 0; i < metaBricks.Length; i++)
		{
			// Create the MetaBrick
			metaBricks[i] = currentVolume.Bricks[i].getMetaBrick();
			metaBricks[i].currentZLevel = metaBricks[i].maxZLevel;                      // TODO: Change this; it shouldn't necessarily be max level to begin with
			metaBricks[i].id = i;
			metaBricks[i].lastBitMask = currentVolume.Bricks[i].calculateLastBitMask();

			// Set some parameters in the brick
			currentVolume.Bricks[i].CurrentZLevel = metaBricks[i].currentZLevel;
		}
		return metaBricks;
	}

	/// <summary>
	/// Determine the amount of data voxels to be read in for the given brick.
	/// </summary>
	/// <param name="br"></param>
	/// <returns></returns>
	private int getMetaBrickDataSize(MetaBrick br)
	{
		int numData = 1 << br.currentZLevel;
		return (numData * numData * numData);

	}

	/// <summary>
	/// Create the "array" of volume metadata for use in the compute shader.
	/// </summary>
	private MetaVolume[] initMetaVolumeBuffer()
	{
		MetaVolume[] metaVolume = new MetaVolume[1];
		metaVolume[0] = currentVolume.getMetaVolume();
		return metaVolume;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="mb"></param>
	private void initDataBuffers(MetaBrick[] mb)
	{
		// Ensure that extra compute buffers are not unnecessarily initialized
		if (currentVolume.Bricks.Length < numberOfBuffers)
			numberOfBuffers = currentVolume.Bricks.Length;

		dataBuffers = new ComputeBuffer[maxNumberOfBuffers];	// The compute buffers that will hold the data
		int[] bufferDataSizes = new int[maxNumberOfBuffers];	// Total size of the data to be put in the ith buffer

		// Initialize the lists of bricks associated with each buffer
		Dictionary<int, List<int>> bricksInBuffer = new Dictionary<int, List<int>>();
		for (int i = 0; i < numberOfBuffers; i++)
		{
			bricksInBuffer.Add(i, new List<int>());
		}

		// Pre-compute the sizes of the compute buffers and which bricks will go inside of each
		for (int i = 0; i < mb.Length; i++)
		{
			int currentBrickSize = getMetaBrickDataSize(mb[i]);					// Get the amount of data points in the current brick	
			currentBrickSize = (int) Math.Ceiling(currentBrickSize / 4.0);		// Account for any padding when packing the bytes into uints
			bufferDataSizes[i % numberOfBuffers] += currentBrickSize;			// Ensure there is room for the ith brick in this buffer
			bricksInBuffer[i % numberOfBuffers].Add(i);							// Note the index of the brick to be added to the buffer
		}

		// Allocate the compute buffers, read in the data, copy the data into the buffer, and send the data to the compute shader
		for (int bufIdx = 0; bufIdx < numberOfBuffers; bufIdx++)
		{
			dataBuffers[bufIdx] = new ComputeBuffer(bufferDataSizes[bufIdx], sizeof(uint)); // new ComputeBuffer((int)Math.Ceiling(bufferDataSizes[bufIdx] / 4.0), 32);
			int currentBufferOffset = 0;

			uint[] rawData = new uint[bufferDataSizes[bufIdx]]; // new uint[(int)Math.Ceiling(bufferDataSizes[bufIdx] / 4.0)];
			for (int j = 0; j < bricksInBuffer[bufIdx].Count; j++)
			{
				int brIdx = bricksInBuffer[bufIdx][j];

				// Read the data and load it into the data buffer
				uint[] newData = currentVolume.Bricks[brIdx].readRaw8Into3DZLevelBufferUint();

				// Copy the packed new data to the data buffer													// TODO: Change this loop copy to Buffer.BlockCopy()
				for (int k = 0; k < newData.Length; k++)
				{
					rawData[currentBufferOffset + k] = newData[k];
				}

				// Set the bufferOffset of the current meta brick
				mb[brIdx].bufferOffset = currentBufferOffset;

				// Set the bufferIndex of the current meta brick
				mb[brIdx].bufferIndex = bufIdx;

				// Update the currentBufferOffset 
				currentBufferOffset += newData.Length;
			}

			// Set the data in the current compute buffer
			dataBuffers[bufIdx].SetData(rawData);

			// Send the data in the compute buffer to the compute shader
			renderingShader.SetBuffer(rendererKernelID, dataBufferNames[bufIdx], dataBuffers[bufIdx]);
		}

		// Initialize any extra buffers to contain 1 single element
		for (int i = numberOfBuffers; i < maxNumberOfBuffers; i++)
		{
			uint[] rawData = new uint[]{ 0 };
			dataBuffers[i] = new ComputeBuffer(1, sizeof(uint));
			dataBuffers[i].SetData(rawData);
			renderingShader.SetBuffer(rendererKernelID, dataBufferNames[i], dataBuffers[i]);
		}
	}

	/// <summary>
	/// Creates a render texture for use with the compute shader
	/// </summary>
	private void initRenderTexture()
	{
		if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
		{
			// Release render texture if we already have one
			if (_target != null)
				_target.Release();

			// Get a render target for Ray Tracing
			//displayTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			_target = new CustomRenderTexture(Screen.width, Screen.height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			_target.material = renderMaterial;
			_target.enableRandomWrite = true;
			_target.Create();
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		SetShaderParameters();
		Render(destination);
	}

	private void SetShaderParameters()
	{
		// Set the necessary parameters in the compute shader
		renderingShader.SetMatrix("_CameraToWorld", mainCamera.cameraToWorldMatrix);
		renderingShader.SetMatrix("_CameraInverseProjection", mainCamera.projectionMatrix.inverse);
	}

	private void Render(RenderTexture destination)
	{
		// Initialize the render texture
		initRenderTexture();

		// Call the compute shader 
		renderingShader.SetTexture(rendererKernelID, "Result", _target);
		int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
		int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
		renderingShader.Dispatch(rendererKernelID, threadGroupsX, threadGroupsY, 1);

		// Blit the results to the screen and finalize the render
		Graphics.Blit(_target, destination);
	}
}