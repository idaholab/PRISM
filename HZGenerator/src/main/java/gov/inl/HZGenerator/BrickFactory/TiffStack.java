package gov.inl.HZGenerator.BrickFactory;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * Nathan Morrical. Summer 2017.
 */
public class TiffStack extends Volume {
	File[] tiffs = null;
	int bytesPerPixel;

	public TiffStack(String path) throws Exception {
		File folder = new File(path);
		if (folder == null) throw new Exception("Invalid tiff directory path");

		/* If the tiff directory exists, get a list of all tiff files in the directory. */
		tiffs = folder.listFiles((dir, name) -> name.endsWith(".tiff") || name.endsWith(".tif"));
		if (tiffs.length <= 0) throw new Exception("No .tiff/.tif files found in the given directory");

		/* Sort the tiffs by name */
		Arrays.sort(tiffs);
		BufferedImage slice = ImageIO.read(tiffs[0]);
		this.height = slice.getHeight();
		this.width = slice.getWidth();
		this.depth = tiffs.length;
		bytesPerPixel = slice.getColorModel().getPixelSize();
	}

	@Override
	public BufferedImage getSlice(int i) {
		BufferedImage slice = null;
		try {
			slice = ImageIO.read(tiffs[i]);
		} catch (IOException e) {
			return null;
		}
		return slice;
	}

	@Override
	public List<String> getSliceList() {
		List<String> names = new ArrayList<>();
		for (File t : tiffs) names.add(t.toString());
		return names;
	}

	@Override
	public long getBytesPerPixel() {
		return bytesPerPixel / 8;
	}
}
