package gov.inl.HZGenerator.Octree;

import gov.inl.HZGenerator.BrickFactory.Brick;
import gov.inl.HZGenerator.CLFW;
import gov.inl.HZGenerator.Kernels.PartitionerResult;
import javafx.util.Pair;
import org.joml.Vector3i;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.LinkedList;
import java.util.List;

/**
 * Created by Nate on 7/28/2017.
 */
public class OctNode {
    List<Pair<Integer, Brick>> bricks;
    OctNode parent = null;
    OctNode children[] = new OctNode[8];
    Boolean isLeaf = true;
    int value = -1;


    Vector3i pxPosition;
    int pxWidth;

    public void add(OctNode child, int index) {
        if (child != null) {
            isLeaf = false;
            children[index] = child;
        }
    }

    public static OctNode buildOctree(Vector3i volumeSize, PartitionerResult pr, int minBrickSize) {
        /* Determine octree size */
        int maxDim = Integer.max(volumeSize.x, Integer.max(volumeSize.y, volumeSize.z));
        int maxOctDim = CLFW.NextPow2(maxDim) / minBrickSize;

        /* Determine total levels */
        int levels = 0;
        int temp = maxOctDim;
        while (temp > 0) {
            levels ++;
            temp /= 2;
        }

        /* Start by adding all bricks to the root node */
        OctNode root = new OctNode();
        root.bricks = new LinkedList<>();
        for (int i = 0; i < pr.bricks.size(); ++i)
            root.bricks.add(new Pair<>(i, pr.bricks.get(i)));

        /* Root position at 0, width covers all bricks */
        root.pxPosition = new Vector3i(0,0,0);
        root.pxWidth = CLFW.NextPow2(maxDim);

        generateLevel(root, root.bricks, levels);

        return root;
    }

    private static void generateLevel(OctNode node, List<Pair<Integer, Brick>> bricks, int currentLevel) {
        /* If we're at the bottom of the tree, exit. */
        if (currentLevel <= 0) return;

        /* If there isn't more than one brick, exit*/
        if (bricks.size() <= 0) return;
        node.isLeaf = false;

        /* Otherwise, initialize the children of this node */
        for (int i = 0; i < 8; ++i) {
            OctNode child = new OctNode();
            child.parent = node;
            child.pxWidth = node.pxWidth / 2;
            child.pxPosition = new Vector3i(
                    ((i & 1 << 0) == 0) ? node.pxPosition.x : node.pxPosition.x + child.pxWidth,
                    ((i & 1 << 1) == 0) ? node.pxPosition.y : node.pxPosition.y + child.pxWidth,
                    ((i & 1 << 2) == 0) ? node.pxPosition.z : node.pxPosition.z + child.pxWidth);
            child.bricks = new LinkedList<>();
            node.children[i] = child;
        }

        /* Split up the bricks to the children */
        Vector3i middle = node.pxPosition.add(new Vector3i(node.pxWidth / 2));
        for (int i = 0; i < bricks.size(); ++i) {
            Pair<Integer, Brick> currentBrick = bricks.get(i);
            Vector3i brickPos = currentBrick.getValue().getPosition();

            int x = brickPos.x >= middle.x ? 1 : 0;
            int y = brickPos.y >= middle.y ? 1 : 0;
            int z = brickPos.z >= middle.z ? 1 : 0;

            int index = (x << 0) | (y << 1) | (z << 2);
            node.children[index].bricks.add(currentBrick);
        }

        for (int i = 0; i < 8; ++i)
            generateLevel(node.children[i], node.children[i].bricks, currentLevel - 1);
    }

    public JSONObject toJson() {
        JSONObject current = new JSONObject();

        try {
            if (!isLeaf) {
                JSONArray children = new JSONArray();
                for (int i = 0; i < 8; ++i) {
                    children.put(this.children[i].toJson());
                }
                current.put("Children", children);
            } else {
                current.put("Value", value);
            }
        } catch (JSONException e) {
            e.printStackTrace();
        }

        return current;
    }
}
