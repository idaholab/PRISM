import gov.inl.HZGenerator.CLFW;
import gov.inl.HZGenerator.Kernels.Blurrer;
import gov.inl.HZGenerator.Kernels.Reducer;
import junit.framework.TestCase;
import org.jocl.Pointer;
import org.jocl.cl_image_format;
import org.jocl.cl_mem;

import javax.imageio.ImageIO;
import javax.swing.*;
import java.awt.*;
import java.awt.image.BufferedImage;
import java.awt.image.DataBufferUShort;
import java.io.File;
import java.io.IOException;

import static org.jocl.CL.*;
import static org.jocl.CL.CL_MEM_USE_HOST_PTR;
import static org.jocl.CL.CL_SUCCESS;

/**
 * Nathan Morrical. Summer 2017.
 */
public class TestBlur extends TestCase {
	protected void setUp() {
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		assertEquals(CLFW.Initialize(openCLSettings), CL_SUCCESS);
	}

	public void testKawaseBlur() throws IOException, InterruptedException {
		File f = new File(getClass().getClassLoader().getResource("TestImage.tif").getPath());
		BufferedImage src = ImageIO.read(f);

		int[] error = {0};

		short[] dataSrc = ((DataBufferUShort) src.getRaster().getDataBuffer()).getData();
		cl_image_format imageFormat = new cl_image_format();
		imageFormat.image_channel_order = CL_R;
		imageFormat.image_channel_data_type = CL_UNSIGNED_INT16;

		// Allocate/Upload image to OpenCL
		cl_mem clSrc = clCreateImage2D(
				CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
				new cl_image_format[]{imageFormat}, src.getWidth(), src.getHeight(),
				0, Pointer.to(dataSrc), error
		);
		assertEquals(error[0], CL_SUCCESS);

		long[] output = {0};
		error[0] |= Blurrer.KawaseBlur(clSrc, src.getWidth(), src.getHeight(), 4);
		long[] origin = {0,0,0};
		long[] region = {src.getWidth(), src.getHeight(), 1};
		error[0] |= clEnqueueReadImage(CLFW.DefaultQueue, clSrc, CL_TRUE, origin, region, 0, 0,
				Pointer.to(dataSrc), 0, null, null );

		JFrame frame = new JFrame();
		frame.getContentPane().setLayout(new FlowLayout());
		ImageIcon icon = new ImageIcon(src);
		Image image = icon.getImage();
		image = image.getScaledInstance(1024, 1024, Image.SCALE_SMOOTH);
		icon = new ImageIcon(image);
		frame.getContentPane().add(new JLabel(icon));
		frame.pack();
		frame.setVisible(true);
		Thread.sleep(100);
	}
}