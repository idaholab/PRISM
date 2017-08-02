using UnityEngine;
using UnityEngine.UI;

/* Color Palette | Marko Sterbentz 6/30/2017
 * This script handles user input on a color palette pop-up menu.
 */
public class ColorPalette : MonoBehaviour
{
	private TransferFunction transferFunction;              // Reference to the transfer function that is in the volumeController
	private VolumeController volumeController;              // The main controller used to synchronize data input, user input, and visualization

	// Color palette slider text
	public Text redValueText;
	public Text greenValueText;
	public Text blueValueText;

	// Color palette sliders
	public Slider redSlider;
	public Slider greenSlider;
	public Slider blueSlider;

	// The color the palette currently holds
	private Color currentColor;

	// Use this for initialization
	void Start()
	{
		// Initialize the color palette text fields
		redValueText.text = redSlider.value.ToString();
		greenValueText.text = greenSlider.value.ToString();
		blueValueText.text = blueSlider.value.ToString();

		volumeController = (VolumeController)GameObject.Find("VolumeController").GetComponent(typeof(VolumeController));
		transferFunction = volumeController.getTransferFunction();

		currentColor = new Color(0, 0, 0, 1);

		// Don't display/use the color palette on startup
		this.gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{

	}

	/* Color Palette Slider Update Functions */
	public void updateRedValue(float newVal)
	{
		redValueText.text = newVal.ToString();
		transferFunction.updateActivePoint(new Color(redSlider.value / 255.0f, greenSlider.value / 255.0f, blueSlider.value / 255.0f));
	}

	public void updateGreenValue(float newVal)
	{
		greenValueText.text = newVal.ToString();
		transferFunction.updateActivePoint(new Color(redSlider.value / 255.0f, greenSlider.value / 255.0f, blueSlider.value / 255.0f));
	}

	public void updateBlueValue(float newVal)
	{
		blueValueText.text = newVal.ToString();
		transferFunction.updateActivePoint(new Color(redSlider.value / 255.0f, greenSlider.value / 255.0f, blueSlider.value / 255.0f));
	}

	// Sets the color palette's sliders to the palette's currentColor.
	// Note: This converts from [0, 1] to [0, 255] color range.
	public void setSliders()
	{
		redSlider.value = Mathf.FloorToInt(currentColor.r * 255);
		greenSlider.value = Mathf.FloorToInt(currentColor.g * 255);
		blueSlider.value = Mathf.FloorToInt(currentColor.b * 255);
	}

	// Set the color palette's current color.
	public void setCurrentColor(Color newCurrentColor)
	{
		this.currentColor = newCurrentColor;
	}

	// Returns the currently selected color in the color palette.
	public Color getCurrentColor()
	{
		return currentColor;
	}
}
