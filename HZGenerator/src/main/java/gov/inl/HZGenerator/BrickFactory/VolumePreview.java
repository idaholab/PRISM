package gov.inl.HZGenerator.BrickFactory;


import javafx.application.Platform;
import javafx.scene.*;
import javafx.scene.layout.Pane;
import javafx.scene.paint.*;
import javafx.scene.shape.*;
import javafx.scene.transform.*;
import java.util.*;
import java.util.Random;
import org.joml.*;

/**
 * Nate Morrical.
 * 	gov.inl.HZGenerator.BrickFactory.VolumePreview - Uses javafx3d to visualize potential bricks
 */
public class VolumePreview {
	Group group;
	Pane pane;

	Camera camera;
	float cameraAngle = 0.f;
	float cameraDistance = 8.f;
	AmbientLight ambientLight = new AmbientLight(Color.rgb(100, 100, 100, 1.));
	PointLight pointLight = new PointLight(Color.rgb(255, 255, 255, 1.0));

	private Box boxes[];

	Boolean updateScene = true;

	/* Initializes a 3D javafx scene, adds an ambient light and point light, and a placeholder box. */
	public VolumePreview(Pane pane) {
		/* Create 3D scene, and add to the pane. */
		this.pane = pane;
		group = new Group();
		SubScene subScene = new SubScene(group, pane.getPrefWidth(), pane.getPrefHeight(),
				true, SceneAntialiasing.DISABLED);
		subScene.setFill(Color.rgb(0, 0, 0, 1.));
		pane.getChildren().clear();
		pane.getChildren().add(subScene);

		/* Add Scene lights */
		pointLight.getTransforms().setAll(
				new Rotate(-30, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, 0, -cameraDistance)
		);
		group.getChildren().add(ambientLight);
		group.getChildren().add(pointLight);

		/* Create Scene Camera */
		camera = new PerspectiveCamera(true);
		camera.getTransforms().setAll(
				new Rotate(-30, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, 0, -cameraDistance)
		);
		subScene.setCamera(camera);

		/* Create empty box */
		Box box = new Box(2.f, 2.f, 2.f);
		box.setMaterial(new PhongMaterial(Color.WHITE));
		box.setDrawMode(DrawMode.LINE);
		group.getChildren().add(box);


		/* Add async refresh daemon. */
		new Timer(true).schedule(
				new TimerTask() {
					@Override
					public void run() {
						Platform.runLater(() -> {
							if (updateScene)
								update();
						});
					}
				}, 0, 16);
	}

	/* Called asynchronously after gov.inl.HZGenerator.BrickFactory.VolumePreview is constructed. */
	public void update() {
		cameraAngle += .3f;
		if (cameraAngle > 360.f)
			cameraAngle = 0.f;
		camera.getTransforms().setAll(
				new Rotate(cameraAngle, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, 0, -cameraDistance)
		);

		pointLight.getTransforms().setAll(
				new Rotate(cameraAngle, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, 0, -cameraDistance)
		);
	};

	/* pauses/starts the update loop */
	public void setUpdateScene(Boolean value) {
		updateScene = value;
	}

	/* Shows the potential partitions of a bricked volume */
	public void show(Volume volume, List<Partition> partitions) {
		boxes = new Box[partitions.size() + 1];

		group.getChildren().clear();
		group.getChildren().add(ambientLight);
		group.getChildren().add(pointLight);

		Box box = new Box(2.f, 2.f, 2.f);
		box.setMaterial(new PhongMaterial(Color.WHITE));
		box.setDrawMode(DrawMode.LINE);
		group.getChildren().add(box);
		boxes[partitions.size()] = box;

		float bbMaxWidth = Integer.max(volume.getWidth(), Integer.max(volume.getHeight(), volume.getDepth()));

		Vector3f bbSize = new Vector3f(bbMaxWidth, bbMaxWidth, bbMaxWidth);
		Vector3f offset = bbSize.sub(volume.getSize()).mul(.5f);

		Random rand = new Random();
		for (int i = 0; i < partitions.size(); ++i) {
			Partition p = partitions.get(i);
			Vector3f offset_ = new Vector3f(offset);
			float size = (p.size * 2.f) / bbMaxWidth;
			/* Reference at bottom left of bounding box, then add on local transform */
			Vector3f pos = offset_.add(p.pos);

			/* Place position inside 1,1,1 cube */
			pos = pos.div(bbMaxWidth).mul(2.f).sub(1.f, 1.f, 1.f);

			/* Move position to center of current cube */
			pos = pos.add(size * .5f, size * .5f, size * .5f);

			/* Render */
			Box b = new Box(size, size, size);
 			b.setMaterial(new PhongMaterial(Color.rgb(rand.nextInt(255), rand.nextInt(255), rand.nextInt(255))));
			b.setTranslateX(pos.x);
			b.setTranslateY(pos.y);
			b.setTranslateZ(pos.z);
			boxes[i] = b;
			group.getChildren().add(b);
		}
	}
}
