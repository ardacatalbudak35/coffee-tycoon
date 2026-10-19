using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [TextArea] public string interactionHint = "Etkileşim";
    public virtual string GetHint() => interactionHint;
    public abstract void Interact(PlayerController player);
}
