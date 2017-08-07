package gov.inl.HZGenerator.BrickFactory;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

import gov.inl.HZGenerator.Kernels.*;
import gov.inl.HZGenerator.Octree.OctNode;
import org.joml.Vector3i;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/**
 * Nathan Morrical. Summer 2017.
 * The following class is used to analyze a volume in parallel, quickly obtaining a list of cubic bricks whose
 * 	width is a power of two. This allows each brick to be curved using HZ Ordering, while adding minimal padding.
 */
public class VolumeBricker {
	Volume lastVolume;
	int minBrickSize, maxBrickSize = 0;
	int bytesPerPixel;
	public PartitionerResult pr = null;

	/* This method uses the GPU to accelerate partitioning, returning a list of bricks */
	public PartitionerResult partition(Volume volume, BrickFactorySettings settings) {
		/* If the volume/settings haven't changed since last time, just return the previously created
		* bricks. */
		if( (lastVolume == volume) &&
			(minBrickSize == settings.getMinBrickSize()) &&
			maxBrickSize == settings.getMaxBrickSize())
				return pr;
		/* Remember the parameters for above return optimization */
		lastVolume = volume;
		minBrickSize = settings.getMinBrickSize();
		maxBrickSize = settings.getMaxBrickSize();
		int width = volume.getWidth();
		int height = volume.getHeight();
		int depth = volume.getDepth();
		bytesPerPixel = (settings.mapTo8BPP == true) ? 1 : lastVolume.getBytesPerPixel();
		pr = new PartitionerResult();
		Partitioner.CombineBricks(width, height, depth, minBrickSize,
				maxBrickSize, pr );
		/* Its possible that all bricks are larger than the min brick size, so we find the actual min brick size here
		* and pass that to the octree to calculate total levels */
		minBrickSize = pr.bricks.get(0).size;
		for (int i = 1; i < pr.bricks.size(); ++i) {
			minBrickSize = Integer.min(minBrickSize, pr.bricks.get(i).size);
		}
		pr.octree = OctNode.buildOctree(new Vector3i(width, height, depth), pr, minBrickSize);
		return pr;
	}

	/* Saves partition data for later visualization */
	public void saveJson(String outputPath) throws IOException {
		try {
			JSONObject json = new JSONObject();
			json.put("bytesPerPixel", bytesPerPixel);
			json.put("totalBricks", pr.bricks.size());

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
			for (int i = 0; i < pr.bricks.size(); ++i) {
				JSONObject brick = new JSONObject();
				brick.put("filename", i + ".hz");
				brick.put("size", pr.bricks.get(i).size);
				JSONArray position = new JSONArray();
				position.put(pr.bricks.get(i).position.x);
				position.put(pr.bricks.get(i).position.y);
				position.put(pr.bricks.get(i).position.z);
				brick.put("position", position);
				bricks.put(brick);
			}
			json.put("bricks", bricks);
			json.put("octree", pr.octree.toJson());
			json.put("totalOctnodes", OctNode.getTotalNodes(pr.octree));
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
