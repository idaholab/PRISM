#ifndef CLINT
#define CLINT
typedef int cl_int;
#endif

__kernel void addInts(
		__global cl_int* buffer,
		__global volatile cl_int* I,
		__local cl_int* scratch,
		cl_int totalElements)
{
    const cl_int gid = get_global_id(0);
    const cl_int lid = get_local_id(0);
    const cl_int wid = get_group_id(0);
    const cl_int ls = get_local_size(0);
    cl_int sum = 0;
    if (gid < totalElements) {
        scratch[lid] = buffer[gid];
        if (lid == (ls - 1))
            scratch[ls] = scratch[ls - 1];
    }
    else {
        scratch[lid] = 0;
        if (lid == (ls - 1))
            scratch[ls] = 0;
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    /* Build sum tree (could probably be optimized) */
    for (int s = 1; s <= ls; s <<= 1) {
        int i = (2 * s * (lid + 1)) - 1;
        if (i < ls) {
            scratch[i] += scratch[i - s];
        }
        barrier(CLK_LOCAL_MEM_FENCE);
    }

    //Do Adjacent sync, accumulating the reduction
    if (lid == 0 && gid != 0) {
        while (I[wid - 1] == -1);
        I[wid] = I[wid - 1] + scratch[ls - 1];
    }
    if (gid == 0) I[0] = scratch[ls - 1];
}

__constant sampler_t reduceSampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;
__kernel void addImage(
    const int width,
    const int height,
    __read_only image2d_t in,
    __global volatile long* I,
    __local long* scratch,
    cl_int totalElements
) {
    const cl_int gid = get_global_id(0);
    const cl_int lid = get_local_id(0);
    const cl_int wid = get_group_id(0);
    const cl_int ls = get_local_size(0);
    const int x = gid % width;
    const int y = gid / width;

    cl_int sum = 0;
    if (gid < totalElements) {
        scratch[lid] = read_imagei(in, reduceSampler, (int2)(x, y)).x;
        if (lid == (ls - 1))
            scratch[ls] = scratch[ls - 1];
    }
    else {
        scratch[lid] = 0;
        if (lid == (ls - 1))
            scratch[ls] = 0;
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    /* Build sum tree (could probably be optimized) */
    for (int s = 1; s <= ls; s <<= 1) {
        int i = (2 * s * (lid + 1)) - 1;
        if (i < ls) {
            scratch[i] += scratch[i - s];
        }
        barrier(CLK_LOCAL_MEM_FENCE);
    }

    //Do Adjacent sync, accumulating the reduction
    if (lid == 0 && gid != 0) {
        while (I[wid - 1] == -1);
        I[wid] = I[wid - 1] + scratch[ls - 1];
    }
    if (gid == 0) I[0] = scratch[ls - 1];
}
