package gov.inl.HZGenerator.Kernels;

import gov.inl.HZGenerator.BrickFactory.Brick;
import org.jocl.cl_mem;

import java.util.ArrayList;
import java.util.List;

import static org.jocl.CL.clReleaseMemObject;

public class PartitionerResult {
	public cl_mem sparseSizes;
	public cl_mem predication;
	public cl_mem sparseLocations;
	public cl_mem addresses;
	public cl_mem denseSizes;
	public cl_mem denseLocations;
	public List<Brick> partitions;
	public int minDimSize;
	public int tensorWidth;
	public int tensorHeight;
	public int tensorDepth;
	public int tensorSize;

	public PartitionerResult() {
		partitions = new ArrayList<>();
	}

	public void finalize() {
		clReleaseMemObject(sparseSizes);
		clReleaseMemObject(predication);
		clReleaseMemObject(sparseLocations);
		clReleaseMemObject(denseSizes);
		clReleaseMemObject(addresses);
		clReleaseMemObject(denseLocations);
	}
}
