﻿using System;
using System.IO;
using UnityEngine;
using SimpleJSON;

/* Volume | Marko Sterbentz 8/2/2017
 * This class represents the volume to be visualized.
 */ 
public class Volume {

	/* Member variables */
	private Brick[] bricks;
	private int bitsPerPixel;
	private int bytesPerPixel;
	private int totalBricks;
	private int minLevel;
	private int maxLevel;
	private int[] globalSize;
	private string dataPath;
	private Vector3 position;
	private Material volumeMaterial;

	/* Properties */
	public Brick[] Bricks
	{
		get
		{
			return bricks;
		}
		set
		{
			bricks = value;
		}
	}
	public int BitsPerPixel
	{
		get
		{
			return bitsPerPixel;
		}
		set
		{
			bitsPerPixel = value;
		}
	}
	public int BytePerPixel
	{
		get
		{
			return bytesPerPixel;
		}
		set
		{
			bytesPerPixel = value;
		}
	}
	public int MinLevel
	{
		get
		{
			return minLevel;
		}
		set
		{
			minLevel = value;
		}
	}
	public int MaxLevel
	{
		get
		{
			return maxLevel;
		}
		set
		{
			maxLevel = value;
		}
	}
	public int[] GlobalSize
	{
		get
		{
			return globalSize;
		}
		set
		{
			globalSize = value;
		}
	}
	public string DataPath
	{
		get
		{
			return dataPath;
		}
		set
		{
			dataPath = value;
		}
	}
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
	public Material VolumeMaterial
	{
		get
		{
			return volumeMaterial;
		}
		set
		{
			volumeMaterial = value;
		}
	}

	/* Constructors */
	public Volume()
	{

	}

	public Volume(string _dataPath, string _metadataFileName, Material _volumeMaterial)
	{
		// Set the default position of the volume
		Position = new Vector3(0.5f, 0.5f, 0.5f);

		// Set the volume material
		VolumeMaterial = _volumeMaterial;

		// Load the volume from the JSOn metadata file
		loadVolume(_dataPath, _metadataFileName);
	}

	/* Methods */
	public void loadVolume(string _dataPath, string metadataFileName)
	{
		try
		{
			// Set the datapath
			DataPath = _dataPath;

			// Read the JSON file specified by the path
			StreamReader reader = new StreamReader(dataPath + metadataFileName);
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
				// Read in metadata from the file
				string newBrickFilename = DataPath + N["bricks"][i]["filename"];
				int newBrickSize = N["bricks"][i]["size"];
				Vector3 newBrickPosition = new Vector3(N["bricks"][i]["position"][0].AsInt,
													   N["bricks"][i]["position"][1].AsInt,
													   N["bricks"][i]["position"][2].AsInt);

				// Generate boundingVolumeCorner and boundingVolumeCenter in world space
				float maxGlobalSize = Mathf.Max(globalSize);
				Vector3 boundingVolumeCenter = new Vector3(maxGlobalSize, maxGlobalSize, maxGlobalSize) / 2.0f;
				Vector3 boundingVolumeCorner = new Vector3(0, 0, 0);
				Vector3 b = boundingVolumeCenter - boundingVolumeCorner;

				// Find volumeCenter and volumeCorner of the data in world space
				Vector3 volumeCenter = new Vector3(globalSize[0], globalSize[1], globalSize[2]) / 2.0f;
				Vector3 volumeCorner = new Vector3(0, 0, 0);
				Vector3 c = volumeCenter - volumeCorner;

				// Calculate vector a: the bottom left corner of the volume in voxel space
				Vector3 volumeCornerVoxelSpace = b - c;

				// Calculate the position for each brick in voxel space
				Vector3 brickPositionVoxelSpace = volumeCornerVoxelSpace + newBrickPosition;

				// Calculate the brick offset
				Vector3 brickOffsetVoxelSpace = new Vector3(newBrickSize, newBrickSize, newBrickSize) / 2.0f;

				// Calculate final position for the brick
				Vector3 finalBrickPosition = (brickPositionVoxelSpace + brickOffsetVoxelSpace) / maxGlobalSize;

				// Create the brick
				bricks[i] = new Brick(newBrickFilename, newBrickSize, finalBrickPosition, volumeMaterial);

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
	public int calculateIsovalueRange()
	{
		return ((int)Mathf.Pow(2.0f, bitsPerPixel)) - 1;
	}

	/*****************************************************************************
	 * SHADER BUFFERING METHODS
	 *****************************************************************************/
	public void updateMaterialPropFloat(string propName, float val, int brickIndex)
	{
		Bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetFloat(propName, val);
	}

	public void updateMaterialPropFloatAll(string propName, float val)
	{
		// Update the volumeMaterial
		VolumeMaterial.SetFloat(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < Bricks.Length; i++)
		{
			Bricks[i].getGameObject().GetComponent<Renderer>().material.SetFloat(propName, val);
		}
	}

	public void updateMaterialPropFloat3(string propName, float[] val, int brickIndex)
	{
		Bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetFloatArray(propName, val);
	}

	public void updateMaterialPropFloat3All(string propName, float[] val)
	{
		// Update the volumeMaterial
		volumeMaterial.SetFloatArray(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < Bricks.Length; i++)
		{
			Bricks[i].getGameObject().GetComponent<Renderer>().material.SetFloatArray(propName, val);
		}
	}

	public void updateMaterialPropInt(string propName, int val, int brickIndex)
	{
		Bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetInt(propName, val);
	}

	public void updateMaterialPropIntAll(string propName, int val)
	{
		// Update the volumeMaterial
		VolumeMaterial.SetInt(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < Bricks.Length; i++)
		{
			Bricks[i].getGameObject().GetComponent<Renderer>().material.SetInt(propName, val);
		}
	}

	public void updateMaterialPropTexture2D(string propName, Texture2D tex, int brickIndex)
	{
		Bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
	}

	public void updateMaterialPropTexture2DAll(string propName, Texture2D tex)
	{
		// Update the volumeMaterial
		VolumeMaterial.SetTexture(propName, tex);

		// Update the materials in all of the bricks
		for (int i = 0; i < Bricks.Length; i++)
		{
			Bricks[i].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
		}
	}

	public void updateMaterialPropTexture3D(string propName, Texture3D tex, int brickIndex)
	{
		Bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
	}

	public void updateMaterialPropTexture3DAll(string propName, Texture3D tex)
	{
		// Update the volumeMaterial
		VolumeMaterial.SetTexture(propName, tex);

		// Update the materials in all of the bricks
		for (int i = 0; i < Bricks.Length; i++)
		{
			Bricks[i].getGameObject().GetComponent<Renderer>().material.SetTexture(propName, tex);
		}
	}

	public void updateMaterialPropVector3(string propName, Vector3 val, int brickIndex)
	{
		Bricks[brickIndex].getGameObject().GetComponent<Renderer>().material.SetVector(propName, val);
	}

	public void updateMaterialPropVector3All(string propName, Vector3 val)
	{
		// Update the volumeMaterial
		VolumeMaterial.SetVector(propName, val);

		// Update the materials in all of the bricks
		for (int i = 0; i < bricks.Length; i++)
		{
			Bricks[i].getGameObject().GetComponent<Renderer>().material.SetVector(propName, val);
		}
	}
}
