package gov.inl.HZGenerator.BrickFactory;

import javafx.embed.swing.SwingFXUtils;
import javafx.scene.image.ImageView;
import jdk.nashorn.internal.runtime.ECMAException;

import java.awt.image.BufferedImage;
import java.awt.image.RescaleOp;

/**
 * Created by BITINK on 6/27/2017.
 */
public class SlicePreview {
	ImageView view = null;
	public float previewIntensity = 1.0f;

	public SlicePreview(ImageView view) throws Exception {
		if (view == null) throw new Exception("ImageView provided was null.");
		this.view = view;
	}

	public void show(BufferedImage slice) {
		if (slice == null) new Exception("Slice provided was null.");

		if (previewIntensity != 1.0f) {
			RescaleOp rescaleOp = new RescaleOp(previewIntensity, 15, null);
			rescaleOp.filter(slice, slice);
		}

		view.setImage(SwingFXUtils.toFXImage(slice, null));
	}
}
