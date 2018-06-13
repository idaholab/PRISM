package gov.inl.HZGenerator.BrickFactory;

import org.joml.*;

import java.awt.image.BufferedImage;
import java.nio.ByteOrder;
import java.util.List;

/**
 * Nathan Morrical. Summer 2017
 * The following is an abstract interface, which can be inherited to allow support for additional data sources.
 */
public abstract class Volume {
	/* A volume is a region with width, height, and depth */
	int width, height, depth = 0;
	int bytesPerPixel = 0;
	ByteOrder byteOrder = ByteOrder.nativeOrder();

	/* Each volume is capable of returning a slice given an index */
	public abstract BufferedImage getSlice(int i);

	/* Different sources handle slice names differently. These names are used for selection in the GUI */
	public abstract List<String> getSliceList();

	public int getWidth() {return width;}
	public int getHeight() {return height;}
	public int getDepth() {return depth;}
	public Vector3f getSize() {return new Vector3f(width, height, depth);}
	public int getBytesPerPixel(){return bytesPerPixel;}
	public ByteOrder getByteOrder() {return byteOrder;}
}
