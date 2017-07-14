#ifndef CLINT
#define CLINT
typedef int cl_int;
#endif

__kernel void StreamScan(
		__global cl_int* buffer,
		__global cl_int* result,
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

    /* Build sum tree */
    for (int s = 1; s <= ls; s <<= 1) {
        int i = (2 * s * (lid + 1)) - 1;
        if (i < ls) {
            scratch[i] += scratch[i - s];
        }
        barrier(CLK_LOCAL_MEM_FENCE);
    }

    //Do Adjacent sync
    if (lid == 0 && gid != 0) {
        while (I[wid - 1] == -1);
        I[wid] = I[wid - 1] + scratch[ls - 1];
    }
    if (gid == 0) I[0] = scratch[ls - 1];

    /* Down-Sweep 4 ways */
    if (lid == 0) scratch[ls - 1] = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    for (int s = ls / 2; s > 0; s >>= 1) {
        int i = (2 * s * (lid + 1)) - 1;
        int temp;
        if (i < ls)
            temp = scratch[i - s];
        barrier(CLK_LOCAL_MEM_FENCE);
        if (i < ls) {
            scratch[i - s] = scratch[i];
            scratch[i] += temp;
        }
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    if (gid < totalElements) {
        sum = (lid == ls - 1) ? scratch[ls - 1] + scratch[ls] : scratch[lid + 1];
        if (wid != 0) sum += I[wid - 1];
        result[gid] = sum;
    }
}
