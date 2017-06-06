// Written by stermj

using System.IO;
using UnityEngine;

public class RAWFileMapper : MonoBehaviour {

    public int width = 256;      // X dimension
    public int height = 256;     // Y dimension
    public int depth = 256;      // Z dimension

    private string path = "Assets/Data/";
    public string filename = "VisMale";
    public string extension = ".raw";

    public float aspectX = 1.00797f;
    public float aspectY = 0.995861f;
    public float aspectZ = 1.57774f;

	// Use this for initialization
	void Start () {
        FileStream volumeDataFile = new FileStream(path + filename + extension, FileMode.Open);
        load8BitRawFile(volumeDataFile);
	}
	
	// Update is called once per frame
	void Update () {
        transform.localScale = new Vector3(aspectX, aspectY, aspectZ);
	}

    // Loads the given 8-bit .raw file into a Texture3D data structure
    private void load8BitRawFile(FileStream file)
    {
        // Read the raw file into a buffer of bytes
        BinaryReader reader = new BinaryReader(file);
        byte[] buffer = new byte[width * height * depth];
        int size = sizeof(byte);
        reader.Read(buffer, 0, size * buffer.Length);
        reader.Close();

        // Scale the scalar values to [0, 1]
        Color[] scalars;
        scalars = new Color[buffer.Length];
        for(int i = 0; i < buffer.Length; i++)
        {
            scalars[i] = new Color(0, 0, 0, ((float)buffer[i] / byte.MaxValue));
        }

        // Put the intensity scalar values into the Texture3D
        Texture3D data = new Texture3D(width, height, depth, TextureFormat.Alpha8, false);
        data.SetPixels(scalars);
        data.Apply();

        // Send the intensity scalar values to the shader
        Renderer rend = GetComponent<Renderer>();
        rend.material.SetTexture("_VolumeDataTexture", data);
    }
}
