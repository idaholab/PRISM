using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HZ16Test : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    //public struct uint3
    //{
    //    public uint x;
    //    public uint y;
    //    public uint z;

    //    public uint3(uint _x, uint _y, uint _z)
    //    {
    //        x = _x;
    //        y = _y;
    //        z = _z;
    //    }
    //}

    ///// <summary>
    ///// Runs through a test of accessing a 16 bit element at the given zIndex.
    ///// </summary>
    //public void testFunction(uint zIndex)
    //{
    //    // Get the masked Z index from the given zIndex
    //    uint maskedZIndex = computeMaskedZIndex(zIndex);       

    //    // Find the hz order index
    //    uint hzIndex = getHZIndex(maskedZIndex);

    //    // Get the dimension of the
    //    uint dataCubeDimension = 1 << _CurrentZLevel;                       // The dimension of the data brick using the current hz level.
    //    float3 texCoord = texCoord3DFromHzIndex(hzIndex, dataCubeDimension, dataCubeDimension, dataCubeDimension);
    //    float data = tex3Dlod(_VolumeDataTexture, float4(texCoord, 0)).a;
    //    return data;
    //}

    ///***************************************** HZ CURVING CODE ************************************************/
    //private uint Compact1By2(uint x)
    //{
    //    x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
    //    x = (x ^ (x >> 2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
    //    x = (x ^ (x >> 4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
    //    x = (x ^ (x >> 8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
    //    x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
    //    return x;
    //}

    //private uint DecodeMorton3X(uint code)
    //{
    //    return Compact1By2(code >> 2);
    //}

    //private uint DecodeMorton3Y(uint code)
    //{
    //    return Compact1By2(code >> 1);
    //}

    //private uint DecodeMorton3Z(uint code)
    //{
    //    return Compact1By2(code >> 0);
    //}

    //private uint3 decode(uint c)
    //{
    //    uint3 cartEquiv = new uint3(0, 0, 0);
    //    c = c << 1 | 1;
    //    uint i = c | c >> 1;
    //    i |= i >> 2;
    //    i |= i >> 4;
    //    i |= i >> 8;
    //    i |= i >> 16;

    //    i -= i >> 1;

    //    c *= _LastBitMask / i;
    //    c &= (~_LastBitMask);
    //    cartEquiv.x = DecodeMorton3X(c);
    //    cartEquiv.y = DecodeMorton3Y(c);
    //    cartEquiv.z = DecodeMorton3Z(c);

    //    return cartEquiv;
    //}

    //// Expands an 8-bit integer into 24 bits by inserting 2 zeros after each bit
    //// Taken from: https://webcache.googleusercontent.com/search?q=cache:699-OSphYRkJ:https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/+&cd=1&hl=en&ct=clnk&gl=us
    //uint Part1By2(uint x)
    //{
    //    x &= 0x000003ff;                  // x = ---- ---- ---- ---- ---- --98 7654 3210
    //    x = (x ^ (x << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
    //    x = (x ^ (x << 8)) & 0x0300f00f;  // x = ---- --98 ---- ---- 7654 ---- ---- 3210
    //    x = (x ^ (x << 4)) & 0x030c30c3;  // x = ---- --98 ---- 76-- --54 ---- 32-- --10
    //    x = (x ^ (x << 2)) & 0x09249249;  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
    //    return x;
    //}

    //// Calculates a 24-bit Morton code for the given 3D point located within the unit cube [0, 1]
    //// Taken from: https://devblogs.nvidia.com/parallelforall/thinking-parallel-part-iii-tree-construction-gpu/
    //uint morton3D(float3 pos)
    //{
    //    // Quantize to the correct resolution
    //    pos.x = min(max(pos.x * (float)_BrickSize, 0.0f), (float)_BrickSize - 1);
    //    pos.y = min(max(pos.y * (float)_BrickSize, 0.0f), (float)_BrickSize - 1);
    //    pos.z = min(max(pos.z * (float)_BrickSize, 0.0f), (float)_BrickSize - 1);

    //    // Interlace the bits
    //    uint xx = Part1By2((uint)pos.x);
    //    uint yy = Part1By2((uint)pos.y);
    //    uint zz = Part1By2((uint)pos.z);

    //    return zz << 2 | yy << 1 | xx;
    //}

    //// Return the index into the hz-ordered array of data given a quantized point within the volume
    //uint getHZIndex(uint zIndex)
    //{
    //    uint hzIndex = (zIndex | _LastBitMask);     // set leftmost one
    //    hzIndex /= hzIndex & -hzIndex;              // remove trailing zeros
    //    return (hzIndex >> 1);                      // remove rightmost one
    //}

    //// Returns the masked z index, allowing for the the data to be quantized to a level of detail specified by the _CurrentZLevel.
    //uint computeMaskedZIndex(uint zIndex)
    //{
    //    int zBits = _MaxZLevel * 3;
    //    uint zMask = -1 >> (zBits - 3 * _CurrentZLevel) << (zBits - 3 * _CurrentZLevel);
    //    return zIndex & zMask;
    //}

    ///***************************************** END HZ CURVING CODE ************************************************/

    ///********* SAMPLING 3D HZ CURVED RAW DATA WITH TEXTURE COORD CALCULATION **********/
    //private float sampleIntensityHz3D(float3 pos)
    //{
    //    uint zIndex = morton3D(pos);                                        // Get the Z order index		
    //    uint maskedZIndex = computeMaskedZIndex(zIndex);                    // Get the masked Z index
    //    uint hzIndex = getHZIndex(maskedZIndex);                            // Find the hz order index
    //    uint dataCubeDimension = 1 << _CurrentZLevel;                       // The dimension of the data brick using the current hz level.
    //    float3 texCoord = texCoord3DFromHzIndex(hzIndex, dataCubeDimension, dataCubeDimension, dataCubeDimension);
    //    float data = tex3Dlod(_VolumeDataTexture, float4(texCoord, 0)).a;
    //    return data;
    //}
}
