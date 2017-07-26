package gov.inl.HZGenerator.BrickFactory;

import javafx.embed.swing.SwingFXUtils;
import javafx.scene.image.ImageView;
import jdk.nashorn.internal.runtime.ECMAException;

import java.awt.image.BufferedImage;
import java.awt.image.RescaleOp;

import static java.awt.image.BufferedImage.TYPE_BYTE_GRAY;
import static java.awt.image.BufferedImage.TYPE_USHORT_GRAY;

/**
 *  Nathan Morrical. Summer 2017.
 *  This class controls slices that show in the slice preview port.
 */
public class SlicePreview {
	ImageView view = null;
	public float previewIntensity = 1.0f;

	public SlicePreview(ImageView view) throws Exception {
		if (view == null) throw new Exception("ImageView provided was null.");
		this.view = view;
	}

	/* Displays a potentially modified version of the provided slice. */
	public void show(BufferedImage slice) {
		if (slice == null) new Exception("Slice provided was null.");

		int type  = -1;
		if (slice.getColorModel().getPixelSize() == 8)
			type = TYPE_BYTE_GRAY;
		if (slice.getColorModel().getPixelSize() == 16)
			type = TYPE_USHORT_GRAY;
		BufferedImage temp = new BufferedImage(slice.getWidth(), slice.getHeight(), type);

		if (previewIntensity != 1.0f) {
			RescaleOp rescaleOp = new RescaleOp(previewIntensity, 15, null);
			rescaleOp.filter(slice, temp);
			view.setImage(SwingFXUtils.toFXImage(temp, null));

		} else {
			view.setImage(SwingFXUtils.toFXImage(slice, null));
		}

	}
}
