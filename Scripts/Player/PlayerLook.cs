using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    Camera cam;

    float xRotation = 0f;

    [SerializeField] float sensitivity = 0.05f;

    void Awake()
    {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ApplyLook(Vector2 input)
    {
        if (input == null)
            return;

        float mouseX = input.x * sensitivity;
        float mouseY = input.y * sensitivity;      

        // Calculate camera rotation for up and down.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotation to camera.
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // Apply rotation to player body.
        transform.Rotate(Vector3.up * mouseX);
    }

    public float GetXRotation()
    {
        return xRotation;
    }
}
