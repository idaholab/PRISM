import gov.inl.HZGenerator.Kernels.Scanner;
import junit.framework.TestCase;
import org.jocl.*;

import java.io.File;

import static org.jocl.CL.*;
import gov.inl.HZGenerator.CLFW;

/**
 * Created by BITINK on 6/14/2017.
 */
public class TestScan extends TestCase {
	protected void setUp() {
		assertEquals(CLFW.Initialize("Kernels/OpenCLSettings.json", "Kernels"), CL_SUCCESS);
	}

	public void testScan() {
		int[] data = new int[1000];

		for (int i = 0; i < 1000; ++i) {
			data[i] = 1;
		}

		int[] error = {0};

		cl_mem input, output;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR, 1000 * Sizeof.cl_int,
				Pointer.to(data), error);
		output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 1000 * Sizeof.cl_int,
				null, error);

		Scanner.StreamScan(input, 1000, output);

		int[] result = new int[1000];
		clEnqueueReadBuffer(CLFW.DefaultQueue, output, CL_TRUE, 0, 1000 * Sizeof.cl_int, Pointer.to(result),
				0, null, null );

		for (int i = 0; i < 1000; ++i) {
			assertEquals(i + 1, result[i]);
		}
	}
}
