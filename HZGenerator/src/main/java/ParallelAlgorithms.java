import org.jocl.*;

import static org.jocl.CL.*;

/**
 * Created by BITINK on 6/14/2017.
 */
public class ParallelAlgorithms {
	static int scan(cl_mem input, int numItems, cl_mem output) {
		int globalSize = CLFW.NextPow2(numItems);

		int[] error = {0};
		cl_kernel kernel = CLFW.Kernels.get("StreamScanKernel");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Determine the number of groups required. */
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);

		int localSize = Integer.min(workgroupSize[0], globalSize);
		int numGroups = (globalSize / workgroupSize[0]); //+1

		/* Each workgroup gets a spot in the intermediate buffer. */
		cl_mem intermediate = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, Sizeof.cl_int * numGroups,
				null, error);
		int[] pattern = {-1};
		error[0] |= clEnqueueFillBuffer(queue, intermediate, Pointer.to(pattern), Sizeof.cl_int, 0,
				Sizeof.cl_int * numGroups, 0, null, null);

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(output));
		error[0] |= clSetKernelArg(kernel,2, Sizeof.cl_mem, Pointer.to(intermediate));
		error[0] |= clSetKernelArg(kernel,3, (localSize + 1) * Sizeof.cl_int, new Pointer());
		error[0] |= clSetKernelArg(kernel,4, Sizeof.cl_int, Pointer.to(new int[]{numItems}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 1, null, new long[]{globalSize}, new long[]{localSize},
				0, null, null);

		clReleaseMemObject(intermediate);
		return error[0];
	}
}
