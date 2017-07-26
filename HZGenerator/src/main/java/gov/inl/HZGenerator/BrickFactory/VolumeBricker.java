package gov.inl.HZGenerator.BrickFactory;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

import gov.inl.HZGenerator.Kernels.*;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/**
 * Nathan Morrical. Summer 2017.
 * The following class is used to analyze a volume in parallel, quickly obtaining a list of cubic bricks whose
 * 	width is a power of two. This allows each brick to be curved using HZ Ordering, while adding minimal padding.
 */
public class VolumeBricker {
	public List<Brick> bricks;
	Volume lastVolume;
	int minBrickSize, maxBrickSize = 0;
	int bytesPerPixel;
	public PartitionerResult pr = null;

	/* Constructor */
	public VolumeBricker() {
		bricks = new ArrayList<>();
	}

	/* This method uses the GPU to accelerate partitioning, returning a list of bricks */
	public List<Brick> partition(Volume volume, BrickFactorySettings settings) {
		/* If the volume/settings haven't changed since last time, just return the previously created
		* bricks. */
		if( (lastVolume == volume) &&
			(minBrickSize == settings.getMinBrickSize()) &&
			maxBrickSize == settings.getMaxBrickSize())
				return bricks;
		/* Remember the parameters for above return optimization */
		lastVolume = volume;
		minBrickSize = settings.getMinBrickSize();
		maxBrickSize = settings.getMaxBrickSize();
		bricks.clear();
		int width = volume.getWidth();
		int height = volume.getHeight();
		int depth = volume.getDepth();
		bytesPerPixel = (settings.mapTo8BPP == true) ? 1 : lastVolume.getBytesPerPixel();
		pr = new PartitionerResult();
		Partitioner.CombineBricks(width, height, depth, minBrickSize,
				maxBrickSize, pr );
		bricks = pr.partitions;
		return bricks;
	}

	/* Saves partition data for later visualization */
	public void saveJson(String outputPath) throws IOException {
		try {
			JSONObject json = new JSONObject();
			json.put("bytesPerPixel", bytesPerPixel);
			json.put("totalBricks", bricks.size());

			int gx = roundUp(lastVolume.getWidth(), minBrickSize);
			int gy = roundUp(lastVolume.getHeight(), minBrickSize);
			int gz = roundUp(lastVolume.getDepth(), minBrickSize);
			JSONArray globalSize = new JSONArray();
			globalSize.put(gx);
			globalSize.put(gy);
			globalSize.put(gz);
			json.put("globalSize", globalSize);

			json.put("minLevel", minBrickSize);
			json.put("maxLevel", maxBrickSize);

			JSONArray bricks = new JSONArray();
			for (int i = 0; i < this.bricks.size(); ++i) {
				JSONObject brick = new JSONObject();
				brick.put("filename", i + ".hz");
				brick.put("size", this.bricks.get(i).size);
				JSONArray position = new JSONArray();
				position.put(this.bricks.get(i).position.x);
				position.put(this.bricks.get(i).position.y);
				position.put(this.bricks.get(i).position.z);
				brick.put("position", position);
				bricks.put(brick);
			}
			json.put("bricks", bricks);
			File f = new File(outputPath + "/metadata.json");
			f.delete();
			Boolean success = f.createNewFile();
			FileWriter fw = new FileWriter(f);

			fw.write(json.toString() );
			fw.close();

		} catch (JSONException e) {
			e.printStackTrace();
		}
	}

	int roundUp(int numToRound, int multiple)
	{
		if (multiple == 0)
			return numToRound;

		int remainder = numToRound % multiple;
		if (remainder == 0)
			return numToRound;

		return numToRound + multiple - remainder;
	}
}
