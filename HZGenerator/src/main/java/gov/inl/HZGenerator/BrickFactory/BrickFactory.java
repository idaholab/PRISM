package gov.inl.HZGenerator.BrickFactory;

import java.io.IOException;
import java.nio.ByteOrder;
import java.util.List;

/**
 * Nate Morrical. Summer 2017
 *
 * The following class constructs a curved volume from various different inputs
 */
public class BrickFactory {
	/* The currently uncurved volume */
	public Volume volume;

	/* Stores settings used to generate bricks */
	public BrickFactorySettings settings;

	/* Splits up a raw into bricks */
	private VolumeBricker volumePartitioner;

	/* Uses the volume and bricks to curve data */
	private VolumeApportioner volumeApportioner;

	/* Constructor */
	public BrickFactory() {
		settings = new BrickFactorySettings();
		volumePartitioner = new VolumeBricker();
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
	public List<String> openRaw(String path, int width, int height, int depth, int bitsPerPixel, ByteOrder byteOrder) {
		try {
			volume = new RawVolume(path, width, height, depth, bitsPerPixel, byteOrder);
		} catch (Exception e) {
			return null;
		}
		return (volume == null) ? null : volume.getSliceList();
	}

	/* Processes slices, then curves optimized bricks of the volume. Saves at settings.outputpath */
	public void generateBricks() {
		volumePartitioner.bytesPerPixel = settings.mapTo8BPP ? 1 : volume.bytesPerPixel;

		/* Brick the volume data, which can be read using the generated json file*/
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

	public void generateBricksAsync() {
		volumeApportioner.done = false;
		Thread t = new Thread(() -> generateBricks());
		t.setDaemon(true);
		t.start();
	}

	/* Returns the volume bricks */
	public List<Brick> getBrickPartitions() {
		/* First, if no volume was loaded, just return null. */
		if (volume == null) return null;

		/* Otherwise, addInts the loaded volume through the volume partitioner */
		return volumePartitioner.partition(volume, settings).bricks;
	}

	/* Returns the file size for the curved volume. */
	public String getResultFileSize() throws Exception {
		if (volume == null) throw new Exception("No volume was loaded. unable to get curved file size.");
		List<Brick> partitions = volumePartitioner.partition(volume, settings).bricks;
		long totalPixels = 0;
		for (Brick p : partitions) {
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

	public int getTotalBricksApportioned() {
		return volumeApportioner.totalProcessed;
	}

	public Boolean getApportionerStatus() {
		return volumeApportioner.done;
	}
}
