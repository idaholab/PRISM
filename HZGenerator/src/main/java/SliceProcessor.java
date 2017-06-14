import javafx.embed.swing.SwingFXUtils;
import javafx.scene.image.WritableImage;
import org.jocl.*;

import java.awt.image.BufferedImage;
import java.awt.image.DataBufferByte;
import java.awt.image.DataBufferUShort;

import static org.jocl.CL.*;
import static org.jocl.CL.CL_MEM_READ_WRITE;
import static org.jocl.CL.CL_MEM_USE_HOST_PTR;

/**
 * Created by BITINK on 6/13/2017.
 */
public class SliceProcessor {
	BufferedImage currentSlice = null;
	private int bytesPerPixel = 1;
	private Boolean masked = false;
	private int threshold = 0;

	/* Constructor */
	public SliceProcessor() {}

	/* Gets and Sets */
	public void setBytesPerPixel(int bytesPerPixel) throws Exception {
		if (bytesPerPixel <= 0 || bytesPerPixel > 2)
			throw new Exception("bytes per pixel must be between 1 and 2 (inclusive)");
		this.bytesPerPixel = bytesPerPixel;
	}

	public int getBytesPerPixel() {
		return bytesPerPixel;
	}

	public void setCurrentSlice (BufferedImage currentSlice) throws Exception {
		if (currentSlice == null)
			throw new Exception("currentSlice cannot be null");
		this.currentSlice = currentSlice;
	}

	public BufferedImage getCurrentSlice() {
		return this.currentSlice;
	}

	public void setMasked(Boolean value) {
		masked = value;
	}

	public Boolean getMask() {return masked;}

	public void setThreshold(int threshold) throws Exception {
		if (threshold < 0) throw new Exception("Threshold must be greater than zero");
		this.threshold = threshold;
	}

	private void uploadSlice(BufferedImage slice, int bytesPerPixel, cl_mem[] buffer) {
		int[] error = new int[1];

		// Setup slice format
		cl_image_format imageFormat = new cl_image_format();
		imageFormat.image_channel_order = CL_R;
		if (bytesPerPixel == 2)
			imageFormat.image_channel_data_type = CL_UNSIGNED_INT16;
		else if (bytesPerPixel == 1)
			imageFormat.image_channel_data_type = CL_UNSIGNED_INT8;

		// Allocate and Upload image to OpenCL
		if (bytesPerPixel == 1) {
			byte[] dataSrc = ((DataBufferByte) currentSlice.getRaster().getDataBuffer()).getData();
			buffer[0] = clCreateImage2D(
					CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
					new cl_image_format[]{imageFormat}, currentSlice.getWidth(), currentSlice.getHeight(),
					0, Pointer.to(dataSrc), error
			);
		} else if (bytesPerPixel == 2) {
			short[] dataSrc = ((DataBufferUShort) currentSlice.getRaster().getDataBuffer()).getData();
			buffer[0] = clCreateImage2D(
					CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
					new cl_image_format[]{imageFormat}, currentSlice.getWidth(), currentSlice.getHeight(),
					0, Pointer.to(dataSrc), error
			);
		}
	}

	private void downloadSlice(cl_mem sourceBuffer, int bytesPerPixel, BufferedImage currentSlice) {
		int[] error = new int[1];

		// Download the image into the current BufferedImage
		if (bytesPerPixel == 1) {
			byte[] dataDst = ((DataBufferByte) currentSlice.getRaster().getDataBuffer()).getData();
			long[] origin = {0,0,0};
			long[] region = {currentSlice.getWidth(), currentSlice.getHeight(), 1};
			error[0] |= clEnqueueReadImage(CLFW.DefaultQueue, sourceBuffer, CL_TRUE, origin, region, 0, 0,
					Pointer.to(dataDst), 0, null, null );
		} else if (bytesPerPixel == 2) {
			short[] dataDst = ((DataBufferUShort) currentSlice.getRaster().getDataBuffer()).getData();
			long[] origin = {0,0,0};
			long[] region = {currentSlice.getWidth(), currentSlice.getHeight(), 1};
			error[0] |= clEnqueueReadImage(CLFW.DefaultQueue, sourceBuffer, CL_TRUE, origin, region, 0, 0,
					Pointer.to(dataDst), 0, null, null );
		}
	}

	public WritableImage getJFXImage() {
		return SwingFXUtils.toFXImage(currentSlice, null);
	}

	/* Image Processing  */
	public void mask() {
		cl_mem[] buffer = {null};
		/* Upload the slice to the GPU */
		uploadSlice(currentSlice, bytesPerPixel, buffer);

		/* Do Kawase blurSlice, then mean threshold. */
		blurSlice(buffer[0], 3, buffer[0]);
		thresholdSlice(buffer[0], threshold, buffer[0]);

		/* Download the slice from the GPU*/
		downloadSlice(buffer[0], bytesPerPixel, currentSlice);
		clReleaseMemObject(buffer[0]);
		clFinish(CLFW.DefaultQueue);
	}

	public void blurSlice(cl_mem input, int passes, cl_mem result) {
		int xSize = currentSlice.getWidth();
		int ySize = currentSlice.getHeight();

		// OpenCL argument data
		int[] error = new int[1];
		cl_kernel kawaseBlurKernel = CLFW.Kernels.get("kawaseBlur");

		int nextPow2 = Integer.max(CLFW.NextPow2(xSize) , CLFW.NextPow2(ySize));
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kawaseBlurKernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);
		int ws = (int)Math.sqrt(workgroupSize[0]);
		long[] globalSize = {nextPow2, nextPow2};
		long[] localSize = {ws, ws};
		long[] width = {xSize};
		long[] height = {ySize};

		// Run kawase blurSlice
		int[] r = {2,3,3,5,7};
		error[0] |= clSetKernelArg(kawaseBlurKernel, 0, Sizeof.cl_int, Pointer.to(width));
		error[0] |= clSetKernelArg(kawaseBlurKernel, 1, Sizeof.cl_int, Pointer.to(height));

		int[] currentRadius = {r[0]};
		error[0] |= clSetKernelArg(kawaseBlurKernel, 2, Sizeof.cl_int, Pointer.to(currentRadius));
		error[0] |= clSetKernelArg(kawaseBlurKernel, 3, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(kawaseBlurKernel, 4, Sizeof.cl_mem, Pointer.to(result));
		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, kawaseBlurKernel, 2, null,
				globalSize, localSize, 0, null, null);

		for (int i = 1; i < passes; ++i) {
			int[] currentR = {r[i]};
			error[0] |= clSetKernelArg(kawaseBlurKernel, 2, Sizeof.cl_int, Pointer.to(currentR));
			error[0] |= clSetKernelArg(kawaseBlurKernel, 3, Sizeof.cl_mem, Pointer.to(result));
			error[0] |= clSetKernelArg(kawaseBlurKernel, 4, Sizeof.cl_mem, Pointer.to(result));
			error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, kawaseBlurKernel, 2, null,
					globalSize, localSize, 0, null, null);
		}
	}

	public void thresholdSlice(cl_mem input, int threshold, cl_mem result) {
		int xSize = currentSlice.getWidth();
		int ySize = currentSlice.getHeight();

		int[] error = {0};
		cl_kernel thresholdMaskKernel = CLFW.Kernels.get("thresholdMask");

		int nextPow2 = Integer.max(CLFW.NextPow2(xSize) , CLFW.NextPow2(ySize));
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(thresholdMaskKernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);
		int ws = (int)Math.sqrt(workgroupSize[0]);
		long[] globalSize = {nextPow2, nextPow2};
		long[] localSize = {ws, ws};
		long[] width = {xSize};
		long[] height = {ySize};

		// Naive threshold
		int[] currentThreshold = {threshold};
		error[0] |= clSetKernelArg(thresholdMaskKernel, 0, Sizeof.cl_int, Pointer.to(width));
		error[0] |= clSetKernelArg(thresholdMaskKernel, 1, Sizeof.cl_int, Pointer.to(height));
		error[0] |= clSetKernelArg(thresholdMaskKernel, 2, Sizeof.cl_int, Pointer.to(currentThreshold));
		error[0] |= clSetKernelArg(thresholdMaskKernel, 3, Sizeof.cl_mem, Pointer.to(input));
		error[0] |= clSetKernelArg(thresholdMaskKernel, 4, Sizeof.cl_mem, Pointer.to(result));
		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, thresholdMaskKernel, 2, null,
				globalSize, localSize, 0, null, null);
	}

	/* All images are processed before being curved. */
	public void processSlice() {
		if (masked) mask();
	}
}
