package gov.inl.HZGenerator.Kernels;

import gov.inl.HZGenerator.BrickFactory.Brick;
import org.jocl.*;
import static org.jocl.CL.*;

import gov.inl.HZGenerator.*;

import java.util.List;

/**
 * Created by BITINK on 6/16/2017.
 */
public class Partitioner {

	public static int numBricks = -1;

	// min and max dim size should be a power of two
	// width, height, and depth are in pixels
	public static int CombineBricks( int width, int height, int depth, int minDimSize, int maxDimSize,
									 List<Brick> partitions) {
		numBricks = -1;
		int[] error = {0};

		// Determine tensor dimensions
		int tensorWidth = (int)Math.ceil(Math.max(width / (float)minDimSize, 1));
		int tensorHeight = (int)Math.ceil(Math.max(height / (float)minDimSize, 1));
		int tensorDepth = (int)Math.ceil(Math.max(depth / (float)minDimSize, 1));
		int tensorSize = tensorWidth * tensorHeight * tensorDepth;

		// Allocate tensor
		cl_mem sparseSizes = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				tensorSize * Sizeof.cl_int, null, error);
		cl_mem predication = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				tensorSize * Sizeof.cl_int, null, error);
		cl_mem sparseLocations = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				tensorSize * Sizeof.cl_int4, null, error);
		cl_mem addresses = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				tensorSize * Sizeof.cl_int, null, error);

		int globalWidth = CLFW.NextPow2(tensorWidth);
		int globalHeight = CLFW.NextPow2(tensorHeight);
		int globalDepth = CLFW.NextPow2(tensorDepth);
		int globalMin = Integer.min(Integer.min(globalWidth, globalHeight), globalDepth);

		cl_kernel kernel = CLFW.Kernels.get("CombineBricks");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Determine the number of groups required. */
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);

		int localWidth, localHeight, localDepth;
		localWidth = localHeight = localDepth =
				Integer.min(CLFW.NextPow2((int)Math.cbrt((double)workgroupSize[0])) >> 1, globalMin);

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(sparseSizes));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(predication));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_mem, Pointer.to(sparseLocations));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_int, Pointer.to(new int[]{minDimSize}));
		error[0] |= clSetKernelArg(kernel,4, Sizeof.cl_int, Pointer.to(new int[]{maxDimSize}));
		error[0] |= clSetKernelArg(kernel,5, Sizeof.cl_int, Pointer.to(new int[]{tensorWidth}));
		error[0] |= clSetKernelArg(kernel,6, Sizeof.cl_int, Pointer.to(new int[]{tensorHeight}));
		error[0] |= clSetKernelArg(kernel,7, Sizeof.cl_int, Pointer.to(new int[]{tensorDepth}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 3, null,
				new long[]{globalWidth, globalHeight, globalDepth},
				new long[]{localWidth, localHeight, localDepth},
				0, null, null);

		/* Scanner the predication to compute densifying addresses */
		Scanner.StreamScan(predication, tensorSize, addresses);

		/* The last element in the scan indicates the total number of bricks */
		int[] totalBricks = {-1};
		clEnqueueReadBuffer(queue, addresses, CL_TRUE, (tensorSize - 1) * Sizeof.cl_int, Sizeof.cl_int,
				Pointer.to(totalBricks), 0, null, null);

		cl_mem denseSizes = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				tensorSize * Sizeof.cl_int, null, error);
		cl_mem denseLocations = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				tensorSize * Sizeof.cl_int4, null, error);

		/* Compactor the size tensor */
		Compactor.Compact(sparseSizes, Compactor.Type.INT, predication, addresses, tensorSize, denseSizes);
		Compactor.Compact(sparseLocations, Compactor.Type.INT4, predication, addresses, tensorSize, denseLocations);

		/* Download data */
		int[] locations = new int[tensorSize * 4];
		int[] sizes = new int[totalBricks[0]];
		error[0] |= clEnqueueReadBuffer(queue, denseLocations, CL_TRUE, 0, totalBricks[0] * Sizeof.cl_int4,
				Pointer.to(locations), 0, null, null);
		error[0] |= clEnqueueReadBuffer(queue, denseSizes, CL_TRUE, 0, totalBricks[0] * Sizeof.cl_int,
				Pointer.to(sizes), 0, null, null);

		/* Append bricks to the list */
		for (int i = 0; i < totalBricks[0]; ++i) {
			Brick p = new Brick();
			p.setPosition(locations[4 * i], locations[(4 * i) + 1], locations[(4 * i) + 2]);
			p.setSize(sizes[i]);
			partitions.add(p);
		}

		/* Release OpenCL memory */
		clReleaseMemObject(sparseSizes);
		clReleaseMemObject(predication);
		clReleaseMemObject(sparseLocations);
		clReleaseMemObject(denseSizes);
		clReleaseMemObject(addresses);
		clReleaseMemObject(denseLocations);
		return error[0];
	}


	public static int CombineBricks (
			int width, int height, int depth, int minDimSize, int maxDimSize,
			PartitionerResult pr) {
		pr.bricks.clear();
		pr.minDimSize = minDimSize;
		numBricks = -1;
		int[] error = {0};

		// Determine tensor dimensions
		pr.tensorWidth = (int)Math.ceil(Math.max(width / (float)minDimSize, 1));
		pr.tensorHeight = (int)Math.ceil(Math.max(height / (float)minDimSize, 1));
		pr.tensorDepth = (int)Math.ceil(Math.max(depth / (float)minDimSize, 1));
		pr.tensorSize = pr.tensorWidth * pr.tensorHeight * pr.tensorDepth;

		// Allocate tensor
		pr.sparseSizes = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorSize * Sizeof.cl_int, null, error);
		pr.predication = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorSize * Sizeof.cl_int, null, error);
		pr.sparseLocations = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorSize * Sizeof.cl_int4, null, error);
		pr.addresses = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorSize * Sizeof.cl_int, null, error);

		int globalWidth = CLFW.NextPow2(pr.tensorWidth);
		int globalHeight = CLFW.NextPow2(pr.tensorHeight);
		int globalDepth = CLFW.NextPow2(pr.tensorDepth);
		int globalMin = Integer.min(Integer.min(globalWidth, globalHeight), globalDepth);

		cl_kernel kernel = CLFW.Kernels.get("CombineBricks");
		cl_command_queue queue = CLFW.DefaultQueue;

		/* Determine the number of groups required. */
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);

		int localWidth, localHeight, localDepth;
		localWidth = localHeight = localDepth =
				Integer.min(CLFW.NextPow2((int)Math.cbrt((double)workgroupSize[0])) >> 1, globalMin);

		/* Call the kernel */
		error[0] |= clSetKernelArg(kernel, 0, Sizeof.cl_mem, Pointer.to(pr.sparseSizes));
		error[0] |= clSetKernelArg(kernel, 1, Sizeof.cl_mem, Pointer.to(pr.predication));
		error[0] |= clSetKernelArg(kernel, 2, Sizeof.cl_mem, Pointer.to(pr.sparseLocations));
		error[0] |= clSetKernelArg(kernel, 3, Sizeof.cl_int, Pointer.to(new int[]{minDimSize}));
		error[0] |= clSetKernelArg(kernel,4, Sizeof.cl_int, Pointer.to(new int[]{maxDimSize}));
		error[0] |= clSetKernelArg(kernel,5, Sizeof.cl_int, Pointer.to(new int[]{pr.tensorWidth}));
		error[0] |= clSetKernelArg(kernel,6, Sizeof.cl_int, Pointer.to(new int[]{pr.tensorHeight}));
		error[0] |= clSetKernelArg(kernel,7, Sizeof.cl_int, Pointer.to(new int[]{pr.tensorDepth}));
		error[0] |= clEnqueueNDRangeKernel(queue, kernel, 3, null,
				new long[]{globalWidth, globalHeight, globalDepth},
				new long[]{localWidth, localHeight, localDepth},
				0, null, null);

		/* Scanner the predication to compute densifying addresses */
		Scanner.StreamScan(pr.predication, pr.tensorSize, pr.addresses);

		/* The last element in the scan indicates the total number of bricks */
		int[] totalBricks = {-1};
		clEnqueueReadBuffer(queue, pr.addresses, CL_TRUE, (pr.tensorSize - 1) * Sizeof.cl_int, Sizeof.cl_int,
				Pointer.to(totalBricks), 0, null, null);

		pr.denseSizes = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorSize * Sizeof.cl_int, null, error);
		pr.denseLocations = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE,
				pr.tensorSize * Sizeof.cl_int4, null, error);

		/* Compactor the size tensor */
		Compactor.Compact(pr.sparseSizes, Compactor.Type.INT, pr.predication, pr.addresses, pr.tensorSize, pr.denseSizes);
		Compactor.Compact(pr.sparseLocations, Compactor.Type.INT4, pr.predication, pr.addresses, pr.tensorSize, pr.denseLocations);

		/* Download data */
		int[] locations = new int[pr.tensorSize * 4];
		int[] sizes = new int[totalBricks[0]];
		error[0] |= clEnqueueReadBuffer(queue, pr.denseLocations, CL_TRUE, 0, totalBricks[0] * Sizeof.cl_int4,
				Pointer.to(locations), 0, null, null);
		error[0] |= clEnqueueReadBuffer(queue, pr.denseSizes, CL_TRUE, 0, totalBricks[0] * Sizeof.cl_int,
				Pointer.to(sizes), 0, null, null);

		/* Append bricks to the list */
		for (int i = 0; i < totalBricks[0]; ++i) {
			Brick p = new Brick();
			p.setPosition(locations[4 * i], locations[(4 * i) + 1], locations[(4 * i) + 2]);
			p.setSize(sizes[i]);
			pr.bricks.add(p);
		}

		return error[0];
	}

	private static void printTensor(int width, int height, int depth, int[] tensor) {
		for (int z = 0; z < depth; ++z) {
			for (int y = 0; y < height; ++y) {
				for (int x = 0; x < width; ++x) {
					int index = x + y * width + z * width * height;
					System.out.print(tensor[index] + " ");
				}
				System.out.println();
			}
			System.out.println("----------------");
		}
	}

	public static PartitionerResult makePartitionerResult() {
		return new PartitionerResult();
	}
}

