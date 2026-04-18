using UnityEngine;
using UnityEngine.InputSystem;

public class StartGame : MonoBehaviour
{
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private int sceneIndex;

    private bool hasStarted = false;

    private void Start()
    {
        if (levelLoader == null)
        {
            levelLoader = FindObjectOfType<LevelLoader>();
        }

        if (levelLoader == null)
        {
            Debug.LogError("LevelLoader not found in the scene.");
        }
    }

    private void Update()
    {
        if (hasStarted || levelLoader == null)
            return;

        if (AnyInputPressed())
        {
            hasStarted = true;
            levelLoader.LoadLevel(sceneIndex);
        }
    }

    private bool AnyInputPressed()
    {
        // Any keyboard key
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        // Any mouse button
        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame ||
             Mouse.current.forwardButton.wasPressedThisFrame ||
             Mouse.current.backButton.wasPressedThisFrame))
        {
            return true;
        }

        // Any common gamepad button
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame ||
                gamepad.selectButton.wasPressedThisFrame ||
                gamepad.leftShoulder.wasPressedThisFrame ||
                gamepad.rightShoulder.wasPressedThisFrame ||
                gamepad.leftStickButton.wasPressedThisFrame ||
                gamepad.rightStickButton.wasPressedThisFrame ||
                gamepad.dpad.up.wasPressedThisFrame ||
                gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame ||
                gamepad.dpad.right.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }
}