__constant sampler_t kawaseSampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

__kernel void kawaseBlur(
    const int width,
    const int height,
    const int r,
    __read_only image2d_t in,
    __write_only image2d_t out
) {
    /* Thread info */
    const int2 gn = {get_global_id(0), get_global_id(1)};
    const float2 fpos = {gn.x, gn.y};

    if (gn.x < width && gn.y < height) {
        int sum = (read_imagei(in, kawaseSampler, fpos + (float2)(-r -.5, -r -.5)).x +
                    read_imagei(in, kawaseSampler, fpos + (float2)(-r -.5, r + .5)).x +
                    read_imagei(in, kawaseSampler, fpos + (float2)(r + .5, -r -.5)).x +
                    read_imagei(in, kawaseSampler, fpos + (float2)(r + .5, r + .5)).x) / 4;
        write_imagei(out, gn, (int4)(sum, 0, 0, 255));
    }
}

