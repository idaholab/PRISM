package gov.inl.HZGenerator.Kernels;

import org.jocl.Pointer;
import org.jocl.Sizeof;
import org.jocl.*;
import org.jocl.CL.*;

import javax.imageio.ImageIO;
import java.awt.*;
import java.awt.geom.Point2D;
import java.awt.geom.Rectangle2D;
import java.awt.image.*;
import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;

import static java.lang.Math.ceil;
import static org.jocl.CL.*;
import static org.jocl.CL.clReleaseMemObject;
import gov.inl.HZGenerator.CLFW;

/**
 * Created by BITINK on 5/24/2017.
 */
public class Blurrer {

	public int passes = 2;
//	public int threshold = 0;

//	// Currently supports byte_gray images
//	@Override
//	public BufferedImage filter(BufferedImage src, BufferedImage dst)
//	{
//		long start = System.nanoTime();
//		// Validity checks for the given images
//		if (src.getType() != BufferedImage.TYPE_BYTE_GRAY)
//			throw new IllegalArgumentException( "Source image is not TYPE_BYTE_GRAY");
//		if (dst == null)
//			dst = createCompatibleDestImage(src, null);
//		else if (dst.getType() != BufferedImage.TYPE_BYTE_GRAY)
//			throw new IllegalArgumentException( "Destination image is not TYPE_BYTE_GRAY");
//		if (src.getWidth() != dst.getWidth() || src.getHeight() != dst.getHeight())
//			throw new IllegalArgumentException( "Images do not have the same size");
//
//		// OpenCL argument data
//		int[] error = new int[1];
//		cl_kernel kawaseBlurKernel = CLFW.Kernels.get("kawaseBlur");
//		cl_kernel thresholdMaskKernel = CLFW.Kernels.get("thresholdMask");
//		cl_mem clSrc;
//		int nextPow2 = (src.getWidth() > src.getHeight()) ?
//				Integer.highestOneBit(src.getWidth()) : Integer.highestOneBit(src.getHeight());
//		nextPow2 <<= 1;
//		int[] workgroupSize = {0};
//		clGetKernelWorkGroupInfo(kawaseBlurKernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
//				Pointer.to(workgroupSize), null);
//		int ws = (int)Math.sqrt(workgroupSize[0]);
//		long[] globalSize = {nextPow2, nextPow2};
//		long[] localSize = {ws, ws};
//		long[] width = {src.getWidth()};
//		long[] height = {src.getHeight()};
//
//		// Get the byte data from the image.
//		byte[] dataSrc = ((DataBufferByte) src.getRaster().getDataBuffer()).getData();
//		cl_image_format imageFormat = new cl_image_format();
//		imageFormat.image_channel_order = CL_R;
//		imageFormat.image_channel_data_type = CL_UNSIGNED_INT8;
//
//		// Allocate/Upload image to OpenCL
//		clSrc = clCreateImage2D(
//			CLFW.DefaultContext, CL_MEM_READ_WRITE | CL_MEM_USE_HOST_PTR,
//				new cl_image_format[]{imageFormat}, src.getWidth(), src.getHeight(),
//				0, Pointer.to(dataSrc), error
//		);
//
//		// Run kawase blurSlice
//		int[] r = {2,3,3,5,7};
//		error[0] |= clSetKernelArg(kawaseBlurKernel, 0, Sizeof.cl_int, Pointer.to(width));
//		error[0] |= clSetKernelArg(kawaseBlurKernel, 1, Sizeof.cl_int, Pointer.to(height));
//		for (int i = 0; i < passes; ++i) {
//			int[] currentR = {r[i]};
//			error[0] |= clSetKernelArg(kawaseBlurKernel, 2, Sizeof.cl_int, Pointer.to(currentR));
//			error[0] |= clSetKernelArg(kawaseBlurKernel, 3, Sizeof.cl_mem, Pointer.to(clSrc));
//			error[0] |= clSetKernelArg(kawaseBlurKernel, 4, Sizeof.cl_mem, Pointer.to(clSrc));
//			error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, kawaseBlurKernel, 2, null,
//					globalSize, localSize, 0, null, null);
//		}
//
//		// Naive threshold
//		int[] currentThreshold = {threshold};
//		error[0] |= clSetKernelArg(thresholdMaskKernel, 0, Sizeof.cl_int, Pointer.to(width));
//		error[0] |= clSetKernelArg(thresholdMaskKernel, 1, Sizeof.cl_int, Pointer.to(height));
//		error[0] |= clSetKernelArg(thresholdMaskKernel, 2, Sizeof.cl_int, Pointer.to(currentThreshold));
//		error[0] |= clSetKernelArg(thresholdMaskKernel, 3, Sizeof.cl_mem, Pointer.to(clSrc));
//		error[0] |= clSetKernelArg(thresholdMaskKernel, 4, Sizeof.cl_mem, Pointer.to(clSrc));
//		error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, thresholdMaskKernel, 2, null,
//				globalSize, localSize, 0, null, null);
//
//		// Read back the blurred image.
//		byte[] dataDst = ((DataBufferByte) dst.getRaster().getDataBuffer()).getData();
//		long[] origin = {0,0,0};
//		long[] region = {src.getWidth(), src.getHeight(), 1};
//		error[0] |= clEnqueueReadImage(CLFW.DefaultQueue, clSrc, CL_TRUE, origin, region, 0, 0,
//				Pointer.to(dataDst), 0, null, null );
//		clReleaseMemObject(clSrc);
//		clFinish(CLFW.DefaultQueue);
//		long end = System.nanoTime();
//		//System.out.println("total time " + (end - start) / 1000000 + "ms");
//
//		return dst;
//	}

	public static int KawaseBlur(cl_mem clSrc, int width, int height, int passes) {
		int[] error = new int[1];
		cl_kernel kawaseBlurKernel = CLFW.Kernels.get("KawaseBlur");

		int nextPow2 = (width > height) ?
				Integer.highestOneBit(width) : Integer.highestOneBit(height);
		nextPow2 <<= 1;
		int[] workgroupSize = {0};
		clGetKernelWorkGroupInfo(kawaseBlurKernel, CLFW.DefaultDevice, CL_KERNEL_WORK_GROUP_SIZE, Sizeof.size_t,
				Pointer.to(workgroupSize), null);
		int ws = (int)Math.sqrt(workgroupSize[0]);
		long[] globalSize = {nextPow2, nextPow2};
		long[] localSize = {ws, ws};

		// Run kawase blurSlice
		int[] r = {2,3,3,5,7};
		error[0] |= clSetKernelArg(kawaseBlurKernel, 0, Sizeof.cl_int, Pointer.to(new int[]{width}));
		error[0] |= clSetKernelArg(kawaseBlurKernel, 1, Sizeof.cl_int, Pointer.to(new int[]{height}));
		for (int i = 0; i < passes; ++i) {
			int[] currentR = {r[i]};
			error[0] |= clSetKernelArg(kawaseBlurKernel, 2, Sizeof.cl_int, Pointer.to(currentR));
			error[0] |= clSetKernelArg(kawaseBlurKernel, 3, Sizeof.cl_mem, Pointer.to(clSrc));
			error[0] |= clSetKernelArg(kawaseBlurKernel, 4, Sizeof.cl_mem, Pointer.to(clSrc));
			error[0] |= clEnqueueNDRangeKernel(CLFW.DefaultQueue, kawaseBlurKernel, 2, null,
					globalSize, localSize, 0, null, null);
		}

		return error[0];
	}
}
