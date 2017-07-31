package gov.inl.HZGenerator.Kernels;

import org.jocl.*;

import static org.jocl.CL.*;
import gov.inl.HZGenerator.CLFW;

/**
 * Nate Morrical. Summer 2017.
 *
 * Check out 39.3.1, Stream Compaction
 * 	https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch39.html
 *
 * TLDR: Stream compaction produces a smaller vector with only interesting elements.
 */
public class Compactor {
	public enum Type {
		INT, INT4
	};

	public static int Compact(cl_mem input, Type type, cl_mem predication, cl_mem address, int totalElements, cl_mem output) {
		int[] error = {0};

		cl_kernel kernel = null;
		switch(type) {
			case INT: kernel = CLFW.Kernels.get("CompactInt");
				break;
			case INT4: kernel = CLFW.Kernels.get("CompactInt4");
				break;
		}
		cl_command_queue queue = CLFW.DefaultQueue;

		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_mem, Pointer.to(predication));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_mem, Pointer.to(address));
		error[0] |= clSetKernelArg(kernel, 4, Sizeof.cl_int, Pointer.to(new int[]{totalElements}));

		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 1, null,
				new long[]{totalElements}, null, 0, null, null);
		return error[0];
	}
}
