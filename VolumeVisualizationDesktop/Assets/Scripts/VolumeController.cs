/* Volume Controller | Marko Sterbentz 07/03/2017
 *                   | Randall Reese   08/27/2018 */

using System;
using UnityEngine;
using System.Collections.Generic;
using Apache.NMS;
using Newtonsoft.Json;

/// <summary>
/// This class is the medium between the data and the volume rendering, and handles requests between the two.
/// </summary>
public class VolumeController : MonoBehaviour {

	private Volume currentVolume;                       // The data volume that will be rendered
	private TransferFunction transferFunction;          // Transfer function for modifying visuals
	private ClippingPlane clippingPlane;                // Clipping plane for modifying viewable portion of the volume
	public Camera mainCamera;                           // Main camera in the scene		
    public SievasController controller;
    private System.Random rand = new System.Random();//Must explicitly say System.Random when working with Unity. Unity has a "UnityEngine.Random()" class that does not do rand numbers. 
    private bool bricksPacked = false;
    private bool bricksRequested = false; 


    [Tooltip("The path to the data relative to the base directory of the Unity project.")]
	public string dataPath = "Assets/Data/BoxHz/";      // The path to the data to be loaded into the renderer. This is currently overridden. 
    public string volumeName = "VisMale";// Defaults to VisMale. 
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
    public int numberOfBuffers = 5;//This is controlled in Unity (Main Camera > Volume Controller > Number Of Buffers. Whatever is here is overridden by what is in Unity.   

	private int maxNumberOfBuffers = 18;//

	private ComputeBuffer[] dataBuffers;//An array of buffers. 

    //Note that every data buffer that we want to use must be HAND INITIALIZED in the RenderingShader.compute code. See line 61 or so there.
    //Also see samplePackedBuffers() function there around line 231. 
    //Note as well that Unity currently only allows 8 unordered access view (uav) objects.
    //This means that we can have a TOTAL of 8 computeBuffers + textures. Because we need a texture and two utility buffers (metaBrick, metaVolume), we only can have five data buffers.
    //With the hope that at some point this contraint will be removed, I have added some frame code for future implementation. 
    private Dictionary<int, string> dataBufferNames = new Dictionary<int, string> {
		{ 0, "_DataBufferZero"},
		{ 1, "_DataBufferOne" },
		{ 2, "_DataBufferTwo" },
        { 3, "_DataBufferThree" },
        { 4, "_DataBufferFour" },
        { 5, "_DataBufferFive"},
        { 6, "_DataBufferSix" },
        { 7, "_DataBufferSeven" },
        { 8, "_DataBufferEight" },
        { 9, "_DataBufferNine" }, 
        { 10, "_DataBufferTen"},
        { 11, "_DataBufferEleven" },
        { 12, "_DataBufferTwelve" },
        { 13, "_DataBufferThirteen" },
        { 14, "_DataBufferFourteen" },
        { 15, "_DataBufferFifteen"},
        { 16, "_DataBufferSixteen" },
        { 17, "_DataBufferSeventeen" }
       
    };

	/// <summary>
	/// Initialization function for the VolumeController. Ensures that the global variables needed by other objects are initialized first.
	/// </summary>
	private void Awake()
	{
		// 0. Create the data volume and its rendering material. This initialized the volume. 
		currentVolume = new Volume(dataPath+volumeName+"/", "metadata.json");
                
        controller.VolName = volumeName; 


        // 1. Set up the transfer function
        int isovalueRange = currentVolume.calculateIsovalueRange();
		transferFunction = new TransferFunction(isovalueRange);

		// 2. Set up the clipping plane
		clippingPlane = new ClippingPlane(new Vector3(0, 0, 0), new Vector3(1, 0, 0), false);
		updateClippingPlaneAll();

		// 3. Set up the target for the camera controls
		CameraControls cameraControls = (CameraControls)mainCamera.GetComponent(typeof(CameraControls));
		cameraControls.target = currentVolume.VolumeCube;

        //4. Initialize data packing. 
        // Moved to update(). 


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
        if (!bricksPacked && controller.SievasInitialized)
        {
            print("Initializing the brick requests");

            if(!bricksRequested)
            {
                for (int b = 0; b < currentVolume.Bricks.Length; b++)
                {
                    IMessageProducer controlProducer = controller.GetComponentsInChildren<SievasController>()[0].controlMsgProducer;

                    if (controlProducer != null)
                    {
                        ITextMessage dataMsg = controlProducer.CreateTextMessage("We are requesting a brick in btnDvr in the function onClickPlay()");
                        dataMsg.Properties.SetBool("BrickRequest", true);
                        
                        dataMsg.Properties.SetInt("BrickNum", b);
                        
                        dataMsg.Properties.SetInt("zLevel", CurrentVolume.Bricks[b].MaxZLevel);

                        print("We are sending a data request for z-level " + CurrentVolume.Bricks[b].MaxZLevel);

                        controlProducer.Send(dataMsg);

                    }
                    else
                    {
                        print("The control Producer seems to be null. ");
                    }

                    bricksRequested = true;

                }
            }
            

            if (controller.ByteDataDict.Count == CurrentVolume.Bricks.Length)
            {
                print("Initializing the brick packing of " + controller.ByteDataDict.Count  + " bricks");
                initializeComputeRenderer();
            }
                
        }
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
	
		// Initialize the volumeBuffer (the metadata buffer)
		MetaVolume[] metaVolumes = initMetaVolumeBuffer();
        //This only ever seems to hold 1 single volume. What's the point of using a buffer to contain a single object?
        //Everything must be wrapped in a buffer if it is to be passed to the shader. Sort of bulky, but that is how it is. 

		// Initialize the data buffers with the RAW data. 
		initDataBuffers(metaBricks);

		// Set the necessary parameters in the compute shader
		metaBrickBuffer = new ComputeBuffer(metaBricks.Length, 64);       
        // n MetaBricks with a size of 64 bytes each.
        //Note that the MetaBrick is 64 bytes *in memory* use. This is not the dimension of the brick. 

        
        metaBrickBuffer.SetData(metaBricks);

        updateMetaBrickBuffer(1); //This is done to enforce a minimal loading (not packing, however) of the data on initial render. 

        

        metaVolumeBuffer = new ComputeBuffer(1, 64);// 1 MetaVolume with a size of 64 bytes each
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
       

       for (int i = 0; i < metaBricks.Length; i++)//This controls what Z level is loaded INTO THE BUFFERS. 
        {
			// Create the MetaBrick
			metaBricks[i] = currentVolume.Bricks[i].getMetaBrick();

            metaBricks[i].currentZLevel =  Math.Min(metaBricks[i].maxZLevel, 8);
            // This is done in order to pack GrayRot into the buffers at a maximum of level 8. z=9 overflows the buffer.    
            // Much has been tried to load the full GrayRot data (~25 GB), but we run out of buffer space. A buffer can be a max of 2 GB under normal operation.
            // We are limited to five buffers of data. Thus we can load a MAXIMUM of 10 GB of data. 
            // As of 12/19/2018 I do not know of a way around this.
            // See my thoughts in the white paper writeup  as to why these constraints are difficult to overcome. 
            
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
		int numData = 1 << br.currentZLevel;//This yields 2^br.currentZLevel

        return (numData * numData * numData);
        

	}

	/// <summary>
	/// Create the "array" of volume metadata for use in the compute shader.
	/// </summary>
	private MetaVolume[] initMetaVolumeBuffer()
	{
		MetaVolume[] metaVolume = new MetaVolume[1];
        //Why do we need a MetaVolume array of a single element? What's the point of using an array of a single element? This is done in order to pass it to the shader. EVERYTHING datawise needs to be in a buffer. 
		metaVolume[0] = currentVolume.getMetaVolume();

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
            
        }


        dataBuffers = new ComputeBuffer[maxNumberOfBuffers];

		int[] bufferDataSizes = new int[maxNumberOfBuffers];	//This will hold the total size of the data to be put in the ith buffer


		// Initialize the lists of bricks associated with each buffer
		Dictionary<int, List<int>> bricksInBuffer = new Dictionary<int, List<int>>();
        // The key is the buffer number (0,1,2, etc) and the value is a List<int> containing the indices of the bricks in that given buffer.


		for (int i = 0; i < numberOfBuffers; i++)
		{
			bricksInBuffer.Add(i, new List<int>());
		}

		// Pre-compute the sizes of the compute buffers and which bricks will go inside of each
		for (int i = 0; i < mb.Length; i++)
		{
			int currentBrickSize = getMetaBrickDataSize(mb[i]);
            // Get the amount of data points in the current brick	

            //8 Bits seems to be the usual. 
            currentBrickSize = currentVolume.BitsPerPixel == 8 ? (int) Math.Ceiling(currentBrickSize / 4.0) : (int) Math.Ceiling(currentBrickSize / 2.0);		
            // Account for any padding when packing the bytes into uints			
           


			bufferDataSizes[i % numberOfBuffers] += currentBrickSize;	// Ensure there is room for the ith brick in this buffer

			bricksInBuffer[i % numberOfBuffers].Add(i);			        // Note the index of the brick to be added to the buffer
		}

              
        // Allocate the compute buffers, read in the data, copy the data into the buffer, and send the data to the compute shader
        for (int bufIdx = 0; bufIdx < numberOfBuffers; bufIdx++)//This is a nested bunch of loops. 
		{
			dataBuffers[bufIdx] = new ComputeBuffer(bufferDataSizes[bufIdx], sizeof(uint));
            // Could the stride (second argument) be made into the size of a short? It could cut the necessary buffer size in half. 
            //Shaders do not have a "short" intrinsic type. Must use uints. 
			int currentBufferOffset = 0;

			uint[] rawData = new uint[bufferDataSizes[bufIdx]]; 
			for (int j = 0; j < bricksInBuffer[bufIdx].Count; j++)
			{
				int brIdx = bricksInBuffer[bufIdx][j];

				// Read the data and load it into the data buffer
				uint[] newData;
				if (currentVolume.BitsPerPixel == 8)
				{
					 
                    newData = pack8BitHZbyteArrayToUint(controller.ByteDataDict[brIdx]); 

                }
                /* 
                                                                                
                                                                                
TTTTTTTTTTTTTTTTTTTTTTT                   DDDDDDDDDDDDD                         
T:::::::::::::::::::::T                   D::::::::::::DDD                      
T:::::::::::::::::::::T                   D:::::::::::::::DD                    
T:::::TT:::::::TT:::::T                   DDD:::::DDDDD:::::D                   
TTTTTT  T:::::T  TTTTTTooooooooooo          D:::::D    D:::::D    ooooooooooo   
        T:::::T      oo:::::::::::oo        D:::::D     D:::::D oo:::::::::::oo 
        T:::::T     o:::::::::::::::o       D:::::D     D:::::Do:::::::::::::::o
        T:::::T     o:::::ooooo:::::o       D:::::D     D:::::Do:::::ooooo:::::o
        T:::::T     o::::o     o::::o       D:::::D     D:::::Do::::o     o::::o
        T:::::T     o::::o     o::::o       D:::::D     D:::::Do::::o     o::::o
        T:::::T     o::::o     o::::o       D:::::D     D:::::Do::::o     o::::o
        T:::::T     o::::o     o::::o       D:::::D    D:::::D o::::o     o::::o
      TT:::::::TT   o:::::ooooo:::::o     DDD:::::DDDDD:::::D  o:::::ooooo:::::o
      T:::::::::T   o:::::::::::::::o     D:::::::::::::::DD   o:::::::::::::::o
      T:::::::::T    oo:::::::::::oo      D::::::::::::DDD      oo:::::::::::oo 
      TTTTTTTTTTT      ooooooooooo        DDDDDDDDDDDDD           ooooooooooo   
                                                                                
                                                                 */


                else//Implment for 16 bit. 
                {
                    //print("Streaming 16-bit data is not yet implemented"); 
                    //newData = currentVolume.Bricks[brIdx].readRaw16Into3DZLevelBufferUint();

                    newData = pack8BitHZbyteArrayToUint(controller.ByteDataDict[brIdx]);
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

        bricksPacked = true;
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
     Note that this does NOT update the actual raw data buffer. 
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
	/// A method for packing an 8 bit byte array into a uint array. 
	/// </summary>
	/// <param name="hzb"> The byte array that is to be packed into a unit array.</param>
	
    public uint[] pack8BitHZbyteArrayToUint(byte[] hzb)
    {
        int totalDataSize = hzb.Length;
        
        if ((totalDataSize % 4) > 0)
        {
            Debug.Log("The total data size is not divisible by 4.");
        }

        uint[] uintBuffer = new uint[Mathf.CeilToInt(totalDataSize / 4.0f)];
        //Why divide by 4?
        // A uint is four bytes on a standard architechture. Is this why we divide the totalDataSize by 4?
        //I believe that this is why we divide by 4. Since each uint can hold 4 bytes, if we have a byte array (which "buffer" is), then dividing the length of the byte array
        //    by 4 tells us how many uints it would take (i.e. how long of a uint array we need) to hold the byte data.

        //Here we take a byte array and pack it into a uint array.

        /*     Byte array         |  0  |  1  |  2  |  3  |  4  |  5 |  6 |  7  |  8  |  9  |
            *  Uint array         |  0                    |  1                  |  2        **********|  
            * 
            * Is there just garbage being shoved into the uint array if the data has a nuber of bytes not perfectly divisible by 4?
            * I would seem that when the uint array is intialized, there is just garbage bits shoved into it. Whatever garabage is sitting in memory. 
            * Note however that the total data size is actually divisible by 8 (at the very least) as long as we are rendering at HZ-level of at least 1.
            * dataSize = 2^currentZLevel. totalDataSize is dataSize^3. Hence totalDataSize will be at least divisible by 8 as long as z > 0.  
            * 
            * Now, the question does still remain: Why even do this odd byte packing into uints in the first place? ANSWER: The shader does not have a "byte" instrinsic type. 
            * 
            * */


            int[] byteShifts = { 24, 16, 8, 0 };    // use for big endian
            //int[] byteShifts = { 0, 8, 16, 24 };    // use for little endian
            int maskIndex = 0;//This keeps track of what uint we should be packing into. 
            for (int i = 0; i < hzb.Length; i++)
            {
                uint val = hzb[i];//Gets the ith byte from the byte array we were given. 

                uintBuffer[i / 4] = uintBuffer[i / 4] | (val << byteShifts[maskIndex]);

                maskIndex = (maskIndex + 1) % 4;
            }

            return uintBuffer;
       
    }

    /// <summary>
    /// A method for packing an 16 bit byte array into a uint array. 
    /// </summary>
    /// <param name="hzb"> The byte array that is to be packed into a unit array.</param>

    public uint[] pack16BitHZbyteArrayToUint(byte[] hzb)//Note that this has only been tested in a very limited scope. 
    {
        int totalDataSize = hzb.Length;//We do not need to calculate this based on the z-level like they do in the Brick class.
        //We can just directly take the measurement of the data size. 

        if ((totalDataSize % 4) > 0)
        {
            Debug.Log("The total data size is not divisible by 4.");
        }

        // Convert the bytes to ushort (Note: Buffer.BlockCopy() assumes data is in little endian format).
        // TODO: Figure out a better way to read in ushort rather than doing this... This may not be available on non-Windows platforms?
        ushort[] ushortBuffer = new ushort[hzb.Length / 2];
        Buffer.BlockCopy(hzb, 0, ushortBuffer, 0, hzb.Length);

        // Pack the ushorts into a uint array
        uint[] uintBuffer = new uint[Mathf.CeilToInt(totalDataSize / 2.0f)];
        int[] byteShifts = { 16, 0};
        int maskIndex = 0;
        for (int i = 0; i < ushortBuffer.Length; i++)
        {
            uint val = ushortBuffer[i];
            uintBuffer[i / 2] = uintBuffer[i / 2] | (val << byteShifts[maskIndex]);
            maskIndex = (maskIndex + 1) % 2;
        }

        return uintBuffer;

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

        if(bricksPacked)
        {
            render(destination);
        }
        
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
        
        renderingShader.Dispatch(rendererKernelID, threadGroupsX, threadGroupsY, 1);

		// Blit the results to the screen and finalize the render
		Graphics.Blit(_target, destination);
	}
}

