package gov.inl.HZGenerator.BrickFactory;

import java.awt.image.BufferedImage;
import java.io.IOException;
import java.util.List;

/**
 * Nate Morrical. Summer 2017
 *
 * The following class orchestrates the construction of a curved volume from various different formats
 */
public class BrickFactory {
	public Volume volume;
	private List<Partition> partitions;

	/* Stores settings used to generate bricks */
	public BrickFactorySettings settings;

	/* Used to perform image processing per slice */
	private SliceProcessor sliceProcessor;

	/* Splits up a raw into bricks */
	private VolumePartitioner volumePartitioner;

	/* Uses the volume and partitions to curve data */
	private VolumeApportioner volumeApportioner;

	public BrickFactory() {
		settings = new BrickFactorySettings();
		sliceProcessor = new SliceProcessor();
		volumePartitioner = new VolumePartitioner();
		volumeApportioner = new VolumeApportioner();
	}

	/* Opens a stack of diffs given a tiff directory */
	public List<String> openTiff(String path) {
		try {
			volume = new TiffStack(path);
		} catch (Exception e) {
			return null;
		}
		return (volume == null) ? null : volume.getSliceList();
	}

	/* Opens a raw with the provided dimensions */
	public List<String> openRaw(String path, int width, int height, int depth, int bitsPerPixel) {
		try {
			volume = new RawVolume(path, width, height, depth, bitsPerPixel);
		} catch (Exception e) {
			return null;
		}
		return (volume == null) ? null : volume.getSliceList();
	}

	/* Updates all components to use the new brick factory settings */
	public void updateSettings(BrickFactorySettings newSettings) {
		settings = newSettings;
	}

	/* Processes slices, then curves optimized partitions of the volume. Saves at settings.outputpath */
	public void generateBricks() {
		/* Partition the volume data, which can be read using the generated json file*/
		volumePartitioner.partition(volume, settings);
		try {
			volumePartitioner.saveJson(settings.outputPath);
		} catch (IOException e) {
			e.printStackTrace();
		}

		try {
			volumeApportioner.apportion(volume, volumePartitioner.pr, settings);
		} catch (IOException e) {
			e.printStackTrace();
		}
	}

	/* Returns a slice after it's been ran through the slice processor. */
	public BufferedImage getProcessedSlice(int sliceIdx) {
		return SliceProcessor.process(volume.getSlice(sliceIdx), settings);
	}

	/* Returns the volume partitions */
	public List<Partition> getPartitions() {
		/* First, if no volume was loaded, just return null. */
		if (volume == null) return null;

		/* Otherwise, addInts the loaded volume through the volume partitioner */
		return volumePartitioner.partition(volume, settings);
	}

	/* Returns the file size for the curved volume. */
	public String getResultFileSize() throws Exception {
		if (volume == null) throw new Exception("No volume was loaded. unable to get curved file size.");
		List<Partition> partitions = volumePartitioner.partition(volume, settings);
		long totalPixels = 0;
		for (Partition p : partitions) {
			totalPixels += Math.pow(p.size, 3);
		}

		long totalBytes = (settings.mapTo8BPP) ? totalPixels : totalPixels * volume.getBytesPerPixel();
		double temp = totalBytes;
		String unit = "B";
		if (temp > 1000) {unit = "KB"; temp /= 1000;}
		if (temp > 1000) {unit = "MB"; temp /= 1000;}
		if (temp > 1000) {unit = "GB"; temp /= 1000;}
		if (temp > 1000) {unit = "TB"; temp /= 1000;}
		return(Math.round(temp * 1000.f) / 1000.f + " " + unit);
	}
}
