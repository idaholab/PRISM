package gov.inl.HZGenerator.BrickFactory;

import org.joml.*;
/**
 * Nate VM. Summer 2017.
 *
 * A partition is a cubic region located in a volume.
 */
public class Partition {
	Vector3f position;
	int size;

	public Partition () {
		position = new Vector3f(0.f, 0.f, 0.f);
	}
	public void setPosition(int x, int y, int z) {
		this.position = new Vector3f(x, y, z);
	}
	public void setSize(int size) {
		this.size = size;
	}
}
