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