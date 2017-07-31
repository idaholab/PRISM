package gov.inl.HZGenerator.Kernels;

import gov.inl.HZGenerator.CLFW;
import org.jocl.*;
import org.joml.Vector3f;
import org.joml.Vector3i;

import java.nio.ByteBuffer;

import static org.jocl.CL.*;

/**
 * Nathan Morrical. Summer 2017.
 *
 * MortonConverter contains kernels which compute Z Order and HZ Order indices for given cartesian coordinates.
 * 	MortonConverter also is capable of curving volumes using the above curves.
 *
 * 	See this: http://www.pascucci.org/pdf-papers/chapter-thaoe.pdf
 */
public class MortonConverter {

	public static int curveByteVolume(int brickWidth, byte[] rawVolume, byte[] curvedVolume) {
		int[] error = {0};

		cl_kernel kernel = CLFW.Kernels.get("curveByteVolume");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Allocate memory */
		int bufferSize = brickWidth * brickWidth * brickWidth * Sizeof.cl_char;
		cl_mem input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
				bufferSize, Pointer.to(rawVolume), error);

		cl_mem output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_WRITE_ONLY,
				bufferSize, null, error);

		/* Compute last bit mask */
		int temp = brickWidth;
		int levels = 0;
		while (temp > 0) {
			temp >>= 1;
			++levels;
		}
		levels -= 1;
		int bits =  levels * 3;
		int last_bit_mask = 1 << bits;

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_int, Pointer.to(new int[]{brickWidth}));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_int, Pointer.to(new int[]{last_bit_mask}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 3, null,
				new long[]{brickWidth, brickWidth, brickWidth},
				null, 0, null, null);

		/* Download the curved volume */
		error[0] |= clEnqueueReadBuffer(queue, output, CL_TRUE, 0, bufferSize,
				Pointer.to(curvedVolume), 0, null, null);

		clReleaseMemObject(input);
		clReleaseMemObject(output);
		return error[0];
	}

	public static int curveShortVolume(int brickWidth, short[] rawVolume, short[] curvedVolume) {
		int[] error = {0};

		cl_kernel kernel = CLFW.Kernels.get("curveShortVolume");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Allocate memory */
		int bufferSize = brickWidth * brickWidth * brickWidth * Sizeof.cl_ushort;


		cl_mem input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
				bufferSize, Pointer.to(rawVolume), error);

		cl_mem output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_WRITE_ONLY,
				bufferSize, null, error);

		/* Compute last bit mask */
		int temp = brickWidth;
		int levels = 0;
		while (temp > 0) {
			temp >>= 1;
			++levels;
		}
		levels -= 1;
		int bits =  levels * 3;
		int last_bit_mask = 1 << bits;

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_int, Pointer.to(new int[]{brickWidth}));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_int, Pointer.to(new int[]{last_bit_mask}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 3, null,
				new long[]{brickWidth, brickWidth, brickWidth},
				null, 0, null, null);

		/* Download the curved volume */
		error[0] |= clEnqueueReadBuffer(queue, output, CL_TRUE, 0, bufferSize,
				Pointer.to(curvedVolume), 0, null, null);

		clReleaseMemObject(input);
		clReleaseMemObject(output);
		return error[0];
	}

	/* Scales 16 bit volume to 8 bit. Helpful for visualizers that only support 8 bits, or for decreasing volume size */
	public static int curveShortToByteVolume(int brickWidth, short[] rawVolume, byte[] curvedVolume) {
		int[] error = {0};

		cl_kernel kernel = CLFW.Kernels.get("curveShortToByteVolume");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Allocate memory */
		int bufferSize = brickWidth * brickWidth * brickWidth * Sizeof.cl_ushort;
		cl_mem input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
				bufferSize, Pointer.to(rawVolume), error);

		cl_mem output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_WRITE_ONLY,
				bufferSize / 2, null, error);

		/* Compute last bit mask */
		int temp = brickWidth;
		int levels = 0;
		while (temp > 0) {
			temp >>= 1;
			++levels;
		}
		levels -= 1;
		int bits =  levels * 3;
		int last_bit_mask = 1 << bits;

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_int, Pointer.to(new int[]{brickWidth}));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_int, Pointer.to(new int[]{last_bit_mask}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 3, null,
				new long[]{brickWidth, brickWidth, brickWidth},
				null, 0, null, null);

		/* Download the curved volume */
		error[0] |= clEnqueueReadBuffer(queue, output, CL_TRUE, 0, bufferSize / 2,
				Pointer.to(curvedVolume), 0, null, null);

		clReleaseMemObject(input);
		clReleaseMemObject(output);
		return error[0];
	}
}
