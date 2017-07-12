/* HZ Order Reader | Marko Sterbentz 6/19/2017
 * This script reads in raw data into either a 2D or 3D texture.
 * Note: The way the data is interpreted must be handled in the shader.
 * Note: It also provides functionality for testing the HZ curving.
 */ 

using System;
using System.IO;
using UnityEngine;

public class HZOrderReader : MonoBehaviour {

    public int width = 128;      // X dimension
    public int height = 256;     // Y dimension
    public int depth = 256;      // Z dimension

    private string path = "Assets/Data/";
    public string filename = "curved_skull";
    public string extension = ".hz";

    // Use this for initialization
    void Start () {
        //readRaw8Into2D(path + filename + extension);
        //readRaw8Into3D(path + filename + extension);
    }

    // Update is called once per frame
    void Update () {
		
	}

    /********************************** FILE READING CODE **********************************/

    // Reads in the 8-bit .raw file in to a 2-dimensional texture.
    // Note: The 2D texture is called "_MainTex" on the shader.
    // Note: Works for hz-order and Cartesian. It is up to the shader to interpret the data correctly.
    private void readRaw8Into2D(string pathToData)
    {
        // Open the file
        FileStream dataFile = new FileStream(pathToData, FileMode.Open);

        // Read in the bytes
        BinaryReader reader = new BinaryReader(dataFile);
        byte[] buffer = new byte[width * height * depth];
        int size = sizeof(byte);
        reader.Read(buffer, 0, size * buffer.Length);
        reader.Close();

        // Load bytes into a texture and upload to GPU
        Texture2D dataTexture = new Texture2D(4096, 4096, TextureFormat.Alpha8, false);         // 4096 * 4096 == 256 * 256 * 256
        dataTexture.LoadRawTextureData(buffer);
        dataTexture.filterMode = FilterMode.Point;
        dataTexture.Apply();

        // Assign this data to a texture so it can be used in the shader
        GetComponent<Renderer>().material.mainTexture = dataTexture;

        Debug.Log("Finished reading in the hz file.");
    }

    // Reads in the 8-bit .raw file in to a 3-dimensional texture.
    // Note: The 3D texture is called "_VolumeDataTexture" on the shader.
    // Note: Works for hz-order and Cartesian. It is up to the shader to interpret the data correctly.
    private Texture3D readRaw8Into3D(string pathToData)
    {
        // Open the file
        FileStream dataFile = new FileStream(pathToData, FileMode.Open);

        // Read in the bytes
        BinaryReader reader = new BinaryReader(dataFile);
        byte[] buffer = new byte[width * height * depth];
        int size = sizeof(byte);
        reader.Read(buffer, 0, size * buffer.Length);
        reader.Close();

        // Scale the scalar values to [0, 1]
        Color[] scalars;
        scalars = new Color[buffer.Length];
        for (int i = 0; i < buffer.Length; i++)
        {
            scalars[i] = new Color(0, 0, 0, ((float)buffer[i] / byte.MaxValue));
        }

        // Put the intensity scalar values into the Texture3D
        Texture3D data = new Texture3D(width, height, depth, TextureFormat.Alpha8, false);
        //data.filterMode = FilterMode.Point;
        data.SetPixels(scalars);
        data.Apply();

        // Send the intensity scalar values to the shader
        Renderer rend = GetComponent<Renderer>();
        rend.material.SetTexture("_VolumeDataTexture", data);

        return data;
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

    /********************************** HZ CURVE TESTING CODE ************************************/
    private uint lastBitMask = 1 << 24;

    bool testHzCurving()
    {
        /* Test input to curve. */
        float[] foof = new float[3];
        foof[0] = 0.1f;
        foof[1] = 0.2f;
        foof[2] = 0.3f;

        /* Perform the curving. */
        uint zIndex = morton3Df(foof);              // Convert from texture coords to volume coords and get the z index
        uint hzIndex = getHZIndex(zIndex);
        uint[] fooAgain = decode(hzIndex);
        float[] foofAgain = toTexCoord(fooAgain);   // convert back to texture coordinates for use

        /* Check that the curving worked.*/
        if (floatsEqual(foofAgain[0], foof[0]) && floatsEqual(foofAgain[1], foof[1]) && floatsEqual(foofAgain[2], foof[2]))
        {
            Debug.Log("Hz Curve Successful.");
            return true;
        }
        else
        {
            Debug.Log("Hz Curve Failed.");
            return false;
        }
    }

    bool curveRaw8File(string inputFileLoc, string outputFileLoc)
    {
        Texture3D uncurvedData = readRaw8Into3D("Assets/Data/skull.raw");

        Color[] uncurvedColors = uncurvedData.GetPixels();
        byte[] curvedColorBytes = new byte[uncurvedColors.Length];

        uint[] pos = new uint[3];
        for (uint x = 0; x < 256; x++)
        {
            pos[0] = x;
            for (uint y = 0; y < 256; y++)
            {
                pos[1] = y;
                for (uint z = 0; z < 256; z++)
                {
                    pos[2] = z;
                    uint zIndex = morton3D(pos);
                    uint hzIndex = getHZIndex(zIndex);

                    curvedColorBytes[hzIndex] = (byte)(uncurvedColors[x + y * 256 + z * 256 * 256].a * 256.0f);
                }
            }
        }
        /* Write output to files. */
        ByteArrayToFile(@outputFileLoc, curvedColorBytes);
        return false;
    }

    uint expandBits(uint v)
    {
        v = (v * 0x00010001u) & 0xFF0000FFu;
        v = (v * 0x00000101u) & 0x0F00F00Fu;
        v = (v * 0x00000011u) & 0xC30C30C3u;
        v = (v * 0x00000005u) & 0x49249249u;
        return v;
    }

    public uint[] decode(uint c)
    {
        uint[] cartEquiv = new uint[3];
        c = c << 1 | 1;
        uint i = c | c >> 1;
        i |= i >> 2;
        i |= i >> 4;
        i |= i >> 8;
        i |= i >> 16;

        i -= i >> 1;

        c *= lastBitMask / i;
        c &= (~lastBitMask);
        cartEquiv[0] = DecodeMorton3X(c);
        cartEquiv[1] = DecodeMorton3Y(c);
        cartEquiv[2] = DecodeMorton3Z(c);

        return cartEquiv;
    }

    uint Compact1By2(uint x)
    {
        x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
        x = (x ^ (x >> 2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
        x = (x ^ (x >> 4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
        x = (x ^ (x >> 8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
        x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
        return x;
    }

    uint DecodeMorton3X(uint code)
    {
        return Compact1By2(code >> 2);
    }

    uint DecodeMorton3Y(uint code)
    {
        return Compact1By2(code >> 1);
    }

    uint DecodeMorton3Z(uint code)
    {
        return Compact1By2(code >> 0);
    }

    uint getHZIndex(uint zIndex)
    {
        int hzIndex = (int)(zIndex | lastBitMask);     // set leftmost one
        hzIndex /= hzIndex & -hzIndex;                 // remove trailing zeros
        return (uint)(hzIndex >> 1);                   // remove rightmost one
    }

    uint morton3Df(float[] pos)
    {
        // Quantized to the correct resolution
        pos[0] = Math.Min(Math.Max(pos[0] * 256.0f, 0.0f), 255.0f);
        pos[1] = Math.Min(Math.Max(pos[1] * 256.0f, 0.0f), 255.0f);
        pos[2] = Math.Min(Math.Max(pos[2] * 256.0f, 0.0f), 255.0f);

        // Interlace the bits
        uint xx = expandBits((uint)pos[0]);
        uint yy = expandBits((uint)pos[1]);
        uint zz = expandBits((uint)pos[2]);

        return xx << 2 | yy << 1 | zz;
    }

    uint morton3D(uint[] pos)
    {
        // Interlace the bits
        uint xx = expandBits(pos[0]);
        uint yy = expandBits(pos[1]);
        uint zz = expandBits(pos[2]);

        return xx << 2 | yy << 1 | zz;
    }

    /********************************** UTILITY METHODS **********************************/
    public bool ByteArrayToFile(string fileName, byte[] byteArray)
    {
        try
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(byteArray, 0, byteArray.Length);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Exception caught in process: ");
            return false;
        }
    }

    float[] toTexCoord(uint[] volumeCoords)
    {
        // Assumes a 256 x 256 x 256 volume
        float[] texCoord = new float[3];
        texCoord[0] = volumeCoords[0] / 256.0f;
        texCoord[1] = volumeCoords[1] / 256.0f;
        texCoord[2] = volumeCoords[2] / 256.0f;
        return texCoord;
    }

    bool floatsEqual(float f1, float f2)
    {
        float epsilon = 0.0000001f;
        if (Math.Abs(f1 - f2) < epsilon)
            return true;
        return false;
    }
}
