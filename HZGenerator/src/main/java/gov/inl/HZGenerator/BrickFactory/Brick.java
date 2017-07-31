package gov.inl.HZGenerator.BrickFactory;

import org.joml.*;
/**
 * Nate VM. Summer 2017.
 *
 * A brick is a cubic region located in a volume.
 */
public class Brick {
	Vector3i position;
	int size;

	public Brick() {
		position = new Vector3i();
	}
	public void setPosition(int x, int y, int z) {
		this.position = new Vector3i(x, y, z);
	}
	public Vector3i getPosition(){return position;}
	public void setSize(int size) {
		this.size = size;
	}
}
