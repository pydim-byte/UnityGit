using UnityEngine;

public class MouseLookTPS : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;   // CameraPivot object

    [Header("Settings")]
    public float mouseSensitivity = 200f;
    public float minY = -45f;
    public float maxY = 45f;

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate player left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
