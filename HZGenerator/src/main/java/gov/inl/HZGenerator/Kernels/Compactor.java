package gov.inl.HZGenerator.Kernels;

import org.jocl.*;

import static org.jocl.CL.*;
import gov.inl.HZGenerator.CLFW;

/**
 * Created by BITINK on 6/16/2017.
 */
public class Compactor {
	public enum Type {
		INT, INT4
	};

	public static int CompactInt(cl_mem input, cl_mem predication, cl_mem address, int totalElements, cl_mem output) {
			return Compact(input, Type.INT, predication, address, totalElements, output);
	}

	public static int CompactInt4(cl_mem input, cl_mem predication, cl_mem address, int totalElements, cl_mem output) {
		return Compact(input, Type.INT4, predication, address, totalElements, output);
	}

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
