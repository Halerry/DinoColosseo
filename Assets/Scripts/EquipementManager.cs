using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("Settings")]
    public int maxEquipmentSlots = 4;

    [Header("UI References")]
    public Transform playerEquipmentContainer; // Where to show player's equipped items
    public GameObject equipmentSlotPrefab; // UI prefab for equipment slots

    // Equipment for each unit
    private Dictionary<DinoUnit, List<Card>> unitEquipment = new Dictionary<DinoUnit, List<Card>>();

    // UI slots for player equipment
    private List<EquipmentSlotUI> playerEquipmentSlots = new List<EquipmentSlotUI>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializePlayerEquipmentUI();
    }

    void InitializePlayerEquipmentUI()
    {
        if (playerEquipmentContainer == null || equipmentSlotPrefab == null)
        {
            Debug.LogWarning("Equipment UI not set up!");
            return;
        }

        // Create 4 empty equipment slots
        for (int i = 0; i < maxEquipmentSlots; i++)
        {
            GameObject slotObj = Instantiate(equipmentSlotPrefab, playerEquipmentContainer);
            EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();

            if (slotUI != null)
            {
                slotUI.SetSlotIndex(i);
                playerEquipmentSlots.Add(slotUI);
            }
        }

        Debug.Log($"Created {maxEquipmentSlots} equipment slots");
    }

    public void EquipCard(DinoUnit unit, Card equipmentCard)
    {
        // Initialize equipment list for this unit if needed
        if (!unitEquipment.ContainsKey(unit))
        {
            unitEquipment[unit] = new List<Card>();
        }

        List<Card> equipment = unitEquipment[unit];

        // If at max capacity, need to replace
        if (equipment.Count >= maxEquipmentSlots)
        {
            if (unit.team == Team.Player)
            {
                // Show replacement UI for player
                ShowReplacementUI(unit, equipmentCard);
            }
            else
            {
                // AI automatically replaces oldest equipment
                Card oldestEquipment = equipment[0];
                equipment.RemoveAt(0);
                Debug.Log($"AI {unit.dinoName} replaced {oldestEquipment.cardName} with {equipmentCard.cardName}");
                equipment.Add(equipmentCard);
                ApplyEquipmentEffect(unit, equipmentCard);
            }
        }
        else
        {
            // Add equipment
            equipment.Add(equipmentCard);
            Debug.Log($"{unit.dinoName} equipped {equipmentCard.cardName}! ({equipment.Count}/{maxEquipmentSlots})");

            ApplyEquipmentEffect(unit, equipmentCard);

            // Update UI for player
            if (unit.team == Team.Player)
            {
                UpdatePlayerEquipmentUI(unit);
            }
        }
    }

    public void ReplaceEquipment(DinoUnit unit, int slotIndex, Card newEquipment)
    {
        if (!unitEquipment.ContainsKey(unit)) return;

        List<Card> equipment = unitEquipment[unit];

        if (slotIndex >= 0 && slotIndex < equipment.Count)
        {
            Card oldEquipment = equipment[slotIndex];

            // Remove old equipment effects
            RemoveEquipmentEffect(unit, oldEquipment);

            // Replace with new
            equipment[slotIndex] = newEquipment;

            Debug.Log($"{unit.dinoName} replaced {oldEquipment.cardName} with {newEquipment.cardName}");

            // Apply new equipment effects
            ApplyEquipmentEffect(unit, newEquipment);

            // Update UI
            if (unit.team == Team.Player)
            {
                UpdatePlayerEquipmentUI(unit);
            }

            // Discard old equipment
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.DiscardCard(oldEquipment);
            }
        }
    }

    void ApplyEquipmentEffect(DinoUnit unit, Card equipment)
    {
        switch (equipment.equipmentType)
        {
            case EquipmentType.MekaLeg:
                // Meka Leg allows multiple attacks - this is checked in DinoUnit
                Debug.Log($"✓ {unit.dinoName} can now attack multiple times per turn!");
                break;
            case EquipmentType.ProtectionGem:
                // Protection Gem gives 33% chance to auto-defend - checked in ReactionManager
                Debug.Log($"✓ {unit.dinoName} has 33% chance to auto-defend when attacked!");
                break;
        }
    }

    void RemoveEquipmentEffect(DinoUnit unit, Card equipment)
    {
        switch (equipment.equipmentType)
        {
            case EquipmentType.MekaLeg:
                Debug.Log($"✗ {unit.dinoName} lost multiple attack ability");
                break;
            case EquipmentType.ProtectionGem:
                Debug.Log($"✗ {unit.dinoName} lost auto-defend ability");
                break;
        }
    }

    void UpdatePlayerEquipmentUI(DinoUnit unit)
    {
        if (!unitEquipment.ContainsKey(unit)) return;

        List<Card> equipment = unitEquipment[unit];

        for (int i = 0; i < playerEquipmentSlots.Count; i++)
        {
            if (i < equipment.Count)
            {
                playerEquipmentSlots[i].SetEquipment(equipment[i]);
            }
            else
            {
                playerEquipmentSlots[i].ClearSlot();
            }
        }
    }

    void ShowReplacementUI(DinoUnit unit, Card newEquipment)
    {
        // For now, just replace the first equipment
        // TODO: Create a proper UI for player to choose which to replace
        Debug.Log("Equipment slots full! Replacing first equipment...");
        ReplaceEquipment(unit, 0, newEquipment);
    }

    // Check if unit has specific equipment
    public bool HasEquipment(DinoUnit unit, EquipmentType equipType)
    {
        if (!unitEquipment.ContainsKey(unit)) return false;

        return unitEquipment[unit].Any(e => e.equipmentType == equipType);
    }

    // Get all equipment for a unit
    public List<Card> GetEquipment(DinoUnit unit)
    {
        if (!unitEquipment.ContainsKey(unit))
            return new List<Card>();

        return new List<Card>(unitEquipment[unit]);
    }

    // Clear equipment when unit dies
    public void ClearEquipment(DinoUnit unit)
    {
        if (unitEquipment.ContainsKey(unit))
        {
            unitEquipment.Remove(unit);
        }
    }
}