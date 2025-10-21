using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image equipmentIcon;
    public TextMeshProUGUI equipmentNameText;
    public TextMeshProUGUI usesText; // For equipment with limited uses
    public GameObject emptySlotIndicator;

    private Card equippedCard;
    private int slotIndex;

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public void SetEquipment(Card equipment)
    {
        equippedCard = equipment;

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);

        if (equipmentNameText != null)
            equipmentNameText.text = equipment.cardName;

        // Set color based on equipment type
        if (equipmentIcon != null)
        {
            equipmentIcon.enabled = true;
            equipmentIcon.color = GetEquipmentColor(equipment.equipmentType);
        }

        // Show uses if applicable
        if (usesText != null)
        {
            if (equipment.maxUses > 0)
            {
                usesText.gameObject.SetActive(true);
                usesText.text = $"{equipment.currentUses}/{equipment.maxUses}";
            }
            else
            {
                usesText.gameObject.SetActive(false);
            }
        }
    }

    public void ClearSlot()
    {
        equippedCard = null;

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        if (equipmentNameText != null)
            equipmentNameText.text = "Empty";

        if (equipmentIcon != null)
            equipmentIcon.enabled = false;

        if (usesText != null)
            usesText.gameObject.SetActive(false);
    }

    Color GetEquipmentColor(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.MekaLeg:
                return new Color(1f, 0.5f, 0f); // Orange
            case EquipmentType.ProtectionGem:
                return new Color(0.5f, 0.5f, 1f); // Light Blue
            default:
                return Color.gray;
        }
    }

    public Card GetEquippedCard()
    {
        return equippedCard;
    }
}