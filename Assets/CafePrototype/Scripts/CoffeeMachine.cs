using UnityEngine;

public class CoffeeMachine : Interactable
{
    public int coffeePrice = 8;
    public float cooldown = 1.5f;
    private float nextUse;

    private void Reset() => interactionHint = "Kahve hazırla (+8 coin)";

    public override void Interact(PlayerController player)
    {
        if (Time.time < nextUse) return;
        nextUse = Time.time + cooldown;
        CafeGameManager.Instance?.AddMoney(coffeePrice);
    }
}
