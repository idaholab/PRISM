package gov.inl.HZGenerator.BrickFactory;

import gov.inl.HZGenerator.Kernels.MortonConverter;
import gov.inl.HZGenerator.Kernels.PartitionerResult;
import gov.inl.SIEVAS.hmortonlib.HMorton3D;
import gov.inl.SIEVAS.hmortonlib.Morton3D;
import javafx.util.Pair;
import org.joml.Vector3f;

import java.awt.image.BufferedImage;
import java.awt.image.DataBufferByte;
import java.awt.image.DataBufferUShort;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.util.*;

import static java.lang.Math.log;

/**
 * Created by BITINK on 6/27/2017.
 */
public class VolumeApportioner {

	Boolean verbose = true;

	List<RandomAccessFile> brickFiles;

	public int[] decode(int c, int last_bit_mask) {
		if(c == 0L) {
			return new int[]{0, 0, 0};
		} else {
			c = c << 1 | 1;

			int i = c;
			i |= (i >>  1);
			i |= (i >>  2);
			i |= (i >>  4);
			i |= (i >>  8);
			i |= (i >> 16);
			i = i - (i >> 1);


			c = c * (last_bit_mask / i);
			c &= ~last_bit_mask;
			Morton3D morton = new Morton3D();
			return morton.decode(c);
		}
	}

	public void apportion(Volume volume, PartitionerResult pr, BrickFactorySettings settings) throws IOException {
		Boolean validateCurve = false;
		if (settings.mapTo8BPP)
			allocatePartitions(pr, 1, settings.outputPath);
		else
			allocatePartitions(pr, volume.getBytesPerPixel(), settings.outputPath);

		/* For each slice */
		for (int z = 0; z < volume.getDepth(); ++z) {

			int[] hzPositions = new int[pr.tensorWidth * pr.minDimSize * pr.tensorHeight* pr.minDimSize];
			int[] brick = new int[pr.tensorWidth * pr.minDimSize * pr.tensorHeight* pr.minDimSize];

			/* Curve each layer */
			int error = MortonConverter.curveLayer(pr, z, hzPositions, brick);

			/* Load slice into memory */
			BufferedImage slice = volume.getSlice(z);

			/* TEST CODE */
			if (validateCurve) {
				if (!validate(slice.getWidth(), slice.getHeight(), pr.tensorWidth * pr.minDimSize, z, brick, hzPositions, pr)) {
					System.out.println("Error, something went wrong while curving.");
				}
			}

			if (verbose) System.out.println("Apportioning layer " + z);

			if (volume.getBytesPerPixel() == 1) {
				byte[] rawBytes = ((DataBufferByte) slice.getRaster().getDataBuffer()).getData();
				apportionBytes(slice.getWidth(), slice.getHeight(), pr.tensorWidth * pr.minDimSize, rawBytes, brick, hzPositions, pr);
			} else {
				short[] rawShorts = ((DataBufferUShort) slice.getRaster().getDataBuffer()).getData();
				apportionShorts(slice.getWidth(), slice.getHeight(), pr.tensorWidth * pr.minDimSize, rawShorts, brick, hzPositions, settings.mapTo8BPP);
			}

			if (verbose) System.out.println("Layer " + Integer.toString(z) + " done. ");
		}

		closeFiles();
	}
	private Boolean validate(int width, int height, int paddedWidth, int z, int[] brick, int[] hzPositions, PartitionerResult pr) {
		if (verbose) System.out.println("Validating curve for layer " + z);
		for (int x = 0; x < width; x += 10) {
			for (int y = 0; y < height; y += 10) {
				if (x > width || y > height) continue;
				int brickNumber = brick[x + y * paddedWidth];
				int address = hzPositions[x + y * paddedWidth];
				int brickSize = pr.partitions.get(brickNumber).size;
				int brickSize_ = brickSize;
				int level = 0;
				while (brickSize > 0) {
					brickSize >>= 1;
					++level;
				}
				level -= 1;

				int bits = level * 3;
				int last_bit_mask = 1 << bits;
				Vector3f brickPos = pr.partitions.get(brickNumber).pos;
				Vector3f pos = new Vector3f(x, y, z).sub(brickPos);

				int[] result = decode(address, last_bit_mask);
				if (pos.x != result[0] || pos.y != result[1] || pos.z != result[2]) {
					return false;
				}
			}
		}
		return true;
	}

	private void apportionShorts(int width, int height, int paddedWidth, short[] rawShorts,
								int[] pixelToBrick, int[] pixelToHZ, Boolean scaleTo8) throws IOException
	{
//		Pair<Integer, Pair<Short, Integer>>[] test = new Pair[rawShorts.length];
//
//		for (int x = 0; x < width; ++x) {
//			for (int y = 0; y < height; ++y) {
//				test[x + y * width] = new Pair<>(pixelToBrick[x + y * paddedWidth],
//						new Pair(rawShorts[x + y * width], x + y * paddedWidth));
//			}
//		}
//
//		Arrays.sort(test, (p1, p2) -> {
//			int result = Integer.compare(p1.getKey(), p2.getKey());
////			if (result == 0) {
////				result = Integer.compare(p1.getValue().getValue(), p2.getValue().getValue());
////			}
//			return result;
//		}
//		);
//
//		RandomAccessFile raf = null;
//		int currentBrick = -1;
//		for (int i = 0; i < test.length; ++i) {
//			if (currentBrick != test[i].getKey()) {
//				currentBrick = test[i].getKey();
//				raf = brickFiles.get(currentBrick);
//			}
//
//			if (scaleTo8) {
//				raf.seek(test[i].getValue().getValue());
//				int temp = test[i].getValue().getKey() & 0xffff; // Convert from -32768 - 32767 to range 0 - 65535
//				byte scaled = (byte)((((double)i) / 65535d) * 255d); // remap to 0-255
//				raf.write(scaled);
//			}
//			else {
//				raf.seek(test[i].getValue().getValue());
//				raf.write(test[i].getValue().getKey());
//			}
//		}



		for (int y = 0; y < height; ++y) {
			for (int x = 0; x < width; ++x) {
				short data = rawShorts[x + y * width];
				int brickNumber = pixelToBrick[x + y * paddedWidth];
				int address = pixelToHZ[x + y * paddedWidth];

				RandomAccessFile raf = brickFiles.get(brickNumber);
				if (scaleTo8 == false) {
					raf.seek(address * 2); // 2 bytes per short
					raf.write(data);
				} else
				{
					raf.seek(address);
					int i = data & 0xffff; // Convert from -32768 - 32767 to range 0 - 65535
					byte scaled = (byte)((((double)i) / 65535d) * 255d); // remap to 0-255
					raf.write(scaled);
				}
			}
		}
	}

	private void apportionBytes(int width, int height, int paddedWidth, byte[] rawBytes,
								int[] pixelToBrick, int[] pixelToHZ, PartitionerResult pr) throws IOException
	{
		for (int y = 0; y < height; ++y) {
			for (int x = 0; x < width; ++x) {
				int data = rawBytes[x + y * width];
				int brickNumber = pixelToBrick[x + y * paddedWidth];
				int address = pixelToHZ[x + y * paddedWidth];

				int brickSize = pr.partitions.get(brickNumber).size;
				int brickSize_ = brickSize;
				int level = 0;
				while (brickSize > 0) {
					brickSize >>= 1;
					++level;
				}
				level -= 1;
				int bits = level * 3;
				int last_bit_mask = 1 << bits;
				int[] result = decode(address, last_bit_mask);

				RandomAccessFile raf = brickFiles.get(brickNumber);
				raf.seek(result[0] + brickSize_ * result[1] + brickSize_ * brickSize_ * result[2]);
//				raf.seek(address);
				raf.write(data);
			}
		}
	}

	public void allocatePartitions(PartitionerResult pr, long bytesPerPixel, String outputPath) throws IOException {
		if (verbose) System.out.println("Allocating curved volume");
		brickFiles = new ArrayList<>();

		for (int i = 0; i < pr.partitions.size(); ++i) {
			int size = pr.partitions.get(i).size;
			long bytes = size * size * size * bytesPerPixel;
			RandomAccessFile raf = new RandomAccessFile(outputPath + "/" + i + ".raw", "rw");
			raf.setLength(bytes);
			brickFiles.add(raf);
		}
	}

	public void closeFiles() throws IOException {
		for (int i = 0; i < brickFiles.size(); ++i) {
			RandomAccessFile raf = brickFiles.get(i);
			raf.close();
		}
	}
}
