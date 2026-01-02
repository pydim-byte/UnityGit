using UnityEngine;

public class ShopController : MonoBehaviour
{
    public GameObject shopUI;

    private void OnEnable()
    {
        // Show shop UI
        shopUI.SetActive(true);

        // Unlock cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        // Hide shop UI
        shopUI.SetActive(false);

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
