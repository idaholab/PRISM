import gov.inl.HZGenerator.CLFW;
import gov.inl.HZGenerator.Kernels.Reducer;
import junit.framework.TestCase;
import org.jocl.Pointer;
import org.jocl.Sizeof;
import org.jocl.cl_image_format;
import org.jocl.cl_mem;

import javax.imageio.ImageIO;
import javax.swing.*;
import java.awt.*;
import java.awt.image.BufferedImage;
import java.awt.image.DataBufferByte;
import java.awt.image.DataBufferUShort;
import java.io.File;
import java.io.IOException;
import static org.jocl.CL.*;

/**
 * Created by BITINK on 6/14/2017.
 */
public class TestReduce extends TestCase {
	protected void setUp() {
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		assertEquals(CLFW.Initialize(openCLSettings), CL_SUCCESS);
	}

	public void testReduce() {
		int[] data = new int[1000];

		for (int i = 0; i < 1000; ++i) {
			data[i] = 1;
		}

		int[] error = {0};

		cl_mem input;
		input = clCreateBuffer(CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR, 1000 * Sizeof.cl_int,
				Pointer.to(data), error);
		int[] result = new int[1];

		Reducer.addInts(input, 1000, result);

		assertEquals(result[0], 1000);
	}

	public void testReduceImage() throws IOException, InterruptedException {
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
		Reducer.addImage(clSrc, src.getWidth(), src.getHeight(), output);

//		JFrame frame = new JFrame();
//		frame.getContentPane().setLayout(new FlowLayout());
//		ImageIcon icon = new ImageIcon(src);
//		Image image = icon.getImage();
//		image = image.getScaledInstance(1024, 1024, Image.SCALE_SMOOTH);
//		icon = new ImageIcon(image);
//		frame.getContentPane().add(new JLabel(icon));
//		frame.pack();
//		frame.setVisible(true);
//		Thread.sleep(100);
	}
}
