/**
 * Created by BITINK on 5/19/2017.
 */



import javafx.application.Application;
import javafx.fxml.FXMLLoader;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.stage.Stage;
import static org.jocl.CL.*;


public class HZGeneratorGUI extends Application {
	Stage stage;

	public static void main(String[] args) {
		// Initialize OpenCL
		if (CLFW.Initialize() != CL_SUCCESS) {
			System.out.println("OpenCL failed to initialize.");
			return;
		}

		Application.launch(HZGeneratorGUI.class, args);
	}

	@Override
	public void start(Stage stage) throws Exception {
		FXMLLoader loader = new FXMLLoader(getClass().getClassLoader().getResource("HZGeneratorGUI.fxml"));
		Parent root = (Parent)loader.load();
		HZController controller = (HZController)loader.getController();
		controller.stage = stage;
		controller.initialize();

		this.stage = stage;
		stage.setTitle("HZ Volume Generator");
		Scene scene = new Scene(root, 1080, 505);
		scene.getStylesheets().add("HZGeneratorGUI.css");
		stage.setScene(scene);
		stage.show();
	}
}
