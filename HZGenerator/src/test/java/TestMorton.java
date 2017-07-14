/**
 * Created by BITINK on 6/14/2017.
 */
import junit.framework.*;

import java.io.File;
import static org.jocl.CL.*;
import org.jocl.*;

import static junit.framework.TestCase.assertEquals;
import gov.inl.HZGenerator.CLFW;

public class TestMorton extends TestCase {
	protected void setUp() {
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		assertEquals(CLFW.Initialize(openCLSettings), CL_SUCCESS);
	}

	int Part1By2(int x)
	{
		x &= 0x000003ff;                  // x = ---- ---- ---- ---- ---- --98 7654 3210
		x = (x ^ (x << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
		x = (x ^ (x <<  8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
		x = (x ^ (x <<  4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
		x = (x ^ (x <<  2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
		return x;
	}

	int Compact1By2(int x)
	{
		x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
		x = (x ^ (x >>  2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
		x = (x ^ (x >>  4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
		x = (x ^ (x >>  8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
		x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
		return x;
	}

	int EncodeMorton3(int x, int y, int z)
	{
		return (Part1By2(z) << 2) + (Part1By2(y) << 1) + Part1By2(x);
	}

	int EncodeHMorton3(int x, int y, int z, int last_bit_mask)
	{
		int morton = (Part1By2(z) << 2) + (Part1By2(y) << 1) + Part1By2(x);
		morton |= last_bit_mask;
		morton /= morton & -morton;
		morton >>= 1;
		return morton;
	}

	int DecodeMorton3X(int  code)
	{
		return Compact1By2(code >> 0);
	}

	int DecodeMorton3Y(int  code)
	{
		return Compact1By2(code >> 1);
	}

	int DecodeMorton3Z(int  code)
	{
		return Compact1By2(code >> 2);
	}

	int DecodeHMorton3X(int  c, int last_bit_mask)
	{
		// Add back rightmost one.
		c = (c << 1) | 1;

		// Determine highest bit
		int i = c;
		i |= (i >>  1);
		i |= (i >>  2);
		i |= (i >>  4);
		i |= (i >>  8);
		i |= (i >> 16);
		i = i - (i >>> 1);

		// Shift left by max bits - highest bit index.
		c = c * (last_bit_mask / i);

		// Mask the number to remove added 1.
		c &= ~last_bit_mask;
		return Compact1By2(c >> 0);
	}

	int DecodeHMorton3Y(int  c, int last_bit_mask)
	{
		// Add back rightmost one.
		c = (c << 1) | 1;

		// Determine highest bit
		int i = c;
		i |= (i >>  1);
		i |= (i >>  2);
		i |= (i >>  4);
		i |= (i >>  8);
		i |= (i >> 16);
		i = i - (i >>> 1);

		// Shift left by max bits - highest bit index.
		c = c * (last_bit_mask / i);

		// Mask the number to remove added 1.
		c &= ~last_bit_mask;
		return Compact1By2(c >> 1);
	}

	int DecodeHMorton3Z(int  c, int last_bit_mask)
	{
		// Add back rightmost one.
		c = (c << 1) | 1;

		// Determine highest bit
		int i = c;
		i |= (i >>  1);
		i |= (i >>  2);
		i |= (i >>  4);
		i |= (i >>  8);
		i |= (i >> 16);
		i = i - (i >>> 1);

		// Shift left by max bits - highest bit index.
		c = c * (last_bit_mask / i);

		// Mask the number to remove added 1.
		c &= ~last_bit_mask;
		return Compact1By2(c >> 2);
	}

	public void testXYZ2Z() {
		int[] points = new int[ 4 * 256];

		// Given a set of points
		for (int i = 0; i < 256; ++i) {
			points[i * 4]     = i;     //x
			points[i * 4 + 1] = i + 1; //y
			points[i * 4 + 2] = i + 2; //z
			points[i * 4 + 3] = 1;     //w
		}

		// When these points are uploaded to the GPU
		int[] error = {0};
		cl_mem input, output;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
				256 * Sizeof.cl_int4, Pointer.to(points), error);
		assertEquals(error[0], CL_SUCCESS);

		output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 256 * Sizeof.cl_int, null, error);
		assertEquals(error[0], CL_SUCCESS);

		// When the XYZ2Z kernel is called on these points in parallel
		cl_kernel xyz2zKernel = CLFW.Kernels.get("XYZ2Z");
		clSetKernelArg(xyz2zKernel, 0, Sizeof.cl_mem, Pointer.to(input));
		clSetKernelArg(xyz2zKernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, xyz2zKernel, 1,
				null, new long[]{256}, null,0,
				null, null);
		assertEquals(error[0], CL_SUCCESS);

		int[] result = new int[256];
		// Then when we download the Z-Order points
		error[0] |= clEnqueueReadBuffer(CLFW.DefaultQueue, output, CL_TRUE, 0, 256 * Sizeof.cl_int,
				Pointer.to(result),0, null, null);

		assertEquals(error[0], CL_SUCCESS);

		// These Z-Order numbers should then be valid.
		for (int i = 0; i < 256; ++i) {
			assertEquals(result[i], EncodeMorton3(points[i * 4], points[i * 4 + 1], points[i * 4 + 2] ));
		}
	}

	public void testZ2XYZ() {
		int[] zs = new int[256];

		// Given a set of z-order numbers
		for (int i = 0; i < 256; ++i) {
			zs[i] = i;
		}

		// When these z-order numbers are uploaded to the GPU
		int[] error = {0};
		cl_mem input, output;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
				256 * Sizeof.cl_int, Pointer.to(zs), error);
		assertEquals(error[0], CL_SUCCESS);
		output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 256 * Sizeof.cl_int4, null, error);
		assertEquals(error[0], CL_SUCCESS);

		// When the Z2XYZ kernel is called on these z-order numbers in parallel
		cl_kernel z2xyzKernel = CLFW.Kernels.get("Z2XYZ");
		error[0] |= clSetKernelArg(z2xyzKernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(z2xyzKernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, z2xyzKernel, 1,
				null, new long[]{256}, null, 0,
				null, null);
		assertEquals(error[0], CL_SUCCESS);

		int[] result = new int[256 * 4];

		// Then when we download the cartesian points
		error[0] |= clEnqueueReadBuffer(CLFW.DefaultQueue, output, CL_TRUE, 0, 256 * Sizeof.cl_int4,
				Pointer.to(result), 0, null, null);


		// These points should then be valid
		for (int i = 0; i < 256; ++i) {
			assertEquals(result[i * 4], DecodeMorton3X(zs[i]));
			assertEquals(result[i * 4 + 1], DecodeMorton3Y(zs[i]));
			assertEquals(result[i * 4 + 2], DecodeMorton3Z(zs[i]));
		}
	}

	public void testXYZ2HZ() {
		int[] points = new int[ 4 * 256];

		// Given a set of points
		for (int i = 0; i < 256; ++i) {
			points[i * 4]     = i;     //x
			points[i * 4 + 1] = i + 1; //y
			points[i * 4 + 2] = i + 2; //z
			points[i * 4 + 3] = 1;     //w
		}

		// When these points are uploaded to the GPU
		int[] error = {0};
		cl_mem input, output;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
				256 * Sizeof.cl_int4, Pointer.to(points), error);
		assertEquals(error[0], CL_SUCCESS);

		output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 256 * Sizeof.cl_int, null, error);
		assertEquals(error[0], CL_SUCCESS);

		// When the XYZ2HZ kernel is called on these points in parallel

		int[] last_bit_mask = {1 << (8 * 3)};
		cl_kernel xyz2hzKernel = CLFW.Kernels.get("XYZ2HZ");
		error[0] |= clSetKernelArg(xyz2hzKernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(xyz2hzKernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(xyz2hzKernel, 2, Sizeof.cl_int, Pointer.to(last_bit_mask));
		assertEquals(error[0], CL_SUCCESS);

		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, xyz2hzKernel, 1,
				null, new long[]{256}, null,0,
				null, null);
		assertEquals(error[0], CL_SUCCESS);

		int[] result = new int[256];
		// Then when we download the HZ-Order points
		error[0] |= clEnqueueReadBuffer(CLFW.DefaultQueue, output, CL_TRUE, 0, 256 * Sizeof.cl_int,
				Pointer.to(result),0, null, null);

		assertEquals(error[0], CL_SUCCESS);

		// These HZ-Order numbers should then be valid.
		for (int i = 0; i < 256; ++i) {
			assertEquals(result[i],
					EncodeHMorton3(points[i * 4], points[i * 4 + 1], points[i * 4 + 2], 1 << (8 * 3)));
		}
	}

	public void testHZ2XYZ() {
		int[] zs = new int[256];

		// Given a set of hz-order numbers
		for (int i = 0; i < 256; ++i) {
			zs[i] = i;
		}

		// When these hz-order numbers are uploaded to the GPU
		int[] error = {0};
		cl_mem input, output;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
				256 * Sizeof.cl_int, Pointer.to(zs), error);
		assertEquals(error[0], CL_SUCCESS);
		output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 256 * Sizeof.cl_int4, null, error);
		assertEquals(error[0], CL_SUCCESS);

		// When the HZ2XYZ kernel is called on these hz-order numbers in parallel
		cl_kernel z2xyzKernel = CLFW.Kernels.get("HZ2XYZ");
		int[] last_bit_mask = {1 << 8};
		error[0] |= clSetKernelArg(z2xyzKernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(z2xyzKernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(z2xyzKernel, 2, Sizeof.cl_int, Pointer.to(last_bit_mask));
		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, z2xyzKernel, 1,
				null, new long[]{256}, null, 0,
				null, null);
		assertEquals(error[0], CL_SUCCESS);

		int[] result = new int[256 * 4];

		// Then when we download the cartesian points
		error[0] |= clEnqueueReadBuffer(CLFW.DefaultQueue, output, CL_TRUE, 0, 256 * Sizeof.cl_int4,
				Pointer.to(result), 0, null, null);
		assertEquals(error[0], CL_SUCCESS);

		// These points should then be valid
		for (int i = 0; i < 256; ++i) {
			assertEquals(result[i * 4], DecodeHMorton3X(zs[i], 1 << 8));
			assertEquals(result[i * 4 + 1], DecodeHMorton3Y(zs[i],1 << 8 ));
			assertEquals(result[i * 4 + 2], DecodeHMorton3Z(zs[i], 1 << 8));
		}
	}
}
