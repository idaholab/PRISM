inline int getAddress(
    __global int *predicationBuffer,
    __global int *addressBuffer,
    int size,
    const int gid
)
{
    int a = addressBuffer[gid];
    int b = addressBuffer[size - 2];
    int c = predicationBuffer[gid];
    int e = predicationBuffer[size - 1];

    int t = gid - a + (e + b);
    int d = (!c) ? t : a - 1;
    return d;
}

__kernel void CompactInt(
    __global int *inputBuffer,
    __global int *resultBuffer,
    __global int *predicationBuffer,
    __global int *addressBuffer,
    int size) {
    int gid = get_global_id(0);

    int address = getAddress(predicationBuffer, addressBuffer, size, gid);
    barrier(CLK_GLOBAL_MEM_FENCE);
    resultBuffer[address] = inputBuffer[gid];
}

__kernel void CompactInt4(
    __global int4 *inputBuffer,
    __global int4 *resultBuffer,
    __global int *predicationBuffer,
    __global int *addressBuffer,
    int size) {
    int gid = get_global_id(0);

    int address = getAddress(predicationBuffer, addressBuffer, size, gid);
    barrier(CLK_GLOBAL_MEM_FENCE);
    resultBuffer[address] = inputBuffer[gid];
}
