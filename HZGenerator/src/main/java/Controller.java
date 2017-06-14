/* ImageIO */
import java.awt.image.BufferedImage;
import java.io.*;

import gov.inl.SIEVAS.hmortonlib.HMorton3D;
import ij.ImagePlus;
import javafx.embed.swing.SwingFXUtils;
import javafx.scene.control.*;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;

import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.scene.layout.AnchorPane;
import javafx.scene.layout.Pane;

import java.util.*;


/**
 * Created by BITINK on 5/22/2017.
 */
public class Controller {

	private BrickPreview brickPreview;
	private VolumeProcessor volumeProcessor;

	/* FXML Controls */
	@FXML public AnchorPane rootPane;
	@FXML private TextField txtTiffPath;
	@FXML private TextField txtRawPath;
	@FXML private ListView<String> tiffImageList;
	@FXML private ListView<String> rawImageList;
	@FXML private ImageView preview;
	@FXML private Slider slider;
	@FXML private CheckBox isMasked;
	@FXML private Pane brickPane;
	@FXML Label lblTime;
	@FXML Label lblFileSize;
	@FXML Label lblNumLevels;
	@FXML Label lblNumBricks;
	@FXML TextField txtRawX;
	@FXML TextField txtRawY;
	@FXML TextField txtRawZ;
	@FXML ChoiceBox maxDimWidthChoice;
	@FXML ChoiceBox minDimWidthChoice;
	@FXML TabPane fileTabPane;

	/* Image Processing */
	ImagePlus openedImage;
	File rawFile;
	String currentImagePath = "";
	int currentSlice = -1;

	/* FXML Events */
	@FXML
	public void initialize() throws Exception {
		tiffImageList.getSelectionModel().selectedItemProperty().addListener((v, oldValue, newValue) -> {
			int previousSlice = currentSlice;
			currentSlice = tiffImageList.getSelectionModel().getSelectedIndex();
			brickPreview.toggleLayer(currentSlice, true);
			if (previousSlice != -1) {
				brickPreview.toggleLayer(previousSlice, false);
			}

			volumeProcessor.setSlice(currentSlice);
			volumeProcessor.updatePreview(preview, isMasked.isSelected());
		});

		rawImageList.getSelectionModel().selectedItemProperty().addListener((v, oldValue, newValue) -> {
			int previousSlice = currentSlice;
			currentSlice = rawImageList.getSelectionModel().getSelectedIndex();
			brickPreview.toggleLayer(currentSlice, true);
			if (previousSlice != -1) {
				brickPreview.toggleLayer(previousSlice, false);
			}

			volumeProcessor.setSlice(currentSlice);
			volumeProcessor.updatePreview(preview, null);
		});

		slider.valueProperty().addListener((observable, oldValue, newValue) -> {
			volumeProcessor.updatePreview(preview, true);
		});

		txtRawX.textProperty().addListener((observable, oldValue, newValue) -> {
			if (!newValue.matches("\\d*")) {
				txtRawX.setText(newValue.replaceAll("[^\\d]", ""));
			}
		});

		txtRawY.textProperty().addListener((observable, oldValue, newValue) -> {
			if (!newValue.matches("\\d*")) {
				txtRawY.setText(newValue.replaceAll("[^\\d]", ""));
			}
		});

		txtRawZ.textProperty().addListener((observable, oldValue, newValue) -> {
			if (!newValue.matches("\\d*")) {
				txtRawZ.setText(newValue.replaceAll("[^\\d]", ""));
			}
		});


		BufferedImage temp = new BufferedImage((int) preview.getFitWidth(), (int) preview.getFitHeight(),
				BufferedImage.TYPE_USHORT_GRAY);
		Image finalImage = SwingFXUtils.toFXImage(temp, null);
		preview.setImage(finalImage);

		maxDimWidthChoice.getItems().add("512");
		maxDimWidthChoice.getItems().add("256");
		maxDimWidthChoice.getItems().add("128");
		maxDimWidthChoice.getItems().add("64");
		maxDimWidthChoice.getItems().add("32");
		maxDimWidthChoice.getItems().add("16");
		maxDimWidthChoice.getItems().add("8");

		minDimWidthChoice.getItems().add("512");
		minDimWidthChoice.getItems().add("256");
		minDimWidthChoice.getItems().add("128");
		minDimWidthChoice.getItems().add("64");
		minDimWidthChoice.getItems().add("32");
		minDimWidthChoice.getItems().add("16");
		minDimWidthChoice.getItems().add("8");



		brickPreview = new BrickPreview(brickPane);
		volumeProcessor = new VolumeProcessor();
	}

	@FXML
	private void openVolume(ActionEvent event) throws Exception {
		String id = ((Button)event.getSource()).getId();

		String result = null;

		if (id.compareTo("btnOpenTiffVolume") == 0) {
			result = volumeProcessor.open("TIFF");
			if (result != null) {
				txtTiffPath.setText(result);
				lblFileSize.setText(volumeProcessor.getCurvedFileSize());
				lblNumBricks.setText(volumeProcessor.getNumBricks() + "");
				lblNumLevels.setText(volumeProcessor.getNumLevels() + "");
				int[] numBricks = volumeProcessor.getNumBricksPerDim();
				int dimWidth = volumeProcessor.getDimWidth();
				brickPreview.createBricks(numBricks[0], numBricks[1], numBricks[2], dimWidth);

				tiffImageList.getItems().clear();
				List<String> names = volumeProcessor.getSliceNames();
				for (String name : names) tiffImageList.getItems().add(name);
			}
		}
		else if (id.compareTo("btnOpenRawSource") == 0) {
			result = volumeProcessor.open("RAW");
			if (result != null) txtRawPath.setText(result);
		}

		if (result == null) {
			Alert alert = new Alert(Alert.AlertType.WARNING);
			alert.setTitle("Warning");
			alert.setHeaderText("Something when wrong while opening volume");
			alert.show();
		}
	}

	@FXML
	private void setRawDimension(ActionEvent event) {
		if (volumeProcessor.inRawMode() == false) {
			Alert alert = new Alert(Alert.AlertType.ERROR);
			alert.setTitle(".raw missing");
			alert.setHeaderText("Please choose a .raw file first. :)");
			alert.show();
			return;
		}
		try {
			int x = (int)Integer.parseInt(txtRawX.getText());
			int y = (int)Integer.parseInt(txtRawY.getText());
			int z = (int)Integer.parseInt(txtRawZ.getText());

			if (x <= 0 || y <= 0 || z <= 0) {
				Alert alert = new Alert(Alert.AlertType.ERROR);
				alert.setTitle("Dimensions too small");
				alert.setHeaderText("Please choose x, y, and z to be greater than or equal to 8. :)");
				alert.show();
				return;
			}

			// Add entries to list
			volumeProcessor.setDimensions(x, y, z);
			List<String> names = volumeProcessor.getSliceNames();
			for (String name : names) rawImageList.getItems().add(name);

			lblFileSize.setText(volumeProcessor.getCurvedFileSize());
			lblNumLevels.setText(Integer.toString(volumeProcessor.getNumLevels()));
			lblNumBricks.setText(Integer.toString(volumeProcessor.getNumBricks()));

			// Create the 3D slices for visual feedback
			int[] numBricks = volumeProcessor.getNumBricksPerDim();
			int dimSize = volumeProcessor.getDimWidth();
			brickPreview.createBricks(numBricks[0], numBricks[1], numBricks[2], dimSize);
		} catch (Exception e) {
			Alert alert = new Alert(Alert.AlertType.ERROR);
			alert.setTitle("Error");
			alert.setHeaderText("Sorry, this is all I got...\n" + e.toString());
			alert.show();
		};
	}

	@FXML
	private void toggleMask(ActionEvent event) {
		Boolean selected = isMasked.isSelected();
		slider.setDisable(!selected);
		volumeProcessor.updatePreview(preview, selected);
	}

	@FXML
	private void generate(ActionEvent event) throws IOException {
		int maxBrickWidth;
		int minBrickWidth;
		try {
			maxBrickWidth = Integer.parseInt(maxDimWidthChoice.getValue().toString());
			minBrickWidth = Integer.parseInt(minDimWidthChoice.getValue().toString());
		}catch (Exception e) {
			Alert alert = new Alert(Alert.AlertType.ERROR);
			alert.setTitle("Error");
			alert.setHeaderText("Please select a valid number for both max and min brick size.");
			alert.show();
			return;
		}

		if (maxBrickWidth < minBrickWidth) {
			Alert alert = new Alert(Alert.AlertType.ERROR);
			alert.setTitle("Error");
			alert.setHeaderText("Max brick size must be greater than or equal to min brick size.");
			alert.show();
			return;
		}


		volumeProcessor.setMaxDimWidth(maxBrickWidth);
		volumeProcessor.setMinDimWidth(minBrickWidth);

		return;

//		new Thread(() -> {
//			try {
//				int xSize = Integer.parseInt(txtRawX.getText());
//				int ySize = Integer.parseInt(txtRawY.getText());
//				int zSize = Integer.parseInt(txtRawZ.getText());
//				int x2 = CLFW.NextPow2(xSize);
//				int y2 = CLFW.NextPow2(ySize);
//				int z2 = CLFW.NextPow2(zSize);
//
//				int maxDim = Math.max(Math.max(x2, y2), z2);
//				int minDim = Math.min(Math.min(x2, y2), z2);
//
//				int xBricks = x2/minDim;
//				int yBricks = y2/minDim;
//				int zBricks = z2/minDim;
//
//				int brickSize = minDim * minDim * minDim;
//				int totalLevels = (int)(Math.log(minDim * minDim * minDim) / Math.log(8));
//				int totalBricks = (x2 / minDim) * (y2 / minDim) * (z2 / minDim);
//
//				HMorton3D hMorton = new HMorton3D(totalLevels);
//
//				RandomAccessFile raw = new RandomAccessFile(rawFile, "r");
//
//				for (int yb = 0; yb < yBricks; ++yb) {
//					for (int zb = 0; zb < zBricks; ++zb) {
//						for (int xb = 0; xb < xBricks; ++xb) {
//							for (int i = 0; i < minDim; ++i) {
//								brickPreview.hideSlice(xb, yb, zb, i);
////									boxes[xb][yb][zb][i].setMaterial(new PhongMaterial(Color.BLACK));
//							}
//						}
//					}
//				}
//
//				// For each brick
//				for (int yb = 0; yb < yBricks; ++yb) {
//					for (int zb = 0; zb < zBricks; ++zb) {
//						for (int xb = 0; xb < xBricks; ++xb) {
//							int xOffset = xb * minDim;
//							int yOffset = yb * minDim;
//							int zOffset = zb * minDim;
//
//							// allocate a file
//							//File f = new File("test" + xb + "_" + yb + "_"  + zb + ".hz");
//							//f.createNewFile();
//							//RandomAccessFile hz = new RandomAccessFile(f, "rw");
//							//hz.setLength(brickSize);
//
//							//byte[] result = new byte[brickSize];
//
//							// For each slice
//							for (int y = 0; y < minDim; y++) {
//								brickPreview.toggleSlice(xb, yb, zb, y);
//
//								//									boxes[xb][yb][zb][y].setMaterial(new PhongMaterial(Color.GREEN));
//
//								for (int z = 0; z < zSize; z++) {
//									for (int x = 0; x < xSize; x++) {
////											int actualX = xOffset + x;
////											int actualY = yOffset + y;
////											int actualZ = zOffset + z;
//
////											raw.seek(actualX + (actualZ * xSize) + (actualY * (xSize * zSize)));
////											byte data = raw.readByte();
//										long newIndex = hMorton.encode(x, y, z);
////											result[(int)newIndex] = data;
//									}
//								}
//							}
//
////								hz.write(result);
////								hz.close();
////								raw.close();
//						}
//					}
//				}
//
//				for (int yb = 0; yb < yBricks; ++yb) {
//					for (int zb = 0; zb < zBricks; ++zb) {
//						for (int xb = 0; xb < xBricks; ++xb) {
//							for (int i = 0; i < minDim; ++i) {
//								brickPreview.toggleSlice(xb, yb, zb, i);
////									boxes[xb][yb][zb][i].setMaterial(new PhongMaterial(Color.WHITE));
//							}
//						}
//					}
//				}
//			}
//			catch (IOException e) {}
//		}).start();
	}
}
