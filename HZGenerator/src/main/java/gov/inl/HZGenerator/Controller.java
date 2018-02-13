package gov.inl.HZGenerator;/* ImageIO */
import java.io.*;

import gov.inl.HZGenerator.BrickFactory.*;
import javafx.application.Platform;
import javafx.scene.control.*;
import javafx.scene.control.Label;
import javafx.scene.control.TextField;
import javafx.scene.image.ImageView;

import javafx.event.ActionEvent;
import javafx.fxml.FXML;
import javafx.scene.layout.AnchorPane;
import javafx.scene.layout.Pane;
import javafx.scene.paint.Color;
import javafx.stage.DirectoryChooser;
import javafx.stage.FileChooser;
import javafx.stage.Stage;

import java.util.*;

import static java.lang.Thread.sleep;


/**
 * Nate Morrical. Summer 2017.
 * The following is an FXML controller, which operates the brick factory through FXML events.
 */
public class Controller {
	private BrickFactory brickFactory;
	private VolumePreview volumePreview;
	private SlicePreview slicePreview;

	int sliceIdx = -1;

	/* FXML Controls */
	@FXML public AnchorPane rootPane;
	@FXML private TextField txtTiffPath;
	@FXML private TextField txtRawPath;
	@FXML private TextField txtResultPath;
	@FXML private ListView<String> sliceList;
	@FXML private ImageView preview;
	@FXML private Slider intensitySlider;
	@FXML private CheckBox mapTo8BPP;
	@FXML private Pane brickPane;
	@FXML Label lblFileSize;
	@FXML Label lblNumBricks;
	@FXML TextField txtRawXSize;
	@FXML TextField txtRawYSize;
	@FXML TextField txtRawZSize;
	@FXML ChoiceBox maxDimWidthChoice;
	@FXML ChoiceBox minDimWidthChoice;
	@FXML ChoiceBox rawBitsPerPixel;
	@FXML TabPane fileTabPane;
	@FXML Tab RAWTab;
	@FXML Tab TIFFTab;
	@FXML Button btnGenerate;

	/* Initializes the brick pane, volume processor, and sets up some FXML elements */
	@FXML public void initialize() throws Exception {
		/* Initialize the brick factory */
		brickFactory = new BrickFactory();

		/* Initialize previews */
		volumePreview = new VolumePreview(brickPane);
		slicePreview = new SlicePreview(preview);

		/* When the user selects a slice from the list of slices, update the volume preview and slice preview */
		sliceList.getSelectionModel().selectedItemProperty().addListener((v, oldValue, newValue) -> {
			int previousSlice = sliceIdx;
			sliceIdx = sliceList.getSelectionModel().getSelectedIndex();
			if (sliceIdx < 0) return;
//			volumePreview.toggleLayer(sliceIdx, true);
//			volumePreview.toggleLayer(previousSlice, false);
			slicePreview.show(brickFactory.volume.getSlice(sliceIdx));
		});

		/* When the threshold slider changes, update the slice */
//		slider.valueProperty().addListener((observable, oldValue, newValue) -> {
//			brickFactory.settings.threshold = newValue.floatValue();
//			slicePreview.show(brickFactory.getSlice(sliceIdx));
//		});

		/* When the preview intensity changes, update the slice*/
		intensitySlider.valueProperty().addListener((observable, oldValue, newValue) -> {
			slicePreview.previewIntensity = newValue.floatValue();
			slicePreview.show(brickFactory.volume.getSlice(sliceIdx));
		});

		/* For raw volumes, validate the field to take only digits. */
		txtRawXSize.textProperty().addListener((observable, oldValue, newValue) -> {
			if (!newValue.matches("\\d*")) {
				txtRawXSize.setText(newValue.replaceAll("[^\\d]", ""));
			}
		});
		txtRawYSize.textProperty().addListener((observable, oldValue, newValue) -> {
			if (!newValue.matches("\\d*")) {
				txtRawYSize.setText(newValue.replaceAll("[^\\d]", ""));
			}
		});
		txtRawZSize.textProperty().addListener((observable, oldValue, newValue) -> {
			if (!newValue.matches("\\d*")) {
				txtRawZSize.setText(newValue.replaceAll("[^\\d]", ""));
			}
		});

		/* setup the max and min brick size drop down. Note: currently restricted to [8, 512]*/
		for (int i = 512; i >= 4; i>>=1) {
			maxDimWidthChoice.getItems().add(i);
			minDimWidthChoice.getItems().add(i);
		}
		minDimWidthChoice.getSelectionModel().select(3);
		maxDimWidthChoice.getSelectionModel().select(0);
		minDimWidthChoice.getSelectionModel().selectedItemProperty().addListener((observable, oldValue, newValue) -> {
			int minBrickSize = Integer.parseInt(minDimWidthChoice.getSelectionModel().getSelectedItem().toString());
			int maxBrickSize = Integer.parseInt(maxDimWidthChoice.getSelectionModel().getSelectedItem().toString());
			if (minBrickSize > maxBrickSize) {
				maxBrickSize = minBrickSize;
				maxDimWidthChoice.setValue(maxBrickSize);
			}
			try {
				brickFactory.settings.setMinBrickSize(minBrickSize);
				brickFactory.settings.setMaxBrickSize(maxBrickSize);
				if (brickFactory.volume != null) {
					List<Brick> bricks = brickFactory.getBrickPartitions();
					volumePreview.show(brickFactory.volume, bricks);
					lblFileSize.setText(brickFactory.getResultFileSize());
					lblNumBricks.setText(bricks.size() + "");
					/* Its possible that all bricks are larger than the min or smaller than max . */
					int actualMinBrickSize = bricks.get(0).getSize();
					int actualMaxBrickSize = bricks.get(0).getSize();
					for (int i = 1; i < bricks.size(); ++i) {
						actualMinBrickSize = Integer.min(actualMinBrickSize, bricks.get(i).getSize());
						actualMaxBrickSize = Integer.max(actualMaxBrickSize, bricks.get(i).getSize());
					}
					minDimWidthChoice.setValue(actualMinBrickSize);
					maxDimWidthChoice.setValue(actualMaxBrickSize);
				}
			} catch (Exception e) {
				e.printStackTrace();
			}
		});
		maxDimWidthChoice.getSelectionModel().selectedItemProperty().addListener((observable, oldValue, newValue) -> {
			int minBrickSize = Integer.parseInt(minDimWidthChoice.getSelectionModel().getSelectedItem().toString());
			int maxBrickSize = Integer.parseInt(maxDimWidthChoice.getSelectionModel().getSelectedItem().toString());
			if (maxBrickSize < minBrickSize) {
				minBrickSize = maxBrickSize;
				minDimWidthChoice.setValue(minBrickSize);
			}
			try {
				brickFactory.settings.setMinBrickSize(minBrickSize);
				brickFactory.settings.setMaxBrickSize(maxBrickSize);
				if (brickFactory.volume != null) {
					List<Brick> bricks = brickFactory.getBrickPartitions();
					volumePreview.show(brickFactory.volume, bricks);
					lblFileSize.setText(brickFactory.getResultFileSize());
					lblNumBricks.setText(bricks.size() + "");
					/* Its possible that all bricks are larger than the min or smaller than max . */
					int actualMinBrickSize = bricks.get(0).getSize();
					int actualMaxBrickSize = bricks.get(0).getSize();
					for (int i = 1; i < bricks.size(); ++i) {
						actualMinBrickSize = Integer.min(actualMinBrickSize, bricks.get(i).getSize());
						actualMaxBrickSize = Integer.max(actualMaxBrickSize, bricks.get(i).getSize());
					}
					minDimWidthChoice.setValue(actualMinBrickSize);
					maxDimWidthChoice.setValue(actualMaxBrickSize);
				}
			} catch (Exception e) {
				e.printStackTrace();
			}
		});

		rawBitsPerPixel.getItems().add("8");
		rawBitsPerPixel.getItems().add("16");
		rawBitsPerPixel.getSelectionModel().select(0);
	}

	/* Creates a dialogue and sets the source path for the appropriate type selection */
	@FXML private void chooseSource(ActionEvent event) throws Exception {
		/* Determine which button was pressed. */
		String id = ((Button)event.getSource()).getId();
		String result = null;

		File selectedLocation = null;

		/* Get the selected location */
		switch (id) {
			case "btnOpenTiffVolume":
				selectedLocation = openFolder();
				txtTiffPath.setText((selectedLocation == null) ? null : selectedLocation.getPath());
				break;
			case "btnOpenRawSource":
				selectedLocation = openFile("RAW files (*.raw)", "*.raw", "*.RAW");
				txtRawPath.setText((selectedLocation == null) ? null : selectedLocation.getPath());
				break;
		}
	}

	/* Creates a dialogue and sets the destination path */
	@FXML private void chooseDestination(ActionEvent event) throws Exception {
		File selectedLocation = openFolder();
		if (selectedLocation != null) {
			txtResultPath.setText(selectedLocation.getPath());
			brickFactory.settings.outputPath = selectedLocation.getPath();
		}
	}

	/* Returns either a chosen folder (File in code) or null */
	private File openFolder() {
		DirectoryChooser dc = new DirectoryChooser();
		return dc.showDialog(new Stage());
	}

	/* Returns either a chosen file or null */
	private File openFile(String description, String... extensions) {
		FileChooser.ExtensionFilter extFilter =
				new FileChooser.ExtensionFilter(description, extensions);
		FileChooser fc = new FileChooser();
		fc.getExtensionFilters().add(extFilter);
		return fc.showOpenDialog(new Stage());
	}

	/* Uses the provided information to load the volume, initialize previews, etc */
	@FXML private void loadVolume(ActionEvent event) {
		/* Get the selected tab id */
		String id = fileTabPane.getSelectionModel().getSelectedItem().getId();
		List<String> sliceNames = null;
		String srcPath;
		int x, y, z;
		try {
			switch (id) {
				case "TIFFTab" :
					srcPath = txtTiffPath.getText();
					sliceNames = brickFactory.openTiff(srcPath);
					break;
				case "RAWTab" :
					srcPath = txtRawPath.getText();
					x = Integer.parseInt(txtRawXSize.getText());
					y = Integer.parseInt(txtRawYSize.getText());
					z = Integer.parseInt(txtRawZSize.getText());
					int bpp = Integer.parseInt(rawBitsPerPixel.getValue().toString());
					if (x <= 0 || y <= 0 || z <= 0) {
						showAlert(Alert.AlertType.ERROR, "Dimensions too small",
								"Please choose x, y, and z to be greater than or equal to 8. :)" );
						return;
					}
					sliceNames = brickFactory.openRaw(srcPath, x, y, z, bpp);
					break;
			}
		} catch (Exception e) {
			showAlert(Alert.AlertType.ERROR, "error",
					"some required information was not provided or was incorrect. ");
			/* Todo: give more verbose information here */
			return;
		}
		/* If no slices were found, do nothing */
		if (sliceNames == null) return;

		/* get minimum and maximum brick sizes. */
		int minBrickSize = Integer.parseInt(minDimWidthChoice.getSelectionModel().getSelectedItem().toString());
		int maxBrickSize = Integer.parseInt(maxDimWidthChoice.getSelectionModel().getSelectedItem().toString());
		try {
			brickFactory.settings.setMinBrickSize(minBrickSize);
			brickFactory.settings.setMaxBrickSize(maxBrickSize);
		} catch (Exception e) {
			e.printStackTrace();
		}

		/* Determine if the volume can be scaled to 8bpp */
		if (brickFactory.volume.getBytesPerPixel() <= 1)
			mapTo8BPP.setDisable(true);
		else
			mapTo8BPP.setDisable(false);

		/* Update the slice list */
		try {
			sliceList.getItems().clear();
			for (String s : sliceNames) sliceList.getItems().add(s);
			sliceIdx = 0;
			List<Brick> bricks = brickFactory.getBrickPartitions();
			volumePreview.show(brickFactory.volume, brickFactory.getBrickPartitions());
			slicePreview.show(brickFactory.volume.getSlice(0));

			/* Its possible that all bricks are larger than the min or smaller than max . */
			int actualMinBrickSize = bricks.get(0).getSize();
			int actualMaxBrickSize = bricks.get(0).getSize();
			for (int i = 1; i < bricks.size(); ++i) {
				actualMinBrickSize = Integer.min(actualMinBrickSize, bricks.get(i).getSize());
				actualMaxBrickSize = Integer.max(actualMaxBrickSize, bricks.get(i).getSize());
			}
			minDimWidthChoice.setValue(actualMinBrickSize);
			maxDimWidthChoice.setValue(actualMaxBrickSize);

			lblFileSize.setText(brickFactory.getResultFileSize());
			lblNumBricks.setText(brickFactory.getBrickPartitions().size() + "");
		} catch (Exception e) {
			showAlert(Alert.AlertType.ERROR, "error",
					"something went wrong while updating the visual previews: " + e.getMessage());
		}
	}

	/* Toggles whether to scale a 16 bit gray to 8 bits before saving */
	@FXML private void toggleMapTo8BPP(ActionEvent event) {
		try {
			brickFactory.settings.mapTo8BPP = mapTo8BPP.isSelected();
			lblFileSize.setText(brickFactory.getResultFileSize());
			lblNumBricks.setText(brickFactory.getBrickPartitions().size() + "");
		} catch (Exception e) {
			e.printStackTrace();
		}
	}

	/* Generates the curved volume, storing at the selected destination */
	@FXML private void generate(ActionEvent event) throws IOException {
		if (brickFactory.volume == null) {
			showAlert(Alert.AlertType.ERROR, "no volume loaded.", "Please load a volume before generating bricks");
			return;
		}

		File f = new File(txtResultPath.getText());
		if (!f.exists()) {
			showAlert(Alert.AlertType.ERROR, "Destination does not exist", "Please choose a valid destination");
			return;
		}
		brickFactory.settings.outputPath = txtResultPath.getText();

		// Get the current time for profiling purposes
		long startTime = System.currentTimeMillis();
		
		// At this point, we're guaranteed a volume is loaded and the destination directory exists
		btnGenerate.setDisable(true);
		btnGenerate.setText("Generating...");
		brickFactory.generateBricksAsync();

		volumePreview.setAll(Color.rgb(0 ,0, 0, 0));

		Thread t = new Thread(() -> {
			int currentBrick = 0;
			volumePreview.colorBrick(currentBrick, Color.rgb(255, 255, 255, 1));
			Platform.runLater(() -> btnGenerate.setText(0 + "/" + brickFactory.getBrickPartitions().size()));

			while (brickFactory.getApportionerStatus() == false) {
				try {
					int totalBricksDone = brickFactory.getTotalBricksApportioned();
					if (totalBricksDone != currentBrick) {
						currentBrick = totalBricksDone;
						final int selected = totalBricksDone;
						Platform.runLater(() -> volumePreview.colorBrick(selected, Color.rgb(255, 255, 255, 1)));
						Platform.runLater(() -> btnGenerate.setText(totalBricksDone + "/" + brickFactory.getBrickPartitions().size()));

					}
					Thread.sleep(16);
				} catch (InterruptedException e) {
					e.printStackTrace();
				}
			}
			Platform.runLater(() -> {
				btnGenerate.setDisable(false);
				btnGenerate.setText("Generate");
				volumePreview.show(brickFactory.volume, brickFactory.getBrickPartitions());
			});
			
			long endTime = System.currentTimeMillis();
			List<String> lines = Arrays.asList("Start Time: ", startTime.toString(), 
											   " End Time: ", endTime.toString(), 
											   " Duration: ", (endTime - startTime).toString());
			Path timerLog = Paths.get("timer_log.txt");
			Files.write(timerLog, lines, Charset.forName("UTF-8"));
		});
		t.setDaemon(true);
		t.start();

	}

	/* Shows a message box for validation purposes */
	private void showAlert(Alert.AlertType type, String title, String text) {
		Alert alert = new Alert(type);
		alert.setTitle(title);
		alert.setHeaderText(text);
		alert.show();
	}
}
