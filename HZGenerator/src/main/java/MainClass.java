/**
 * Created by BITINK on 5/19/2017.
 */



import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.stage.Stage;

import java.io.File;
import java.nio.file.Paths;

import static org.jocl.CL.*;


public class MainClass extends Application {
	Stage stage;

	public static void main(String[] args) {
		Application.launch(MainClass.class, args);
	}

	@Override
	public void start(Stage stage) throws Exception {
		// Initialize OpenCL
		File openCLSettings = new File(getClass().getClassLoader().getResource("Kernels/OpenCLSettings.json").getPath());
		if (CLFW.Initialize(openCLSettings) != CL_SUCCESS) {
			System.out.println("OpenCL failed to initialize.");
			return;
		}

		FXMLLoader loader = new FXMLLoader(getClass().getClassLoader().getResource("HZGeneratorGUI.fxml"));
		Parent root = (Parent)loader.load();

		this.stage = stage;
		stage.setTitle("HZ Volume Generator");
		Scene scene = new Scene(root, 1080, 505);
		scene.getStylesheets().add("HZGeneratorGUI.css");
		stage.setScene(scene);
		stage.show();
	}
}
