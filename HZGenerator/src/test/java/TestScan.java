import junit.framework.TestCase;
import org.jocl.*;

import java.io.File;

import static org.jocl.CL.*;

/**
 * Created by BITINK on 6/14/2017.
 */
public class TestScan extends TestCase {
	protected void setUp() {
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		assertEquals(CLFW.Initialize(openCLSettings), CL_SUCCESS);
	}

	public void testScan() {
		int[] data = new int[1000];

		for (int i = 0; i < 1000; ++i) {
			data[i] = 1 % 2;
		}

		int[] error = {0};

		cl_mem input, output;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR, 1000 * Sizeof.cl_int,
				Pointer.to(data), error);
		output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 1000 * Sizeof.cl_int,
				null, error);

		ParallelAlgorithms.scan(input, 1000, output);

		int[] result = new int[1000];
		clEnqueueReadBuffer(CLFW.DefaultQueue, output, CL_TRUE, 0, 1000 * Sizeof.cl_int, Pointer.to(result),
				0, null, null );
	}
}
