using System;
using UnityEngine;
using Vectrosity;
using System.Collections.Generic;

/* Volume Controller | Marko Sterbentz 7/3/2017
 * This class is the medium between the data and the volume rendering, and handles requests between the two.
 */
public class VolumeController : MonoBehaviour {
	
	private Volume currentVolume;						// The data volume that will be rendered
	private TransferFunction transferFunction;			// Transfer function for modifying visuals
	private ClippingPlane clippingPlane;				// Clipping plane for modifying viewable portion of the volume
	public Camera mainCamera;							// Main camera in the scene		
	public string dataPath = "Assets/Data/BoxHz/";		// The path to the data to be loaded into the renderer

	// Objects to draw for debugging purposes
	public GameObject clippingPlaneCube;
	private VectorLine boundingBoxLine;

	// Brick Analysis compute shader
	public ComputeShader brickAnalysisShader;
	private int analysisKernelID;

	// Use this for initialization. This will ensure that the global variables needed by other objects are initialized first.
	private void Awake()
	{
		// 0. Create the default material
		Shader hzShader = Shader.Find("Custom/HZVolume");
		Material volumeMaterial = new Material(hzShader);

		// 1. Create the data volume
		currentVolume = new Volume(dataPath, "metadata.json", volumeMaterial);

		// 2. Set up the transfer function
		int isovalueRange = currentVolume.calculateIsovalueRange();
		transferFunction = new TransferFunction(isovalueRange);

		// 3. Set up the clipping plane
		clippingPlane = new ClippingPlane(new Vector3(0,0,0), new Vector3(1, 0 ,0), false);
		updateClippingPlaneAll();

		// DEBUG: Creating a wireframe bounding box cube
		boundingBoxLine = new VectorLine("boundingCube", new List<Vector3>(24), 2.0f);
		boundingBoxLine.MakeCube(new Vector3(0.5f, 0.5f, 0.5f), 1, 1, 1);               // Note: Places the corner at (0, 0, 0)

		// DEBUG: Create a box for the plane
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
		boundingBoxLine.Draw();

		// Update the Z-order render level as necessary
		checkZRenderLevelInput();

		// TESTING: Running brickAnalysis compute shader
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
	}

	/*****************************************************************************
	 * UTILITY METHODS
	 *****************************************************************************/
	 // Updates the Z-order render level of all bricks in the current volume based on user input.
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
	// Sends the values in the VolumeController's ClippingPlane to all bricks' materials.
	public void updateClippingPlaneAll()
	{
		for (int i = 0; i < currentVolume.Bricks.Length; i++)
		{
			// Transform the clipping plane from world to local space of the current brick, accounting for scale
			Vector3 localClippingPlanePosition = currentVolume.Bricks[i].getGameObject().transform.InverseTransformPoint(clippingPlane.Position);

			// Send the transformed clipping plane location to the current brick's material shader
			currentVolume.updateMaterialPropVector3("_ClippingPlanePosition", localClippingPlanePosition, i);;
		}

		// Update the clipping plane's normal for all bricks 
		currentVolume.updateMaterialPropVector3All("_ClippingPlaneNormal", clippingPlane.Normal);

		// Update the clipping plane's enabled status for all bricks
		currentVolume.updateMaterialPropIntAll("_ClippingPlaneEnabled", Convert.ToInt32(clippingPlane.Enabled));
	}

	/*****************************************************************************
	 * ACCESSORS AND MUTATORS
	 *****************************************************************************/
	public TransferFunction getTransferFunction()
	{
		return transferFunction;
	}

	public Volume getCurrentVolume()
	{
		return currentVolume;
	}

	public ClippingPlane getClippingPlane()
	{
		return clippingPlane;
	}

	/*****************************************************************************
	* COMPUTE METHODS
	*****************************************************************************/
	public BrickData[] runBrickAnalysis()
	{
		// Create the brick data
		BrickData[] computeData = new BrickData[currentVolume.Bricks.Length];
		for (int i = 0; i < currentVolume.Bricks.Length; i++)
		{
			computeData[i] = currentVolume.Bricks[i].getBrickData();
		}

		// Put the brick data into a compute buffer
		ComputeBuffer buffer = new ComputeBuffer(computeData.Length, 28); // Note: 28 is the number of bytes in a BrickData struct
		buffer.SetData(computeData);

		// Send the compute buffer data to the GPU
		brickAnalysisShader.SetBuffer(analysisKernelID, "dataBuffer", buffer);

		// Send the camera's position to the GPU
		brickAnalysisShader.SetVector("cameraPosition", new Vector4(mainCamera.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z, 0.0f));

		// Run the kernel
		brickAnalysisShader.Dispatch(analysisKernelID, computeData.Length, 1, 1);

		// Retrieve the data
		BrickData[] analyzedData = new BrickData[currentVolume.Bricks.Length];
		buffer.GetData(analyzedData);
		buffer.Dispose();

		// Return the analyzed data
		return analyzedData;
	}
}