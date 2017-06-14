import javafx.application.Platform;
import javafx.scene.*;
import javafx.scene.layout.Pane;
import javafx.scene.paint.Color;
import javafx.scene.paint.PhongMaterial;
import javafx.scene.shape.Box;
import javafx.scene.shape.DrawMode;
import javafx.scene.transform.Rotate;
import javafx.scene.transform.Translate;

import java.util.Timer;
import java.util.TimerTask;

/**
 * Nate VM.
 * 	BrickPreview - Uses javafx3d to visualize potential bricks
 */
public class BrickPreview {
	Camera camera;
	float cameraAngle = 0.f;

	Group root;
	Pane pane;

	AmbientLight al = new AmbientLight(Color.rgb(100, 100, 100, 1.));
	PointLight pl = new PointLight(Color.rgb(255, 255, 255, 1.0));

	private Box[][][][] boxes;
	int xBricks, yBricks, zBricks, dimWidth;
	float boxWidth = 5.5f;
	Color toggledColor = Color.GREEN;
	Color untoggledColor = Color.WHITE;
	Color hiddenColor = Color.rgb(0, 0, 0, 0.0);

	Boolean updateScene = true;

	/* Initializes a 3D javafx scene, adds an ambient light and point light, and a placeholder box. */
	public BrickPreview(Pane pane) {
		/* Create 3D scene, and add to the pane. */
		this.pane = pane;
		root = new Group();
		SubScene subScene = new SubScene(root, pane.getPrefWidth(), pane.getPrefHeight(),
				true, SceneAntialiasing.DISABLED);
		subScene.setFill(Color.rgb(0, 0, 0, 1.));
		pane.getChildren().clear();
		pane.getChildren().add(subScene);

		/* Add Scene lights */
		pl.getTransforms().addAll(
				new Rotate(-30, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, .25, -18)
		);
		root.getChildren().add(al);
		root.getChildren().add(pl);

		/* Create Scene Camera */
		camera = new PerspectiveCamera(true);
		camera.getTransforms().addAll(
				new Rotate(-30, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, .25, -18)
		);
		subScene.setCamera(camera);

		/* Create empty box */
		Box box = new Box(boxWidth, boxWidth, boxWidth);
		box.setMaterial(new PhongMaterial(untoggledColor));
		box.setDrawMode(DrawMode.LINE);
		root.getChildren().add(box);


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

	/* Called asynchronously after BrickPreview is constructed. */
	public void update() {
		cameraAngle += .3f;
		if (cameraAngle > 360.f)
			cameraAngle = 0.f;
		camera.getTransforms().setAll(
				new Rotate(cameraAngle, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, .25, -18)
		);

		pl.getTransforms().setAll(
				new Rotate(cameraAngle, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, .25, -18)
		);
	};

	/* Throws away the previous bricks and creates a new set to be rendered. */
	public void createBricks(int xBricks, int yBricks, int zBricks, int dimWidth) {
		this.xBricks = xBricks;
		this.yBricks = yBricks;
		this.zBricks = zBricks;
		this.dimWidth = dimWidth;

		// Slice major
		boxes = new Box[xBricks][yBricks][zBricks][dimWidth];

		root.getChildren().clear();
		root.getChildren().add(al);
		root.getChildren().add(pl);

		int maxBricks = Integer.max(xBricks, Integer.max(yBricks, zBricks));

		float subBoxWidth = boxWidth/ maxBricks;

		float xOffset = -(subBoxWidth * (xBricks - 1)) / 2.f ;
		float yOffset = -(subBoxWidth * (yBricks - 1)) / 2.f;
		float zOffset = -(subBoxWidth * (zBricks-1)) / 2.f;

		// For each slice in each brick
		for (int xb = 0; xb < xBricks; ++xb) {
			for (int yb = 0; yb < yBricks; ++yb) {
				for (int zb = 0; zb < zBricks; ++zb) {
					for (int i = 0; i < dimWidth; ++i) {
						boxes[xb][yb][zb][i] = new Box(subBoxWidth, subBoxWidth * (1.f / dimWidth), subBoxWidth);
						boxes[xb][yb][zb][i].setMaterial(new PhongMaterial(Color.WHITE));
						boxes[xb][yb][zb][i].setTranslateX(subBoxWidth * xb + xOffset);
						float sliceOffset = -(subBoxWidth/2.f) + subBoxWidth * (i / (float)dimWidth);
						boxes[xb][yb][zb][i].setTranslateY(subBoxWidth * yb + sliceOffset + yOffset);
						boxes[xb][yb][zb][i].setTranslateZ(subBoxWidth * zb + zOffset);
						root.getChildren().add(boxes[xb][yb][zb][i]);
					}
				}
			}
		}


	}

	/* Switches between the toggled and untoggled colors */
	public void toggleSlice(int x, int y, int z, int slice) {
		PhongMaterial mat = (PhongMaterial)boxes[x][y][z][slice].getMaterial();
		if(mat.getDiffuseColor().equals(toggledColor)) {
			mat.setDiffuseColor(untoggledColor);
		} else {
			mat.setDiffuseColor(toggledColor);
		}
	};

	public void toggleSlice(int x, int y, int z, int slice, Boolean toggled) {
		PhongMaterial mat = (PhongMaterial)boxes[x][y][z][slice].getMaterial();
		double xs = boxes[x][y][z][slice].getScaleX();
		double ys = boxes[x][y][z][slice].getScaleX();
		double zs = boxes[x][y][z][slice].getScaleX();
		if(!toggled) {
			mat.setDiffuseColor(untoggledColor);
			boxes[x][y][z][slice].setScaleX(xs - .1);
			boxes[x][y][z][slice].setScaleY(ys - .1);
			boxes[x][y][z][slice].setScaleZ(zs - .1);
		} else {
			mat.setDiffuseColor(toggledColor);
			boxes[x][y][z][slice].setScaleX(xs + .1);
			boxes[x][y][z][slice].setScaleY(ys + .1);
			boxes[x][y][z][slice].setScaleZ(zs + .1);
		}
	};

	/* toggles all slices on a layer */
	public void toggleLayer(int imageSlice) {
		int slice = imageSlice % dimWidth;
		int y = imageSlice / dimWidth;

		for (int x = 0; x < xBricks; ++x)
			for (int z = 0; z < zBricks; ++z) {
				toggleSlice(x, y, z, slice);
			}
	}

	public void toggleLayer(int imageSlice, Boolean toggled) {
		int slice = imageSlice % dimWidth;
		int y = imageSlice / dimWidth;

		for (int x = 0; x < xBricks; ++x)
			for (int z = 0; z < zBricks; ++z)
				toggleSlice(x, y, z, slice, toggled);
	}

	/* Sets the material of the selected brick to hiddenColor. */
	public void hideSlice(int x, int y, int z, int slice) {
		PhongMaterial mat = (PhongMaterial)boxes[x][y][z][slice].getMaterial();
		mat.setDiffuseColor(hiddenColor);
	}

	/* pauses/starts the update loop */
	public void setUpdate(Boolean value) {
		updateScene = value;
	}


}
