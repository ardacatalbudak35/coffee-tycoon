using UnityEngine;

public class UpgradeStation : Interactable
{
    public int upgradeCost = 50;
    public int level = 1;

    private void Reset() => interactionHint = "Kahve makinesini geliştir (-50)";

    public override void Interact(PlayerController player)
    {
        if (CafeGameManager.Instance == null) return;
        if (!CafeGameManager.Instance.TrySpend(upgradeCost)) return;
        level++;
        upgradeCost += 50;
        CafeGameManager.Instance.SetUpgradeLevel(level);
        interactionHint = "Makine Lv." + level + " geliştir (-" + upgradeCost + ")";
        CafeGameManager.Instance.ShowMessage("Makine seviyesi: " + level);
        transform.localScale += Vector3.one * 0.05f;
    }
}
