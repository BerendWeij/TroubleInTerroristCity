using UnityEngine;
using UnityEngine.InputSystem;

public class DeathCam : MonoBehaviour
{
    public float mouseSensitivity = .1f;
    public float moveSpeed = 5f;

    private float rotationX;
    private float rotationY; // Store horizontal rotation separately

    private float mouseX;
    private float mouseY;

    private float horizontalInput;
    private float verticalInput;

    void Update()
    {
        // Apply vertical rotation
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // Apply horizontal rotation
        rotationY += mouseX;

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        // Clamp horizontalInput and verticalInput between -1 and 1
        horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);

        Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        Vector3 moveVelocity = moveDirection.normalized * moveSpeed;

        transform.position += moveVelocity * Time.deltaTime;
    }

    public void SetLookInput(InputAction.CallbackContext context)
    {
        mouseX = context.ReadValue<Vector2>().x * mouseSensitivity;
        mouseY = context.ReadValue<Vector2>().y * mouseSensitivity;
    }

    public void SetMovementInput(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
        verticalInput = context.ReadValue<Vector2>().y;
    }
}