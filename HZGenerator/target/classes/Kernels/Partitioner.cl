__kernel void CombineBricks(
    __global int *tensor,
    __global int *predication,
    __global int3 *location,
    int minDim,
    int maxDim,
    int width,
    int height,
    int depth
) {
    int3 gid = (int3)(get_global_id(0), get_global_id(1), get_global_id(2));
    int3 gs = (int3)(get_global_size(0), get_global_size(1), get_global_size(2));
    int3 res = (int3)(width, height, depth);

    int currentWidth = minDim;
    int i = 2;
    int3 dim = res - res % i;
    int p = 1;
    while(all(dim > 0) && currentWidth < maxDim) {
        if (all(gid < dim)) {
            currentWidth  <<= 1;
            if (any((gid % i) != 0))  {
                p = 0;
            }
        }
        i <<= 1;
        dim = res - res % i;
    }

    if (all(gid < res)) {
        int index = gid.x + gid.y * res.x + gid.z * res.x * res.y;
        tensor[index] = currentWidth;
        if (p == 1) location[index] = gid * minDim;
        predication[index] = p;
    }
}

