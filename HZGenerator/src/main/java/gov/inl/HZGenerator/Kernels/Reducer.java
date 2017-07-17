package gov.inl.HZGenerator.Kernels;

import gov.inl.HZGenerator.CLFW;
import org.jocl.*;

import static org.jocl.CL.*;

/**
 * Created by BITINK on 6/14/2017.
 */
public class Reducer {
	public static int addInts(cl_mem input, int numItems, int[] total) {
		int globalSize = CLFW.NextPow2(numItems);

		int[] error = {0};
		cl_kernel kernel = CLFW.Kernels.get("addInts");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Determine the number of groups required. */
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);

		int localSize = Integer.min(workgroupSize[0], globalSize);
		int numGroups = Integer.max((globalSize / workgroupSize[0]), 1); //+1

		/* Each workgroup gets a spot in the intermediate buffer. */
		cl_mem intermediate = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, Sizeof.cl_int * numGroups,
				null, error);
		int[] pattern = {-1};
		error[0] |= clEnqueueFillBuffer(queue, intermediate, Pointer.to(pattern), Sizeof.cl_int, 0,
				Sizeof.cl_int * numGroups, 0, null, null);

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel,1, Sizeof.cl_mem, Pointer.to(intermediate));
		error[0] |= clSetKernelArg(kernel,2, (localSize + 1) * Sizeof.cl_int, new Pointer());
		error[0] |= clSetKernelArg(kernel,3, Sizeof.cl_int, Pointer.to(new int[]{numItems}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 1, null, new long[]{globalSize}, new long[]{localSize},
				0, null, null);

		/* Read back the total */
		clEnqueueReadBuffer(queue, intermediate, CL_TRUE, Sizeof.cl_int * (numGroups - 1), Sizeof.cl_int,
				Pointer.to(total), 0, null, null);

		clReleaseMemObject(intermediate);
		return error[0];
	}

	public static int addImage(cl_mem input, int width, int height, long[] total) {
		int globalSize = CLFW.NextPow2(width * height);

		int[] error = {0};
		cl_kernel kernel = CLFW.Kernels.get("addImage");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Determine the number of groups required. */
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);

		int localSize = Integer.min(workgroupSize[0], globalSize);
		int numGroups = Integer.max((globalSize / workgroupSize[0]), 1); //+1

		/* Each workgroup gets a spot in the intermediate buffer. */
		cl_mem intermediate = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, Sizeof.cl_long * numGroups,
				null, error);
		long[] pattern = {-1};
		error[0] |= clEnqueueFillBuffer(queue, intermediate, Pointer.to(pattern), Sizeof.cl_long, 0,
				Sizeof.cl_long * numGroups, 0, null, null);

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_int, Pointer.to(new int[]{width}));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_int, Pointer.to(new int[]{height}));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel,3, Sizeof.cl_mem, Pointer.to(intermediate));
		error[0] |= clSetKernelArg(kernel,4, (localSize + 1) * Sizeof.cl_long, new Pointer());
		error[0] |= clSetKernelArg(kernel,5, Sizeof.cl_int, Pointer.to(new int[]{width * height}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 1, null, new long[]{globalSize}, new long[]{localSize},
				0, null, null);

		/* Read back the total */
		clEnqueueReadBuffer(queue, intermediate, CL_TRUE, Sizeof.cl_long * (numGroups - 1), Sizeof.cl_long,
				Pointer.to(total), 0, null, null);

		clReleaseMemObject(intermediate);
		return error[0];
	}
}