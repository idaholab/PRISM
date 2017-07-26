package gov.inl.HZGenerator;

import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.stage.Stage;

import java.io.File;

import static org.jocl.CL.*;

/**
 * Nate Morrical. Summer 2017
 *
 * MainClass
 *   Main entry point for the HZ Generator application. Initializes CLFW and the FXML GUI
 */
public class MainClass extends Application {
	Stage stage;

	/* Simply launches the application */
	public static void main(String[] args) {
		Application.launch(MainClass.class, args);
	}

	/* Initializes OpenCL and FXML GUI */
	@Override public void start(Stage stage) throws Exception {
		// Initialize OpenCL
//		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath().substring(5));
		if (CLFW.Initialize("Kernels/OpenCLSettings.json", "Kernels") != CL_SUCCESS) {
			System.out.println("OpenCL failed to initialize.");
			return;
		}

		/* Initialize FXML GUI  */
		FXMLLoader loader = new FXMLLoader(getClass().getClassLoader().getResource("HZGeneratorGUI.fxml"));
		Parent root = (Parent)loader.load();

		this.stage = stage;
		stage.setTitle("HZ Volume Generator");
		Scene scene = new Scene(root, 1080, 517);
		scene.getStylesheets().add("HZGeneratorGUI.css");
		stage.setScene(scene);
		stage.show();
	}
}
