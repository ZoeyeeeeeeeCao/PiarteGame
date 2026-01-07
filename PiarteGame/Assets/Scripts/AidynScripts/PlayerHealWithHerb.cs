using UnityEngine;

public class PlayerHealWithHerb : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData herbItem;
    [SerializeField] private int herbCost = 1;

    [Header("Input")]
    [SerializeField] private KeyCode healKey = KeyCode.H;

    private void Update()
    {
        if (Input.GetKeyDown(healKey))
            TryHeal();
    }

    public bool TryHeal()
    {
        var health = PlayerHealthController.Instance;
        if (health == null) return false;

        if (health.IsDead)
            return false;

        if (health.IsFullHealth)
            return false;

        if (!StaticInventory.Has(herbItem, herbCost))
            return false;

        // Spend herb
        if (!StaticInventory.Remove(herbItem, herbCost))
            return false;

        // Heal
        float healAmount = herbItem.healAmount > 0 ? herbItem.healAmount : 20f;
        health.Heal(healAmount);



        Debug.Log($"Used {herbItem.displayName}, healed {healAmount}");
        return true;
    }
}
