// "Insert" two 0 bits after each of the 10 low bits of x
inline unsigned int Part1By2(unsigned int x)
{
  x &= 0x000003ff;                  // x = ---- ---- ---- ---- ---- --98 7654 3210
  x = (x ^ (x << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
  x = (x ^ (x <<  8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
  x = (x ^ (x <<  4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
  x = (x ^ (x <<  2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
  return x;
}

// Inverse of Part1By2 - "delete" all bits not at positions divisible by 3
inline unsigned int Compact1By2(unsigned int x)
{
  x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
  x = (x ^ (x >>  2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
  x = (x ^ (x >>  4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
  x = (x ^ (x >>  8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
  x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
  return x;
}

inline unsigned int EncodeMorton3(unsigned int x, unsigned int y, unsigned int z)
{
  return (Part1By2(z) << 2) + (Part1By2(y) << 1) + Part1By2(x);
}

inline unsigned int DecodeMorton3X(unsigned int  code)
{
  return Compact1By2(code >> 0);
}

inline unsigned int DecodeMorton3Y(unsigned int  code)
{
  return Compact1By2(code >> 1);
}

inline unsigned int DecodeMorton3Z(unsigned int  code)
{
  return Compact1By2(code >> 2);
}

__kernel void XYZ2Z (
    __global uint3* points,
    __global uint* zs
) {
    int gid = get_global_id(0);
    uint3 point = points[gid];
    uint z = EncodeMorton3(point.s0, point.s1, point.s2);
    zs[gid] = z;
}

__kernel void Z2XYZ (
    __global uint* zs,
    __global uint3* points
) {
    int gid = get_global_id(0);
    uint z = zs[gid];
    uint3 point = (uint3)(DecodeMorton3X(z), DecodeMorton3Y(z),DecodeMorton3Z(z) );
    points[gid] = point;
}

__kernel void XYZ2HZ (
    __global uint3* points,
    __global uint* hzs,
    int last_bit_mask
) {
    int gid = get_global_id(0);
    uint3 point = points[gid];
    uint z = EncodeMorton3(point.s0, point.s1, point.s2);

    z |= last_bit_mask;
    z /= z & -z;
    z >>= 1;
    hzs[gid] = z;
}

__kernel void HZ2XYZ (
    __global uint* hzs,
    __global uint3* points,
    int last_bit_mask
) {
    int gid = get_global_id(0);
    uint c = hzs[gid];

    // Add back rightmost one.
    c = (c << 1) | 1;

    // Determine highest bit
    int i = c;
    i |= (i >>  1);
    i |= (i >>  2);
    i |= (i >>  4);
    i |= (i >>  8);
    i |= (i >> 16);
    i = i - (i >> 1);

    // Shift left by max bits - highest bit index.
    c = c * (last_bit_mask / i);

    // Mask the number to remove added 1.
    c &= ~last_bit_mask;

    uint3 point = (uint3)(DecodeMorton3X(c), DecodeMorton3Y(c),DecodeMorton3Z(c) );
    points[gid] = point;
}

/*
    brickAddresses is a texture where each pixel is a brick index.
        Each pixel corresponds to a uniform grid where each unit is the minimum brick size.
        To access, take gid / minimumBrickSize
    brickPositions contains an offset for each brick
*/
__kernel void curveLayer(
    __global int* brickAddresses,
    __global int4* brickPositions,
    __global int* brickSizes,
    int minimumBrickSize,
    int z, int width, int height,
    __global int* result,
    __global int* brick
) {
    int3 gid = (int3)(get_global_id(0), get_global_id(1), z);
    int3 tensorIdx = gid / minimumBrickSize;
    int tensorAddress = tensorIdx.x
                      + tensorIdx.y * (width / minimumBrickSize)
                      + tensorIdx.z * (width / minimumBrickSize) * (height / minimumBrickSize);
//    if (gid.x == 0 && gid.y == 4 && gid.z == 0) {
//        printf("gid: %d %d %d bid %d bs: %d levels %d bits: %d lbm: %d pos %d %d %d reverted %d %d %d \n",
//                    gid.x, gid.y, gid.z, brickNumber, brickSize_, levels,
//                    bits, last_bit_mask, position.x, position.y, position.z, point.x, point.y, point.z);
//
//        printf("tensorIdx %d %d %d \n", tensorIdx.x, tensorIdx.y, tensorIdx.z);
//    }
    int brickSize = brickSizes[tensorAddress];
    int brickSize_ = brickSize;
    int3 tensorOffset = (tensorIdx% (brickSize / minimumBrickSize));
    //printf("gid: %d %d %d tensorIdx %d %d %d tensorOffset %d %d %d\n",
      //  gid.x, gid.y, gid.z, tensorIdx.x, tensorIdx.y, tensorIdx.z,
        //tensorOffset.x, tensorOffset.y, tensorOffset.z);
    tensorIdx = tensorIdx - tensorOffset;



    tensorAddress = tensorIdx.x
                  + tensorIdx.y * (width / minimumBrickSize)
                  + tensorIdx.z * (width / minimumBrickSize) * (height / minimumBrickSize);

    int brickNumber = brickAddresses[tensorAddress] - 1;

    int4 offset = brickPositions[brickNumber];
//    int levels =(int)(ceil(log2((double)brickSize))) + 1;


    int levels = 0;
    while (brickSize > 0) {
        brickSize >>= 1;
        ++levels;
    }
    levels -= 1;

    int bits =  levels * 3;
    int last_bit_mask = 1 << bits;
    int3 position = gid - (int3)(offset.x, offset.y, offset.z);

    int zidx = EncodeMorton3(position.x, position.y, position.z);
    int zidx_ = zidx;
    zidx |= last_bit_mask;
    zidx /= zidx & -zidx;
    zidx >>= 1;
//    if (gid.x == 0 && gid.y == 1) {
        uint c = zidx;

        // Add back rightmost one.
        c = (c << 1) | 1;

        // Determine highest bit
        int i = c;
        i |= (i >>  1);
        i |= (i >>  2);
        i |= (i >>  4);
        i |= (i >>  8);
        i |= (i >> 16);
        i = i - (i >> 1);

        // Shift left by max bits - highest bit index.
        c = c * (last_bit_mask / i);

        // Mask the number to remove added 1.
        c &= ~last_bit_mask;

        uint3 point = (uint3)(DecodeMorton3X(c), DecodeMorton3Y(c),DecodeMorton3Z(c) );
//
//        printf("%d %d %d\n", position.x, position.y, position.z);
//        printf("%d %d %d\n", point.x, point.y, point.z);
//        printf("lbm %d\n", last_bit_mask);
//    }
//    if (position.x != point.x ||
//        position.y != point.y ||
//        position.z != point.z ||
//        (gid.x == 0 && gid.y == 128 && gid.z == 0)) {
      //  printf("gid: %d %d %d bid %d bs: %d levels %d bits: %d lbm: %d pos %d %d %d reverted %d %d %d \n",
        //    gid.x, gid.y, gid.z, brickNumber, brickSize_, levels,
          //  bits, last_bit_mask, position.x, position.y, position.z, point.x, point.y, point.z);
//    }


    //curve position.
    result[gid.x + gid.y * width] = zidx;
    brick[gid.x + gid.y * width] = brickNumber;
}