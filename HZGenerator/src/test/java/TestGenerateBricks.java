import junit.framework.TestCase;

import java.io.File;

import static org.jocl.CL.*;
import gov.inl.HZGenerator.CLFW;

/**
 * Created by BITINK on 6/14/2017.
 */
public class TestGenerateBricks extends TestCase {
	protected void setUp() {
		assertEquals(CLFW.Initialize("Kernels/OpenCLSettings.json", "Kernels"), CL_SUCCESS);
	}

	public void testGenerateBricks() {
//		assertEquals(Partitioner.addInts(9, 7, 5, 2, 16), true);
	}
}
