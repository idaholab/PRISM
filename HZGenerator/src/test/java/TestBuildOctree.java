import gov.inl.HZGenerator.CLFW;
import gov.inl.HZGenerator.Octree.OctNode;
import junit.framework.TestCase;
import org.joml.Vector3i;

import static org.jocl.CL.CL_SUCCESS;

/**
 * Created by Nate on 7/28/2017.
 */
public class TestBuildOctree extends TestCase {
    protected void setUp() {
        assertEquals(CLFW.Initialize("Kernels/OpenCLSettings.json", "Kernels"), CL_SUCCESS);
    }

    public void testBuildOctree() {
        //Given some volume dimensions in pixels
        Vector3i volumeSize = new Vector3i(16, 18, 19);

        int minBrickWidth = 16;

        //When we compute the octree from that size
//        OctNode octree = OctNode.buildOctree(volumeSize, minBrickWidth);
//        System.out.println(octree.toJson());
//        System.out.println();

        //Our results should be valid
    }



}
