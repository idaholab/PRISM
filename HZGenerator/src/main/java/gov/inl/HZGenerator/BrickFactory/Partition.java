package gov.inl.HZGenerator.BrickFactory;

import org.joml.*;
/**
 * Created by BITINK on 6/27/2017.
 */
public class Partition {
	Vector3f pos;
	int size;

	public Partition () {
		pos = new Vector3f(0.f, 0.f, 0.f);
	}

	public void setPosition(int x, int y, int z) {
		this.pos = new Vector3f(x, y, z);
	}
	public void setSize(int size) {
		this.size = size;
	}
}
