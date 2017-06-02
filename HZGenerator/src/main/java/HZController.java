/* ImageIO */
import java.awt.*;
import java.awt.image.BufferedImage;
import java.io.*;

import com.sun.org.apache.xpath.internal.operations.Bool;
import ij.IJ;
import ij.io.Opener;
import ij.plugin.ImageCalculator;
import ij.plugin.filter.GaussianBlur;
import ij.process.ImageConverter;
import ij.ImagePlus;
import ij.process.ImageStatistics;
import ij.process.ShortProcessor;
import javafx.application.Platform;
import javafx.beans.InvalidationListener;
import javafx.beans.property.SimpleLongProperty;
import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.embed.swing.SwingFXUtils;
import javafx.embed.swing.SwingNode;
import javafx.scene.*;
import javafx.scene.control.*;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;

import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.scene.input.DragEvent;
import javafx.scene.input.Dragboard;
import javafx.scene.input.MouseEvent;
import javafx.scene.input.ScrollEvent;
import javafx.scene.layout.AnchorPane;
import javafx.scene.layout.Pane;
import javafx.scene.paint.Color;
import javafx.scene.paint.PhongMaterial;
import javafx.scene.shape.Box;
import javafx.scene.shape.DrawMode;
import javafx.scene.transform.Rotate;
import javafx.scene.transform.Translate;
import javafx.stage.DirectoryChooser;
import javafx.stage.Stage;

import java.util.Arrays;
import java.util.List;
import java.util.Timer;
import java.util.TimerTask;

import javafx.collections.*;

/**
 * Created by BITINK on 5/22/2017.
 */
public class HZController {
	public Stage stage;

	@FXML public AnchorPane rootPane;
	@FXML private TextField txtSelectedDirectory;
	@FXML private TreeView<String> fileTree;
	@FXML private ImageView preview;
	@FXML private Slider slider;
	@FXML private CheckBox isMasked;
	private PerspectiveCamera camera;
	@FXML private Pane brickPane;
	private Box box;
	@FXML Label lblTime;
	long lastTime = -1;
	Boolean update3DScene = true;

	@FXML Spinner spnrX;
	@FXML Spinner spnrY;
	@FXML Spinner spnrZ;

	String selectedThresholdMode;
	ImagePlus openedImage;
	BufferedImage b_openedImage;

	String currentImagePath = "";
	public void initialize() throws Exception {
		fileTree.getSelectionModel().selectedItemProperty().addListener((v, oldValue, newValue) -> {
			if ( currentImagePath.compareTo(newValue.getValue()) != 0) {
				currentImagePath = newValue.getValue();
				updateImage(newValue.getValue());
			}
		});

		slider.valueChangingProperty().addListener(new ChangeListener<Boolean>() {
			@Override
			public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) {
				refreshImage();
			}
		});

		BufferedImage temp = new BufferedImage((int) preview.getFitWidth(), (int) preview.getFitHeight(),
				BufferedImage.TYPE_USHORT_GRAY);
		Image finalImage = SwingFXUtils.toFXImage(temp, null);
		preview.setImage(finalImage);

		lblTime.textProperty().bind(new SimpleLongProperty(lastTime).asString());


		createContent();
	}


	public Parent createContent() throws Exception {
		box = new Box(5., 5., 5.);
		box.setMaterial(new PhongMaterial(Color.RED));
		box.setDrawMode(DrawMode.LINE);

		AmbientLight ia = new AmbientLight(Color.rgb(255, 255, 255, .01));

		camera = new PerspectiveCamera(true);
		camera.getTransforms().addAll(
				new Rotate(-30, Rotate.Y_AXIS),
				new Rotate(-30, Rotate.X_AXIS),
				new Translate(0, .25, -15)
		);

		//Build the Scene graph
		Group root = new Group();
		root.getChildren().add(camera);
		root.getChildren().add(box);
		root.getChildren().add(ia);

		//Use a subscene
		SubScene subScene = new SubScene(root, brickPane.getPrefWidth(), brickPane.getPrefHeight());
		subScene.setCamera(camera);
		subScene.setFill(Color.rgb(0, 0, 0, 1.));
		Group group = new Group();
		group.getChildren().add(subScene);

		brickPane.getChildren().add(group);

		new Timer().schedule(
			new TimerTask() {
				@Override
				public void run() {
					Platform.runLater(() -> {
						if (update3DScene)
							update3D();
					});
				}
			}, 0, 50);
		return group;
	}

	@FXML
	private void openFolderDialogue(ActionEvent event)
	{
		/* Choose a directory */
		DirectoryChooser dc = new DirectoryChooser();
		File selectedDirectory = dc.showDialog(stage);
		if (selectedDirectory != null) {
			txtSelectedDirectory.setText(selectedDirectory.getAbsolutePath());
		}

		buildTree(selectedDirectory);
	}

	@FXML
	private void dropFolder(DragEvent event) {
		final Dragboard db = event.getDragboard();

		boolean success = false;
		if(db.hasFiles()) {
			success = true;
			/* Only take the first dragged file */
			final File file = db.getFiles().get(0);
			buildTree(file);
		}
	}

	@FXML
	private void maskChecked(ActionEvent event) {
		Boolean selected = isMasked.isSelected();
		slider.setDisable(!selected);
		refreshImage();
	}

	/* Adds all .tif/.tiff image paths of a given directory to the file tree. */
	private void buildTree(File selectedDirectory) {
		/* Clear previous file tree */
		fileTree.setRoot(null);

		/* Add all tiff files to directory  */
		TreeItem<String> rootItem = new TreeItem<String>("", null);
		File [] files = selectedDirectory.listFiles(new FilenameFilter() {
			@Override
			public boolean accept(File dir, String name) {
				return name.endsWith(".tiff") || name.endsWith(".tif");
			}
		});
		try {
			File f = new File("test.dump");
			f.createNewFile();
			//RandomAccessFile raf = new RandomAccessFile(f, "rw");
//			System.out.println((long)Math.pow(2, 63));
			//raf.setLength(50000000000L);
			//raf.close();
		} catch (IOException e) {
			e.printStackTrace();
		}


		/* Sort volume by filename, then add to tree */
		Arrays.sort(files);
		for (File f : files) {
			rootItem.getChildren().add(new TreeItem<String>(f.getPath(), null));
		}

		/* Auto expand, hide the root node, and add tree items to tree. */
		rootItem.setExpanded(true);
		fileTree.setShowRoot(false);
		fileTree.setRoot(rootItem);
	}

	private void refreshImage() {
		update3DScene = false;
		if ( openedImage != null) {
			if (isMasked.isSelected()) {
				new Thread(new Runnable() {
					@Override
					public void run() {
						long start = System.nanoTime();
					/* Blur the image */
						CLBlur clb = new CLBlur();
						BufferedImage src = openedImage.getBufferedImage();
						ImageStatistics stats = ImageStatistics.getStatistics(
								openedImage.getProcessor(), ImageStatistics.MEAN, openedImage.getCalibration());
						double mean = stats.mean;
						//				double stdDev = stats.stdDev;
						//				double max = 3 * stdDev;
						//				double min = -3 * stdDev;
						//				clb.threshold = (int)(mean + (slider.getValue() * (max - min)));
						clb.passes = 2;
						clb.threshold = (int) (mean);
						clb.threshold = (int) ((clb.threshold / openedImage.getDisplayRangeMax()) * 255.f); //temporary until 16bits works
						clb.filter(src, src);
						Image finalImage = SwingFXUtils.toFXImage(src, null);
						long end = System.nanoTime();

						//lastTime = (end - start) / 1000000;
						preview.setImage(finalImage);
					}
				}).start();
			}
			else {
				ImageConverter ico = new ImageConverter(openedImage);
				ico.convertToGray16();
				BufferedImage temp = openedImage.getBufferedImage();
				Image finalImage = SwingFXUtils.toFXImage(temp, null);
				preview.setImage(finalImage);
			}
		}
		update3DScene = true;
	}

	private void updateImage(String path) {
		try {
			Opener opener = new Opener();
			openedImage = opener.openImage(path);
			refreshImage();
		} catch (Exception e) {
			System.out.println(e);
		}
	}

	private void update3D() {
		box.getTransforms().add(
				new Rotate(.1, Rotate.Y_AXIS)
		);
	}
}
