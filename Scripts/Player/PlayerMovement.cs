using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    CharacterController controller;
    Camera mainCamera;

    const float gravity = -9.81f;
    bool isGrounded;
    bool isSprinting;

    Vector3 yVelocity;
    Vector3 currentVelocity;
    Vector3 speedSmoothVelocity;
    Vector3 direction;

    [SerializeField] float baseSpeed = 5f;
    [SerializeField] float sprintSpeed = 10f;
    [SerializeField] float smoothSpeed = 0.1f;
    [SerializeField] float airControlSpeed = 2.5f;
    [SerializeField] float jumpHeight = 3f;  

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;     
    }

    public void ApplyMovement(Vector2 input)
    {
        if (input == null)
            return;

        direction = Vector3.zero;
        direction.x = input.x;
        direction.z = input.y;

        float targetSpeed = isSprinting ? sprintSpeed : baseSpeed;
        Vector3 targetVelocity = direction * targetSpeed;

        if (isGrounded)
        {
            currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref speedSmoothVelocity, smoothSpeed);
        }
        else
        {
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, airControlSpeed * Time.deltaTime);
        }        

        controller.Move(transform.TransformDirection(currentVelocity) * Time.deltaTime); // Moves with CharacterController based on direction/input.

        if (isGrounded && yVelocity.y < 0) // Ensures the Player is pulled downwards with a constant small amount of force.
        {
            yVelocity.y = -10f; // -2 Default
        }

        yVelocity.y += gravity * 2.5f * Time.deltaTime;
        controller.Move(yVelocity * Time.deltaTime); // Adding vertical velocity movement to the CharacterController.

        if (isSprinting && currentVelocity.magnitude > 0.1f) // Change camera FOV if sprint button is being pressed down AND Player isn't standing still
        {           
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 70f, 10f * Time.deltaTime);
        }
        else
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 60f, 10f * Time.deltaTime);
        }
    }

    public void Jump()
    {
        if (isGrounded)
        {
            yVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);                        
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }
}
