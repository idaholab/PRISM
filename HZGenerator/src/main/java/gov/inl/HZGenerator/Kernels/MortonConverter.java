package gov.inl.HZGenerator.Kernels;

import gov.inl.HZGenerator.CLFW;
import org.jocl.*;

import static org.jocl.CL.*;

/**
 * Nathan Morrical. Summer 2017.
 */
public class MortonConverter {

	public static int curveLayer(PartitionerResult pr, int z, int[] positions, int[] brick) {
		int[] error = {0};

		cl_kernel kernel = CLFW.Kernels.get("curveLayer");
		cl_command_queue queue = CLFW.DefaultQueue;

		cl_mem result = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorWidth * pr.minDimSize * pr.tensorHeight * pr.minDimSize * Sizeof.cl_int, null, error);

		cl_mem brickbuffer = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorWidth * pr.minDimSize * pr.tensorHeight * pr.minDimSize * Sizeof.cl_int, null, error);

		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(pr.addresses));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(pr.denseLocations));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_mem, Pointer.to(pr.sparseSizes));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_int, Pointer.to(new int[]{pr.minDimSize}));
		error[0] |= clSetKernelArg(kernel, 4, Sizeof.cl_int, Pointer.to(new int[]{z}));
		error[0] |= clSetKernelArg(kernel, 5, Sizeof.cl_int, Pointer.to(new int[]{pr.tensorWidth * pr.minDimSize}));
		error[0] |= clSetKernelArg(kernel, 6, Sizeof.cl_int, Pointer.to(new int[]{pr.tensorHeight * pr.minDimSize}));
		error[0] |= clSetKernelArg(kernel, 7, Sizeof.cl_mem, Pointer.to(result));
		error[0] |= clSetKernelArg(kernel, 8, Sizeof.cl_mem, Pointer.to(brickbuffer));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 2, null,
				new long[]{pr.tensorWidth * pr.minDimSize, pr.tensorHeight * pr.minDimSize},
				null, 0, null, null);

		error[0] |= clEnqueueReadBuffer(queue, result, CL_TRUE, 0,
				pr.tensorWidth * pr.minDimSize * pr.tensorHeight * pr.minDimSize * Sizeof.cl_int,
				Pointer.to(positions), 0, null, null);

		error[0] |= clEnqueueReadBuffer(queue, brickbuffer, CL_TRUE, 0,
				pr.tensorWidth * pr.minDimSize * pr.tensorHeight * pr.minDimSize * Sizeof.cl_int,
				Pointer.to(brick), 0, null, null);

		int[] brickAddresses = new int[pr.tensorSize];
		error[0] = clEnqueueReadBuffer(queue, pr.addresses, CL_TRUE, 0,
				pr.tensorSize * Sizeof.cl_int,
				Pointer.to(brickAddresses), 0, null, null);

		clReleaseMemObject(result);
		clReleaseMemObject(brickbuffer);

		return error[0];
	}
}
