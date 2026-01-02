using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopSwitch : MonoBehaviour
{
    public GameModeManager gameModeManager;

    public void OpenShopButton()
    {
        gameModeManager.OpenShop();
    }

    public void CloseShopButton()
    {
        gameModeManager.CloseShop();
    }
}
