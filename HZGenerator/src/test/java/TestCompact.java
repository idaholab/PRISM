import gov.inl.HZGenerator.Kernels.Compactor;
import junit.framework.TestCase;
import org.jocl.Pointer;
import org.jocl.Sizeof;
import org.jocl.cl_mem;

import java.io.File;

import static org.jocl.CL.*;
import static org.jocl.CL.CL_TRUE;
import gov.inl.HZGenerator.CLFW;

/**
 * Created by BITINK on 6/16/2017.
 */
public class TestCompact extends TestCase {
	protected void setUp() {
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		assertEquals(CLFW.Initialize(openCLSettings), CL_SUCCESS);
	}

	public void testCompact() {
		int[] input = new int[1000];
		int[] predication = new int[1000];
		int[] address = new int[1000];

		for (int i = 0; i < 1000; ++i) {
			input[i] = i;
			predication[i] = i % 2;
			address[i] = (i+ 1) / 2;
		}

		int[] error = {0};

		cl_mem p_input, p_predication, p_address, p_output;
		p_input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR, 1000 * Sizeof.cl_int,
				Pointer.to(input), error);
		p_predication = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR, 1000 * Sizeof.cl_int,
				Pointer.to(predication), error);
		p_address = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR, 1000 * Sizeof.cl_int,
				Pointer.to(address), error);
		p_output = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE, 1000 * Sizeof.cl_int,
				null, error);

		assertEquals(Compactor.Compact(p_input, Compactor.Type.INT, p_predication, p_address, 1000, p_output), CL_SUCCESS);

		int[] result = new int[1000];
		clEnqueueReadBuffer(CLFW.DefaultQueue, p_output, CL_TRUE, 0, 1000 * Sizeof.cl_int, Pointer.to(result),
				0, null, null );

		for (int i = 0; i < 500; i++) {
			assertEquals((i * 2) + 1, result[i]);
		}

		for (int i = 500; i < 1000; i++) {
			assertEquals((i - 500) * 2, result[i]);
		}
	}
}
