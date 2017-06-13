using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HZOrderReader : MonoBehaviour {

    public int width = 256;      // X dimension
    public int height = 256;     // Y dimension
    public int depth = 256;      // Z dimension

    private string path = "Assets/Data/";
    public string filename = "skull";
    public string extension = ".hz";

    //private ushort[] shortData;

    // Use this for initialization
    void Start () {
        readHZOrderRaw8();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void readHZOrderRaw8()
    {
        // Open the file
        FileStream dataFile = new FileStream(path + filename + extension, FileMode.Open);

        // Read in the bytes
        BinaryReader reader = new BinaryReader(dataFile);
        byte[] buffer = new byte[width * height * depth];
        int size = sizeof(byte);
        reader.Read(buffer, 0, size * buffer.Length);
        reader.Close();

        // Load bytes into a texture and upload to GPU
        //Texture2D dataTexture = new Texture2D(width * depth * height, 1, TextureFormat.Alpha8, false);
        Texture2D dataTexture = new Texture2D(4096, 4096, TextureFormat.Alpha8, false);         // 4096 * 4096 == 256 * 256 * 256
        buffer[0] = 0xFF;
        dataTexture.LoadRawTextureData(buffer);
        dataTexture.Apply();

        // Assign this data to a texture so it can be used in the shader
        GetComponent<Renderer>().material.mainTexture = dataTexture;

        Debug.Log("Finished reading in the hz file.");
    }

    //private void readHZOrderShort16()
    //{
    //    // Initialize the data buffer
    //    shortData = new ushort[width * height * depth];

    //    // Open the file
    //    FileStream dataFile = new FileStream(path + filename + extension, FileMode.Open);

    //    // Read in the bytes
    //    BinaryReader reader = new BinaryReader(dataFile);
    //    byte[] data = new byte[width * height * depth * 2];
    //    int size = sizeof(ushort);
    //    reader.Read(data, 0, size * shortData.Length);
    //    reader.Close();

    //    // Load bytes into a texture and upload to GPU
    //    Texture2D dataTexture = new Texture2D(width * height * depth, 1, TextureFormat.Alpha8, false);
    //    dataTexture.LoadRawTextureData(data);
    //    dataTexture.Apply();

    //    // Assign this data to a texture so it can be used in the shader
    //    GetComponent<Renderer>().material.mainTexture = dataTexture;      
    //}
}
