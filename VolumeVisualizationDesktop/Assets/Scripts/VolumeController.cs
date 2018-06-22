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
	public string dataPath = "Assets/Data/BoxHz/";		// The path to the data to be loaded into the renderer

	// Objects to draw for debugging purposes
	public GameObject clippingPlaneCube;
	private LineRenderer boundingBoxLine;
    public bool drawBoundingBox = false;

	// Brick Analysis compute shader
	//public ComputeShader brickAnalysisShader;
	//private int analysisKernelID;

	// Objects for rendering with the compute shader
	public CustomRenderTexture _target;
	public Material renderMaterial;
	public ComputeShader renderingShader;
	private int rendererKernelID;

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

		// DEBUG: Create a box for the plane
		//clippingPlaneCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//clippingPlaneCube.transform.position = clippingPlane.Position;
		//clippingPlaneCube.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
		//clippingPlaneCube.transform.position = clippingPlane.Position;
		//clippingPlaneCube.transform.localScale = new Vector3(2.1f, 2.1f, 0.01f);
		//clippingPlaneCube.transform.rotation = Quaternion.LookRotation(clippingPlane.Normal);

		// Load the compute shader kernel
		//analysisKernelID = brickAnalysisShader.FindKernel("BrickAnalysis");
	}

	/// <summary>
	/// Updates the VolumeController once per frame.
	/// </summary>
	private void Update ()
	{
		// Update the Z-order render level as necessary
		//checkZRenderLevelInput();


	}

	/*****************************************************************************
	 * UTILITY METHODS
	 *****************************************************************************/
	/// <summary>
	/// Updates the Z-order render level of all bricks in the current volume based on user input.
	/// </summary>
	public void checkZRenderLevelInput()
	{
		if (Input.GetKeyDown("0"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(0);
		}
		if (Input.GetKeyDown("1"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(1);
		}
		if (Input.GetKeyDown("2"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(2);
		}
		if (Input.GetKeyDown("3"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(3);
		}
		if (Input.GetKeyDown("4"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(4);
		}
		if (Input.GetKeyDown("5"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(5);
		}
		if (Input.GetKeyDown("6"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(6);
		}
		if (Input.GetKeyDown("7"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(7);
		}
		if (Input.GetKeyDown("8"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(8);
		}
		if (Input.GetKeyDown("9"))
		{
			for (int i = 0; i < currentVolume.Bricks.Length; i++)
				currentVolume.Bricks[i].updateCurrentZLevel(9);
		}
	}

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

	/*****************************************************************************
	* OLD: COMPUTE METHODS
	*****************************************************************************/
	/// <summary>
	/// An example of how to use compute shaders in Unity. 
	/// Does not perform any useful function with regards to rendering the volume.
	/// </summary>
	/// <returns></returns>
	//public BrickData[] runBrickAnalysis()
	//{
	//	// Create the brick data
	//	BrickData[] computeData = new BrickData[currentVolume.Bricks.Length];
	//	for (int i = 0; i < currentVolume.Bricks.Length; i++)
	//	{
	//		computeData[i] = currentVolume.Bricks[i].getBrickData();
	//	}

	//	// Put the brick data into a compute buffer
	//	ComputeBuffer buffer = new ComputeBuffer(computeData.Length, 28); // Note: 28 is the number of bytes in a BrickData struct
	//	buffer.SetData(computeData);

	//	// Send the compute buffer data to the GPU
	//	brickAnalysisShader.SetBuffer(analysisKernelID, "dataBuffer", buffer);

	//	// Send the camera's position to the GPU
	//	brickAnalysisShader.SetVector("cameraPosition", new Vector4(mainCamera.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z, 0.0f));

	//	// Run the kernel
	//	brickAnalysisShader.Dispatch(analysisKernelID, computeData.Length, 1, 1);

	//	// Retrieve the data
	//	BrickData[] analyzedData = new BrickData[currentVolume.Bricks.Length];
	//	buffer.GetData(analyzedData);
	//	buffer.Dispose();

	//	// Return the analyzed data
	//	return analyzedData;
	//}

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
		MetaBrick[] metaBricks = new MetaBrick[currentVolume.Bricks.Length];
		int[] numData = new int[metaBricks.Length];										// holds the number of data voxels to be read in by brick with corresponding id
		for (int i = 0; i < metaBricks.Length; i++)
		{
			// Create the MetaBrick
			metaBricks[i] = currentVolume.Bricks[i].getMetaBrick();
			metaBricks[i].currentZLevel = metaBricks[i].maxZLevel;						// TODO: Change this; it shouldn't necessarily be max level to begin with
			metaBricks[i].id = i;
			metaBricks[i].lastBitMask = currentVolume.Bricks[i].calculateLastBitMask();

			// Set some parameters in the brick
			currentVolume.Bricks[i].CurrentZLevel = metaBricks[i].currentZLevel;

			// Determine the amount of voxels to be read in for this brick
			numData[i] = 1 << metaBricks[i].currentZLevel;								// equivalent to 2^currentZLevel
			numData[i] = numData[i] * numData[i] * numData[i];
		}

		// Initialize the volumeBuffer (the metadata buffer)
		MetaVolume[] metaVolumes = new MetaVolume[1];
		metaVolumes[0] = currentVolume.getMetaVolume();

		// Initialize the dataBuffer (contains the actual data)
		int totalData = 0;
		for (int i = 0; i < numData.Length; i++) totalData += numData[i];
		ComputeBuffer dataBuffer = new ComputeBuffer(totalData, 32); // currentVolume.BitsPerPixel);

		int currentBufferIndex = 0;

		if (currentVolume.BitsPerPixel == 8)
		{
			uint[] rawData = new uint[totalData];						// TODO: This might need to be uints for now... D:

			for (int i = 0; i < currentVolume.Bricks.Length; i++)
			{
				// Read the data and load it into the data buffer
				uint[] newData = currentVolume.Bricks[i].readRaw8Into3DZLevelBufferUint();
				Buffer.BlockCopy(newData, 0, rawData, currentBufferIndex, numData[i]);// newData.Length);

				// Set the metaBricks' bufferIndices
				metaBricks[i].bufferIndex = currentBufferIndex;

				// Move the currentIndex to the end of the data that was just read in
				currentBufferIndex += numData[i];
			}

			dataBuffer.SetData(rawData);
		}
		//else
		//{
		//	ushort[] rawData = new ushort[totalData * currentVolume.BitsPerPixel];

		//	for (int i = 0; i < currentVolume.Bricks.Length; i++)
		//	{
		//		// Read the data and load it into the data buffer
		//		ushort[] newData = currentVolume.Bricks[i].readRaw16Into3DZLevelBuffer();
		//		Buffer.BlockCopy(newData, 0, rawData, currentBufferIndex, newData.Length);

		//		// Set the metaBricks' bufferIndices
		//		metaBricks[i].bufferIndex = currentBufferIndex;

		//		// Move the currentIndex to the end of the data that was just read in
		//		currentBufferIndex += numData[i];
		//	}

		//	dataBuffer.SetData(rawData);
		//}

		// Set the necessary parameters in the compute shader (the two metadata buffers, the data buffer, camera matrices, etc.) 
		ComputeBuffer metaBrickBuffer = new ComputeBuffer(metaBricks.Length, 60);       // n MetaBricks with a size of 60 bytes each
		metaBrickBuffer.SetData(metaBricks);

		ComputeBuffer metaVolumeBuffer = new ComputeBuffer(1, 60);                      // 1 MetaVolume with a size of 60 bytes each
		metaVolumeBuffer.SetData(metaVolumes);

		renderingShader.SetBuffer(rendererKernelID, "_MetaBrickBuffer", metaBrickBuffer);
		renderingShader.SetBuffer(rendererKernelID, "_MetaVolumeBuffer", metaVolumeBuffer);
		renderingShader.SetBuffer(rendererKernelID, "_DataBuffer", dataBuffer);

		//metaBrickBuffer.Dispose();
		//metaVolumeBuffer.Dispose();
		//dataBuffer.Dispose();
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