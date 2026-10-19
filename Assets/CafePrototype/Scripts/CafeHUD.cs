using UnityEngine;
using UnityEngine.UI;

public class CafeHUD : MonoBehaviour
{
    public static CafeHUD Instance { get; private set; }
    public Text interactionText;
    public Button interactButton;

    private void Awake() => Instance = this;
    public void SetInteraction(string hint)
    {
        bool visible = !string.IsNullOrEmpty(hint);
        if (interactionText != null) interactionText.text = visible ? hint : "";
        if (interactButton != null) interactButton.gameObject.SetActive(visible);
    }
}
