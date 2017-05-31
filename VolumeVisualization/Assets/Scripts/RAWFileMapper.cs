// Written by stermj

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RAWFileMapper : MonoBehaviour {

    private Texture3D data;

    private int width = 256;      // X dimension
    private int height = 256;     // Y dimension
    private int depth = 128;      // Z dimension

    private Color[] scalars;

    private Renderer rend;

	// Use this for initialization
	void Start () {
        FileStream volumeDataFile = new FileStream("Assets/Data/VisMale.raw", FileMode.Open);
        load8BitRawFile(volumeDataFile);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    // Loads the given 8-bit .raw file into a Texture3D data structure
    private void load8BitRawFile(FileStream file)
    {
        // Initialize the Texture3D of data
        data = new Texture3D(width, height, depth, TextureFormat.RGBA32, false);

        // Read the raw file into a buffer of bytes
        BinaryReader reader = new BinaryReader(file);
        byte[] buffer = new byte[width * height * depth];
        int size = sizeof(byte);
        reader.Read(buffer, 0, size * buffer.Length);
        reader.Close();
        
        // Scale the scalar values to [0, 1]
        scalars = new Color[buffer.Length];
        for(int i = 0; i < buffer.Length; i += 4)
        {
            scalars[i] = new Color(((float) buffer[i] / byte.MaxValue), 0, 0, 1);
        }

        // Put the intensity scalar values into the Texture3D
        data.SetPixels(scalars);
        data.Apply();

        // Send the intensity scalar values to the shader
        rend = GetComponent<Renderer>();
        rend.material.SetTexture("_VolumeDataTexture", data);

    }

}
