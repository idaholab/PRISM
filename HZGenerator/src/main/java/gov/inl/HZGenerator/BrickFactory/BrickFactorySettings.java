package gov.inl.HZGenerator.BrickFactory;

/**
 * Nathan Morrical. Summer 2017.
 * This settings class contains settings used during the bricking process.
 */
public class BrickFactorySettings {
	/* Determines if the slices of the original volume should be masked */
	public Boolean masked = false;

	/* If the input is more than 8 bits per pixel, this scales the output to 8bpp */
	public Boolean mapTo8BPP = false;

	/* If the volume is being masked, how much above or below mean should be culled? */
	public float threshold = 0.0f;

	/* Max and minimum sizes to partition */
	private int minBrickSize = 1 << 6;
	private int maxBrickSize = 1 << 9;

	public String outputPath;

	public void setMinBrickSize(int size) throws Exception {
		/* if the given size is not a power of two */
		if ( (size & (size - 1)) != 0)
			throw new Exception("size must be a power of two");
		else
			minBrickSize = size;
	}
	public void setMaxBrickSize(int size) throws Exception {
		/* if the given size is not a power of two */
		if ( (size & (size - 1)) != 0)
			throw new Exception("size must be a power of two");
		else
			maxBrickSize = size;
	}
	public int getMinBrickSize() {return minBrickSize;}
	public int getMaxBrickSize() {return maxBrickSize;}
}
