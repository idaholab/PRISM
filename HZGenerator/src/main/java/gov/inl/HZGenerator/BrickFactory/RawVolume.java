package gov.inl.HZGenerator.BrickFactory;

import java.awt.image.BufferedImage;
import java.awt.image.DataBufferByte;
import java.awt.image.DataBufferShort;
import java.awt.image.DataBufferUShort;
import java.io.File;
import java.io.RandomAccessFile;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.List;

/**
 * Nathan Morrical. Summer 2017.
 * A raw volume is a single file volume who's width, height, depth, and bits per pixel must be provided for interpreting
 */
public class RawVolume extends Volume {
	File rawFile;

	/* Constructor */
	public RawVolume(String path, int width, int height, int depth, int bitsPerPixel, ByteOrder byteOrder) throws Exception {
		rawFile = new File(path);
		if (rawFile == null) throw new Exception("invalid raw path provided");
		if (width <= 0 || height <= 0 || depth <= 0)
			throw new Exception("invalid volume dimensions");
		if (!(bitsPerPixel == 8 || bitsPerPixel == 16))
			throw new Exception("invalid bits per pixel. RawVolume only supports 8 or 16 bpp");
		this.width = width;
		this.height = height;
		this.depth = depth;
		this.bytesPerPixel = bitsPerPixel / 8;
		this.byteOrder = byteOrder;
	}

	/* Seeks to an offset dependent on width and height, and then reads either shorts or bytes to a buffered image */
	@Override public BufferedImage getSlice(int i) {
		try {
			int zOffset = (i * width * height) * bytesPerPixel;
			int type = (bytesPerPixel == 1) ? BufferedImage.TYPE_BYTE_GRAY : BufferedImage.TYPE_USHORT_GRAY;

			if (type == BufferedImage.TYPE_BYTE_GRAY) {
				BufferedImage currentSlice = new BufferedImage(width, height, BufferedImage.TYPE_BYTE_GRAY);
				byte[] rawBytes = ((DataBufferByte) currentSlice.getRaster().getDataBuffer()).getData();
				RandomAccessFile raf = new RandomAccessFile(rawFile, "r");
				raf.seek(zOffset);
				raf.read(rawBytes, 0, width * height);
				return currentSlice;
			} else if (type == BufferedImage.TYPE_USHORT_GRAY) {
				BufferedImage currentSlice = new BufferedImage(width, height, BufferedImage.TYPE_USHORT_GRAY);
				short[] rawBytes = ((DataBufferUShort) currentSlice.getRaster().getDataBuffer()).getData();
				RandomAccessFile raf = new RandomAccessFile(rawFile, "r");
				raf.seek(zOffset);
				byte[] data = new byte[2 * width * height];
				raf.read(data, 0, 2 * width * height);
				ByteBuffer bb = ByteBuffer.wrap(data).order(this.byteOrder);
				for (int j = 0; j < width * height; ++j)
					rawBytes[j] = (short)(((data[2 * j + 0] & 0xff) << 8) + (data[2 * j +  1] & 0xff));
				return currentSlice;
			}
		} catch (Exception e) {
			System.out.println("Something went wrong while updating raw image.");
		}
		return null;
	}

	/* Just a list of numbers, each number referring to a slice */
	@Override public List<String> getSliceList() {
		List<String> names = new ArrayList<>();
		for (int i = 0; i < depth; ++i) {
			names.add("Slice #" + Integer.toString(i));
		}
		return names;
	}
}
