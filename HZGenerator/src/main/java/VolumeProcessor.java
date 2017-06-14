import ij.ImagePlus;
import ij.io.Opener;
import ij.process.ImageStatistics;
import ij.process.ShortProcessor;
import javafx.scene.image.ImageView;
import javafx.stage.DirectoryChooser;
import javafx.stage.FileChooser;
import javafx.stage.Stage;
import org.jocl.*;

import java.awt.image.BufferedImage;
import java.awt.image.DataBufferByte;
import java.io.*;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import static org.jocl.CL.*;

/**
 * Created by BITINK on 6/7/2017.
 */
public class VolumeProcessor implements Runnable {
	private Boolean rawMode = false;
	private File selectedLocation;
	private File[] files;

	private int xWidth, yWidth, zWidth;
	private int xBricks, yBricks, zBricks;
	private int dimWidth;
	private int maxDimWidth;
	private int minDimWidth;
	private int totalLevels;
	private int totalBricks;

	private int currentSliceIndx = 0;
	private SliceProcessor sliceProcessor;

	/* -- Constructor -- */
	public VolumeProcessor() {
		sliceProcessor = new SliceProcessor();
	};

	/* -- Gets and Sets -- */

	/* Raw mode only: sets the x, y, and z dimensions of the raw volume. */
	public void setDimensions(int x, int y, int z) {
		if (rawMode) {
			setDimensions_(x, y, z);
		}
	}

	/* Private to prevent invalid input for tiff stacks. */
	private void setDimensions_(int x, int y, int z) {
		xWidth = x;
		yWidth = y;
		zWidth = z;

		int x2 = CLFW.NextPow2(xWidth);
		int y2 = CLFW.NextPow2(yWidth);
		int z2 = CLFW.NextPow2(zWidth);

		long minDim = Integer.min(Integer.min(x2, y2), z2);
		long maxDim = Integer.max(Integer.min(x2, y2), z2);
		long numPixels = maxDim * maxDim * maxDim;
		totalLevels = (int)( Math.log(numPixels)/Math.log(8) );
		totalBricks = (int)((x2 / minDim) * (y2 / minDim) * (z2 / minDim));

		dimWidth = (int)minDim;
		xBricks = x2 / (int)minDim;
		yBricks = y2 / (int)minDim;
		zBricks = z2 / (int)minDim;
	}

	/* Get the names of each slice. (in raw mode, slice names are just numbers) */
	public List<String> getSliceNames() {
		List<String> paths = new ArrayList<>();
		if (rawMode) {
			if (yWidth <=0 ) return null;
			for (int i = 0; i < yWidth; ++i) {
				paths.add(Integer.toString(i));
			}
		} else {
			if (selectedLocation == null) return null;
			if (files.length <= 0) return null;
			for (File file : files) {
				paths.add(file.getAbsolutePath());
			}
		}
		return paths;
	}

	public Boolean inRawMode() {
		if (rawMode == true && selectedLocation != null)
			return true;
		return false;
	}

	public String getCurvedFileSize() {
		long totalBytes = (long)totalBricks * (long)dimWidth * (long)dimWidth *
				(long)dimWidth * (long)sliceProcessor.getBytesPerPixel();
		double temp = totalBytes;
		String unit = "B";
		if (temp > 1000) {unit = "KB"; temp /= 1000;}
		if (temp > 1000) {unit = "MB"; temp /= 1000;}
		if (temp > 1000) {unit = "GB"; temp /= 1000;}
		if (temp > 1000) {unit = "TB"; temp /= 1000;}
		return(Math.round(temp * 1000.f) / 1000.f + " " + unit);
	}

	public int getNumLevels() {
		if (totalLevels <= 0) return -1;
		return totalLevels;
	}

	public int getNumBricks() {
		if (totalBricks <= 0) return -1;
		return totalBricks;
	}

	public int getDimWidth() {
		if (dimWidth<= 0) return -1;
		return dimWidth;
	}

	public int[] getNumBricksPerDim() {
		if (xBricks <= 0 || yBricks <= 0 || zBricks <= 0) return new int[3];
		int[] temp = {xBricks, yBricks, zBricks};
		return temp;
	}

	public void setSlice(int slice) {
		if (slice < yWidth)
			currentSliceIndx = slice;
	}

	public void setMaxDimWidth(int width) {maxDimWidth = width;}

	public void setMinDimWidth(int width) {minDimWidth = width;}

	/* Updates the preview to show the current slice. Returns -1 if there was an error. */
	public int updatePreview(ImageView preview, Boolean masked) {
		if (selectedLocation == null) { return -1; }

		sliceProcessor.setMasked(masked);

		if (rawMode)
			addRawToSliceProcessor();
		else
			addTiffToSliceProcessor(masked);

		sliceProcessor.processSlice();
		preview.setImage(sliceProcessor.getJFXImage());
		return 0;
	}

	/* -- File IO -- */
	public String open(String type) throws Exception {
		selectedLocation = null;

		rawMode = false;
		if (type.compareTo("TIFF") == 0) {
			DirectoryChooser dc = new DirectoryChooser();
			selectedLocation = dc.showDialog(new Stage());

			files = selectedLocation.listFiles((dir, name) -> name.endsWith(".tiff") || name.endsWith(".tif"));
			if (files.length <= 0) {
				selectedLocation = null;
				return null;
			}
			Arrays.sort(files);
			Opener opener = new Opener();
			ImagePlus tempImage = opener.openImage(files[0].getPath());

			setDimensions_(tempImage.getWidth(), files.length, tempImage.getHeight());
			sliceProcessor.setBytesPerPixel(tempImage.getBytesPerPixel());
		} else if (type.compareTo("RAW") == 0) {
			rawMode = true;

			FileChooser.ExtensionFilter extFilterRAW =
					new FileChooser.ExtensionFilter("RAW files (*.raw)", "*.raw", "*.RAW");

			FileChooser fc = new FileChooser();
			fc.getExtensionFilters().add(extFilterRAW);
			selectedLocation = fc.showOpenDialog(new Stage());
			sliceProcessor.setBytesPerPixel(1);
		}

		if (selectedLocation == null) return null;
		else return selectedLocation.getAbsolutePath();
	};

	/* -- Volume Processing -- */

	public void curve() {};

	public void uncurve() {};

	private void addRawToSliceProcessor() {
		try {
			int yOffset = (currentSliceIndx * xWidth * zWidth);

			//  TODO: add support for type_ushort_gray
			BufferedImage currentSlice = new BufferedImage(xWidth, zWidth, BufferedImage.TYPE_BYTE_GRAY);
			byte[] rawBytes = ((DataBufferByte) currentSlice.getRaster().getDataBuffer()).getData();
			RandomAccessFile raf = new RandomAccessFile(selectedLocation, "r");
			raf.seek(yOffset);
			raf.read(rawBytes, 0, xWidth*zWidth);

			sliceProcessor.setCurrentSlice(currentSlice);
		} catch (Exception e) {
			System.out.println("Something went wrong while updating raw image.");
		};
	}

	private void addTiffToSliceProcessor(Boolean masked) {
		try {
			/* Open the Tiff */
			Opener opener = new Opener();
			ImagePlus openedImage = opener.openImage(files[currentSliceIndx].getPath());

			BufferedImage currentSlice;

			/* Have ImageJ convert to the appropriate buffered image */
			if (openedImage.getBytesPerPixel() == 2) {
				ShortProcessor sp = (ShortProcessor)openedImage.getProcessor();
				currentSlice = sp.get16BitBufferedImage();
			} else {
				currentSlice = openedImage.getBufferedImage();
			}
			sliceProcessor.setCurrentSlice(currentSlice);
			if (sliceProcessor.getMask()) {
				/* Compute the mean pixel value for masking */
				ImageStatistics stats = ImageStatistics.getStatistics(
						openedImage.getProcessor(), ImageStatistics.MEAN,
						openedImage.getCalibration());
				double mean = stats.mean;
				sliceProcessor.setThreshold((int)mean);
			}

		} catch (Exception e) {
			System.out.println(e);
		}
	}

	@Override
	public void run() {
		// Todo paralelize this
		// For each brick
		for (int xb = 0; xb < xBricks; ++xb) {
			for (int yb = 0; yb < yBricks; ++yb) {
				for (int zb = 0; zb < zBricks; ++zb) {

					// For each slice in the current brick
//					for (int y = 0; y <  = 0; )

				}
			}
		}
	}
}
