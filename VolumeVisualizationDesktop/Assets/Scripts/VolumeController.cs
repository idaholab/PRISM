/* Volume Controller | Marko Sterbentz 07/03/2017
 *                   | Randall Reese   08/27/2018 */

using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This class is the medium between the data and the volume rendering, and handles requests between the two.
/// </summary>
public class VolumeController : MonoBehaviour {

	private Volume currentVolume;                       // The data volume that will be rendered
	private TransferFunction transferFunction;          // Transfer function for modifying visuals
	private ClippingPlane clippingPlane;                // Clipping plane for modifying viewable portion of the volume
	public Camera mainCamera;                           // Main camera in the scene		

    

    [Tooltip("The path to the data relative to the base directory of the Unity project.")]
	public string dataPath = "Assets/Data/BoxHz/";      // The path to the data to be loaded into the renderer

	// Objects to draw for debugging purposes
	public GameObject clippingPlaneCube;
	private LineRenderer boundingBoxLine;

	[Tooltip("Draws a wireframe bounding box around the volume.")]
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

	[Tooltip("Max size of a single RWStructuredBuffer. (in bytes)"), Range(10000000, 300000000)]
	public int maxBufferSize = 200000000;

    [Tooltip("The maximum number of data buffers to use."), Range(1, 10)]
    public int numberOfBuffers = 3;//This is hard coded as 3 in Unity (Main Camera > Volume Controller > Number Of Buffers. Whatever is here is overridden by what is in Unity.   

	private int maxNumberOfBuffers = 3;//Why is the max three buffers? Is this just a magic number?

	private ComputeBuffer[] dataBuffers;//An array of buffers. 

	private Dictionary<int, string> dataBufferNames = new Dictionary<int, string> {
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

       // currentVolume.Scale = Scale;

       

		// 1. Set up the transfer function
		int isovalueRange = currentVolume.calculateIsovalueRange();
		transferFunction = new TransferFunction(isovalueRange);

		// 2. Set up the clipping plane
		clippingPlane = new ClippingPlane(new Vector3(0, 0, 0), new Vector3(1, 0, 0), false);
		updateClippingPlaneAll();

		// 3. Set up the target for the camera controls
		CameraControls cameraControls = (CameraControls)mainCamera.GetComponent(typeof(CameraControls));
		cameraControls.target = currentVolume.VolumeCube;

        
		// 4. Initialize the render texture, compute shader, and other objects needed for rendering

        //Debug.Log(currentVolume.Scale);


		initializeComputeRenderer();//Called after the volume is scaled. 

		// 5.  Unbound the framerate of this application
		Application.targetFrameRate = -1;

		// 6. Turn off v-sync
		QualitySettings.vSyncCount = 0;

		// DEBUG: Creating a wireframe bounding box cube
		if (drawBoundingBox)
		{

            boundingBoxLine = createBoundingBoxLineRenderer();
            Debug.Log("Trying to draw bounding box" + boundingBoxLine); 
            			
		}
	}

	/// <summary>
	/// Updates the VolumeController once per frame.
	/// </summary>
	private void Update()
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
	 *									PROPERTIES
	 *****************************************************************************/
	public Volume CurrentVolume
	{
		get
		{
			return currentVolume;
		}
	}

	public TransferFunction TransferFunction
	{
		get
		{
			return transferFunction;
		}
	}

	public ClippingPlane ClippingPlane
	{
		get
		{
			return clippingPlane;
		}
	}

	public ComputeShader RenderingComputeShader
	{
		get
		{
			return renderingShader;
		}
	}

	public int RendererKernelID
	{
		get
		{
			return rendererKernelID;
		}
	}

	/*****************************************************************************
	 *								 UTILITY METHODS
	 *****************************************************************************/
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
			//currentVolume.updateMaterialPropVector3("_ClippingPlanePosition", localClippingPlanePosition, i);;
		}

		// Update the clipping plane's normal for all bricks 
		//currentVolume.updateMaterialPropVector3All("_ClippingPlaneNormal", clippingPlane.Normal);

		// Update the clipping plane's enabled status for all bricks
		//currentVolume.updateMaterialPropIntAll("_ClippingPlaneEnabled", Convert.ToInt32(clippingPlane.Enabled));
	}

	/*****************************************************************************
	* COMPUTE SHADER RENDERING METHODS
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
		MetaBrick[] metaBricks_2 = new MetaBrick[4];

		// Initialize the volumeBuffer (the metadata buffer)
		MetaVolume[] metaVolumes = initMetaVolumeBuffer();//This only ever seems to hold 1 single volume. What's the point of using a buffer to contain a single object?
    

		// Initialize the data buffers
		initDataBuffers(metaBricks);

		// Set the necessary parameters in the compute shader
		metaBrickBuffer = new ComputeBuffer(metaBricks.Length, 64);       // n MetaBricks with a size of 64 bytes each.
        //This looks to be correct. Note that the MetaBrick is 64 bytes *in memory* use. This is not the dimension of the brick. 
               
		metaBrickBuffer.SetData(metaBricks);

        /*
        metaBrickBuffer.GetData(metaBricks_2);

        for (int i = 0; i < metaBricks.Length; i++)
        {
            Debug.Log("These are the metaBrick z-levels " + metaBricks_2[i].currentZLevel);

            metaBricks_2[i].currentZLevel = i+3;

            Debug.Log("These are the metaBrick z-levels " + metaBricks_2[i].currentZLevel);

        }

        metaBrickBuffer.SetData(metaBricks_2);*/

        metaVolumeBuffer = new ComputeBuffer(1, 64);                      // 1 MetaVolume with a size of 64 bytes each
		metaVolumeBuffer.SetData(metaVolumes);

		renderingShader.SetBuffer(rendererKernelID, "_MetaBrickBuffer", metaBrickBuffer);
		renderingShader.SetBuffer(rendererKernelID, "_MetaVolumeBuffer", metaVolumeBuffer);

		// Set other rendering properties
		renderingShader.SetInt("_Steps", 128);
		renderingShader.SetFloat("_NormPerRay", 1.0f);
	}

	/// <summary>
	/// Create the array of brick metadata for use in the compute shader based on the bricks in the currentVolume.
	/// </summary>
	/// <returns></returns>
	private MetaBrick[] initMetaBrickBuffer()
	{
        MetaBrick[] metaBricks = new MetaBrick[currentVolume.Bricks.Length];//Make an array of MetaBricks. 
       // MetaBrick[] metaBricks = new MetaBrick[1];//Make an array of MetaBricks. 

        //Debug.Log("This is the length of the metaBricks array: " + metaBricks.Length);//The length seems to be correct. 



       for (int i = 0; i < metaBricks.Length; i++)
        {
			// Create the MetaBrick
			metaBricks[i] = currentVolume.Bricks[i].getMetaBrick();

            //metaBricks[i].currentZLevel = i==0 ? metaBricks[i].maxZLevel : 0 ;//  
            metaBricks[i].currentZLevel =  metaBricks[i].maxZLevel;//    
            // metaBricks[i].currentZLevel = ((i * 23) + 5) % 9; //metaBricks[i].maxZLevel; 
            //Debug.Log("HZ Level for this brick was " + ((i * 23)+5) % 9);

            metaBricks[i].id = i;
			metaBricks[i].lastBitMask = currentVolume.Bricks[i].calculateLastBitMask();

			// Set some parameters in the brick
			currentVolume.Bricks[i].CurrentZLevel = metaBricks[i].currentZLevel;
		}
        //       
        /*
        metaBricks[0] = currentVolume.Bricks[0].getMetaBrick();
        metaBricks[0].currentZLevel = 8;
        metaBricks[0].id = 0;

        metaBricks[0].lastBitMask = currentVolume.Bricks[0].calculateLastBitMask();

        currentVolume.Bricks[0].CurrentZLevel = metaBricks[0].currentZLevel;//*/

        return metaBricks;
	}

	/// <summary>
	/// Determine the amount of data voxels to be read in for the given brick.
	/// </summary>
	/// <param name="br"></param>
	/// <returns></returns>
	private int getMetaBrickDataSize(MetaBrick br)
	{
		int numData = 1 << br.currentZLevel;//This yields 2^br.currentZLevel


       //Debug.Log("The number of data points is as follows: " + numData * numData * numData);

        return (numData * numData * numData);
        

	}

	/// <summary>
	/// Create the "array" of volume metadata for use in the compute shader.
	/// </summary>
	private MetaVolume[] initMetaVolumeBuffer()
	{
		MetaVolume[] metaVolume = new MetaVolume[1];//Why do we need a MetaVolume array of a single element? What's the point of using an array of a single element?
		metaVolume[0] = currentVolume.getMetaVolume();

       // metaVolume[0].scale = new Vector3(1.7f, 1.1f, 0.5f); 

       // Debug.Log("The MetaVolume Scale has been hard coded as  " + metaVolume[0].scale); //Has zero effect it seems. 



		return metaVolume;
	}

	/// <summary>
	/// Sends data buffers initialized with the volume data to the compute shader. 
	/// </summary>
	/// <param name="mb"></param>
	private void initDataBuffers(MetaBrick[] mb)
	{
		// Ensure that extra compute buffers are not unnecessarily initialized. Essentially for use when the number of bricks is ONE. 
		if (currentVolume.Bricks.Length < numberOfBuffers)
        {
            numberOfBuffers = currentVolume.Bricks.Length;
            //Debug.Log("Did we enter this IF statement?");
        }

        //Debug.Log("Number of data buffers is 2? " + numberOfBuffers);
        dataBuffers = new ComputeBuffer[maxNumberOfBuffers];	// The compute buffers that will hold the data. Instantiated on line 49 in the class data. 
        //For some reason the max number of buffers is three. Seems to just be a magic number. 

		int[] bufferDataSizes = new int[maxNumberOfBuffers];	//This will hold the total size of the data to be put in the ith buffer


		// Initialize the lists of bricks associated with each buffer
		Dictionary<int, List<int>> bricksInBuffer = new Dictionary<int, List<int>>();
        // The key is the buffer number (0,1,2) and the value is a List<int> containing the indices of the bricks in that given buffer.


		for (int i = 0; i < numberOfBuffers; i++)
		{
			bricksInBuffer.Add(i, new List<int>());
		}

		// Pre-compute the sizes of the compute buffers and which bricks will go inside of each
		for (int i = 0; i < mb.Length; i++)
		{
			int currentBrickSize = getMetaBrickDataSize(mb[i]);
            // Get the amount of data points in the current brick	

            //Debug.Log("For MetaBrick " + i + "  the boxMin/boxMax is  " + mb[i].boxMin.ToString("G4") + " and " + mb[i].boxMax.ToString("G4"));
            

            //8 Bits seems to be the usual. 
            currentBrickSize = currentVolume.BitsPerPixel == 8 ? (int) Math.Ceiling(currentBrickSize / 4.0) : (int) Math.Ceiling(currentBrickSize / 2.0);		
            // Account for any padding when packing the bytes into uints			
           


			bufferDataSizes[i % numberOfBuffers] += currentBrickSize;	// Ensure there is room for the ith brick in this buffer

            //Debug.Log("bufferDataSizes for " + i + " is " + bufferDataSizes[i % numberOfBuffers]); 

			bricksInBuffer[i % numberOfBuffers].Add(i);			        // Note the index of the brick to be added to the buffer
		}

        //Debug.Log("This is the breakdown of buffer allocation " + bufferDataSizes);

        /*foreach (int v in bufferDataSizes)
        {
            Debug.Log("This is in the buffer data sizes array: " + v);

        }*/


       
        // Allocate the compute buffers, read in the data, copy the data into the buffer, and send the data to the compute shader
        for (int bufIdx = 0; bufIdx < numberOfBuffers; bufIdx++)//This is a nested bunch of loops. 
		{
			dataBuffers[bufIdx] = new ComputeBuffer(bufferDataSizes[bufIdx], sizeof(uint));
			int currentBufferOffset = 0;

			uint[] rawData = new uint[bufferDataSizes[bufIdx]]; 
			for (int j = 0; j < bricksInBuffer[bufIdx].Count; j++)
			{
				int brIdx = bricksInBuffer[bufIdx][j];

				// Read the data and load it into the data buffer
				uint[] newData;
				if (currentVolume.BitsPerPixel == 8)
				{
					newData = currentVolume.Bricks[brIdx].readRaw8Into3DZLevelBufferUint();
                    //The function readRaw8Into3DZLevelBufferUint() packs all the data for the brick into an array of uints.
                    //How does this employ HZ order? I guess it only reads as far into the HZ file as the current z-level dictates.
                    //So to increase the HZ level, maybe we just read in the rest of the file up to where we need to? Seems to be the idea. 
                    // newData holds the raw data only for a single brick. This newData is then packed into the rawData buffer 
                }
                else
				{
					newData = currentVolume.Bricks[brIdx].readRaw16Into3DZLevelBufferUint();
				}

				// Copy the packed new data to the data buffer													// TODO: Change this loop copy to Buffer.BlockCopy() for faster memory copying
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
            //dataBuffers[1].SetData(rawData);

            // Send the data in the compute buffer to the compute shader
            renderingShader.SetBuffer(rendererKernelID, dataBufferNames[bufIdx], dataBuffers[bufIdx]);
            //When we send this data, does this overwrite any scale that we may have been trying to enforce? In other words, does the HZ curve data also contain relative positioning?

            //renderingShader.SetBuffer(rendererKernelID, dataBufferNames[1], dataBuffers[1]);
        }

		// Initialize any extra buffers to contain 1 single element.
        // Why would we need extra buffers?
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
            { _target.Release(); }

			// Get a render target for Ray Tracing
			_target = new CustomRenderTexture(Screen.width, Screen.height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			_target.material = renderMaterial;
			_target.enableRandomWrite = true;
			_target.Create();
		}
	}

    /*
     A method used by the VolumeController object in the GeneralControlsHandler to update the MetaBrickBuffer with a new Z-level setting. 
         */
    public void updateMetaBrickBuffer(int newZlevel)
    {
        MetaBrick[] mbBuff = new MetaBrick[this.CurrentVolume.Bricks.Length]; 

        metaBrickBuffer.GetData(mbBuff); 

        for (int i = 0; i < mbBuff.Length; i++)
        {


            // Update the actual Brick objects in the volume. 
            currentVolume.Bricks[i].CurrentZLevel = newZlevel;

            //Update the associated MetaBrick. We run the update through the actual brick first to enforce clamping to maximal level.
            //If you directly assign the MetaBrick the newZlevel, then you can overrun the available z levels when you have bricks of different sizes. 
            //By passing the newZlevel through the actual brick first, we clamp the z level of the metaBrick to at most the maxZLevel. 
            mbBuff[i].currentZLevel = currentVolume.Bricks[i].CurrentZLevel;//    
            

        }

        metaBrickBuffer.SetData(mbBuff);


    }

	/// <summary>
	/// A method called by Unity after all rendering is complete. Allows for post-processing effects.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="destination"></param>
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
      //  Debug.Log("How often are we re-rendering?");
		setShaderParameters();
		render(destination);
	}

	/// <summary>
	/// Sets some shader parameters needed for performing ray marching.
	/// </summary>
	private void setShaderParameters()
	{
		// Set the necessary parameters in the compute shader
		renderingShader.SetMatrix("_CameraToWorld", mainCamera.cameraToWorldMatrix);

        //Debug.Log(mainCamera.cameraToWorldMatrix); 

		renderingShader.SetMatrix("_CameraInverseProjection", mainCamera.projectionMatrix.inverse);

        //Debug.Log(mainCamera.projectionMatrix.inverse);
    }

	/// <summary>
	/// Calls the rendering kernel and outputs the results to the screen.
	/// </summary>
	/// <param name="destination"></param>
	private void render(RenderTexture destination)
	{

        //Debug.Log("How often are we re-rendering?");

        // Initialize the render texture
        initRenderTexture();

		// Call the compute shader 
		renderingShader.SetTexture(rendererKernelID, "Result", _target);
		int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
		int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
        

        //Debug.Log("Trying to locate any eror in the render shader: " + renderingShader.FindKernel("CSMain"));

        renderingShader.Dispatch(rendererKernelID, threadGroupsX, threadGroupsY, 1);

        
		// Blit the results to the screen and finalize the render
		Graphics.Blit(_target, destination);
	}
}