import junit.framework.TestCase;

import java.io.File;

import static org.jocl.CL.*;
import gov.inl.HZGenerator.CLFW;

/**
 * Created by BITINK on 6/14/2017.
 */
public class TestGenerateBricks extends TestCase {
	protected void setUp() {
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		assertEquals(CLFW.Initialize(openCLSettings), CL_SUCCESS);
	}

	public void testGenerateBricks() {
//		assertEquals(Partitioner.addInts(9, 7, 5, 2, 16), true);
	}
}
