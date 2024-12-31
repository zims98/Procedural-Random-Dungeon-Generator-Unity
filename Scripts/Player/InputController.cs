using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    InputSystem_Actions playerInput;

    PlayerMovement playerMovement;
    PlayerLook playerLook;

    bool canMoveCamera = true;
    bool isUIActive = false;

    [SerializeField] GameObject uiPanel;

    void Awake()
    {
        playerInput = new InputSystem_Actions();

        playerInput.Player.Enable(); // UI Action Map enabled by default until Player object is enabled
    }  

    void OnEnable()
    {       
        // Menu (UI)    
        playerInput.UI.ToggleMenu.performed += ctx => OnToggleMenu();
        playerInput.Player.ToggleMenu.performed += ctx => OnToggleMenu();
    }

    void OnDisable()
    {
        if (playerMovement != null)
        {
            // Movement
            playerInput.Player.Jump.performed -= ctx => playerMovement.Jump();
            playerInput.Player.Sprint.performed -= playerMovement.OnSprint;
            playerInput.Player.Sprint.canceled -= playerMovement.OnSprint;
        }       

        // Menu (UI)
        playerInput.Player.ToggleMenu.performed -= ctx => OnToggleMenu();
        playerInput.UI.ToggleMenu.performed -= ctx => OnToggleMenu();
    }

    void Update()
    {
        if (playerMovement == null || playerLook == null)
            return;

        Vector2 movementInput = playerInput.Player.Move.ReadValue<Vector2>();
        playerMovement.ApplyMovement(movementInput);

        if (Input.GetKeyDown(KeyCode.Escape)) // Temp. code
            canMoveCamera = false;

        if (canMoveCamera)
        {
            Vector2 lookInput = playerInput.Player.Look.ReadValue<Vector2>();
            playerLook.ApplyLook(lookInput);
        }      
    }

    public void OnToggleMenu()
    {
        isUIActive = !isUIActive;

        if (uiPanel != null)
        {
            if (isUIActive)
            {
                // Disable gameplay input and enable UI
                playerInput.UI.Enable();
                playerInput.Player.Disable();

                // Show cursor
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                // Enable UI panel
                uiPanel.SetActive(true);
            }
            else
            {
                // Disable UI input and enable gameplay
                playerInput.Player.Enable();
                playerInput.UI.Disable();

                // Hide and lock cursor
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                // Disable UI panel
                uiPanel.SetActive(false);
            }
        }        
    }

    public void InitializePlayer(GameObject player)
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        playerLook = player.GetComponent<PlayerLook>();

        // Movement, we subscribe here instead of in OnEnable because we need to get reference to the Player components first
        playerInput.Player.Jump.performed += ctx => playerMovement.Jump();
        playerInput.Player.Sprint.performed += playerMovement.OnSprint;
        playerInput.Player.Sprint.canceled += playerMovement.OnSprint;       
    }

    void OnApplicationFocus(bool focus)
    {
        canMoveCamera = focus;

        Cursor.lockState = focus ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
