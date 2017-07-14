package gov.inl.HZGenerator.BrickFactory;

import org.joml.*;

import java.awt.image.BufferedImage;
import java.util.List;

/**
 * Created by BITINK on 6/27/2017.
 */
public abstract class Volume {
	int width, height, depth = 0;

	abstract BufferedImage getSlice(int i);
	abstract List<String> getSliceList();

	public int getWidth() {return width;}
	public int getHeight() {return height;}
	public int getDepth() {return depth;}
	public Vector3f getSize() {return new Vector3f(width, height, depth);}

	public abstract long getBytesPerPixel();
}
