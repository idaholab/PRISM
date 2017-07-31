package gov.inl.HZGenerator.BrickFactory;

import gov.inl.HZGenerator.Kernels.MortonConverter;
import gov.inl.HZGenerator.Kernels.PartitionerResult;

import java.awt.*;
import java.awt.image.BufferedImage;
import java.awt.image.DataBufferByte;
import java.awt.image.DataBufferUShort;
import java.io.IOException;
import java.nio.MappedByteBuffer;
import java.nio.channels.FileChannel;
import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.*;

import java.util.List;
import java.util.stream.IntStream;

/**
 * Nathan Morrical. Summer 2017.
 * The following class uses the original volume and the results of a volume partitioner to calculate Hierarchical Z-Order
 * 	indices for each pixel in a slice by slice fashion.
 */
public class VolumeApportioner {
	/* For debugging */
	Boolean verbose = true;
	public Boolean done = false;
	public int totalProcessed;

	/* We use mapped byte buffers to maximize disk write performance */
	List<FileChannel> brickChannels;
	List<MappedByteBuffer> brickBuffers;

	/* Distributes the volume into curved bricks, saving the results to the specified destination */
	public void apportion(Volume volume, PartitionerResult pr, BrickFactorySettings settings) throws IOException {
		done = false;
		totalProcessed = 0;

		/* Allocate the volume bricks */
		if (verbose) System.out.println("Allocating curved volume");
		if (settings.mapTo8BPP)
			allocatePartitions(pr, 1, settings.outputPath);
		else
			allocatePartitions(pr, volume.getBytesPerPixel(), settings.outputPath);

		/* Curve each brick */
		IntStream.range(0, pr.partitions.size()).forEach(i -> {
				try {
					if (verbose) System.out.println("Apportioning partition " + i);
					Brick p = pr.partitions.get(i);
					if (volume.bytesPerPixel == 1)
							apportion8BitPartition(volume, p, i);
					else if (volume.bytesPerPixel == 2)
						apportion16BitPartition(volume, p, i, settings.mapTo8BPP);
					totalProcessed++;
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		);
		if (verbose) System.out.println("Done");
		done = true;
	}

	/* Allocates the curved bricks on disk, opening each file and placing it in the list of files */
	public void allocatePartitions(PartitionerResult pr, long bytesPerPixel, String outputPath) throws IOException {
		brickChannels = new ArrayList<>();
		brickBuffers = new ArrayList<>();

		for (int i = 0; i < pr.partitions.size(); ++i) {
			int size = pr.partitions.get(i).size;
			long bytes = size * size * size * bytesPerPixel;
			Path path = FileSystems.getDefault().getPath(outputPath + "/" + i + ".hz");
			brickChannels.add(FileChannel.open(path,
					StandardOpenOption.CREATE,
					StandardOpenOption.READ,
					StandardOpenOption.WRITE));
			brickBuffers.add(brickChannels.get(i).map(FileChannel.MapMode.READ_WRITE, 0, bytes));
		}
	}

	public void apportion8BitPartition(Volume v, Brick p, int pid) throws IOException {
		byte[] raw = new byte[p.size * p.size * p.size];
		DataBufferByte[] dataBuffers = new DataBufferByte[p.size];

		/* Load slices into the input in parallel */
		IntStream.range((int)p.position.z, Integer.min((int)p.position.z + p.size, v.depth)).parallel().forEach(i -> {
			BufferedImage subImage = new BufferedImage(p.size, p.size, BufferedImage.TYPE_BYTE_GRAY);
			Graphics g = subImage.getGraphics();

			/* Get original image */
			BufferedImage image = v.getSlice(i);

			/* Draw original onto new */
			g.drawImage(image, 0, 0, p.size, p.size, (int)p.position.x, (int)p.position.y,
					(int)p.position.x + p.size, (int)p.position.y + p.size, null);

			/* Save data buffer */
			dataBuffers[(int) (i - p.position.z)] = (DataBufferByte) subImage.getRaster().getDataBuffer();
			g.dispose();
		});
		IntStream.range(0, Integer.min(p.size, v.depth)).forEach(i
				-> System.arraycopy(dataBuffers[i].getData(), 0, raw, p.size * p.size * i, p.size * p.size));

		/* Curve the brick */
		byte[] curved = new byte[p.size * p.size * p.size];
		MortonConverter.curveByteVolume(p.size, raw, curved);

		/* Write the brick out to file */
		brickBuffers.get(pid).put(curved);
		brickBuffers.get(pid).force();
		brickChannels.get(pid).close();
	}

	public void apportion16BitPartition(Volume v, Brick p, int pid, Boolean mapTo8) throws IOException {
		short[] raw = new short[p.size * p.size * p.size];
		DataBufferUShort[] dataBuffers = new DataBufferUShort[p.size];

		/* Load slices into the input in parallel */
		IntStream.range((int)p.position.z, Integer.min((int)p.position.z + p.size, v.depth)).parallel().forEach(i -> {
			BufferedImage subImage = new BufferedImage(p.size, p.size, BufferedImage.TYPE_USHORT_GRAY);
			Graphics g = subImage.getGraphics();

			/* Get original image */
			BufferedImage image = v.getSlice(i);

			/* Draw original onto new */
			g.drawImage(image, 0, 0, p.size, p.size, (int)p.position.x, (int)p.position.y,
					(int)p.position.x + p.size, (int)p.position.y + p.size, null);

			/* Save data buffer */
			dataBuffers[(int)(i - p.position.z)] = (DataBufferUShort) subImage.getRaster().getDataBuffer();
			g.dispose();
		});
		IntStream.range(0, Integer.min(p.size, v.depth)).forEach(i
				-> System.arraycopy(dataBuffers[i].getData(), 0, raw, p.size * p.size * i, p.size * p.size));

		/* Curve the brick */
		MappedByteBuffer bb = brickBuffers.get(pid);
		if (mapTo8) {
			byte[] curved = new byte[p.size * p.size * p.size];
			MortonConverter.curveShortToByteVolume(p.size, raw, curved);

			/* Write the brick out to file */
			bb.put(curved);
			bb.force();

		} else {
			short[] curved = new short[p.size * p.size * p.size];
			MortonConverter.curveShortVolume(p.size, raw, curved);

			/* Write the brick out to file */
			bb.asShortBuffer().put(curved);
			bb.force();
		}
		brickChannels.get(pid).close();
	}
}
