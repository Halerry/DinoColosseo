using UnityEngine;

public enum CardType
{
    Attack,
    Defend,
    Medicine,
    Charge,
    Equipment // New!
}

public enum EquipmentType
{
    MekaLeg,
    ProtectionGem,
    // Add more equipment types here later
}

[System.Serializable]
public class Card
{
    public CardType cardType;
    public EquipmentType equipmentType; // Only used if cardType == Equipment
    public string cardName;
    public string description;
    public Sprite cardSprite;

    // Equipment-specific properties
    public int maxUses = 0; // How many times equipment can be used (0 = infinite)
    public int currentUses = 0; // Current remaining uses

    // Constructor for regular cards
    public Card(CardType type)
    {
        cardType = type;

        switch (type)
        {
            case CardType.Attack:
                cardName = "Attack";
                description = "Deal damage to an enemy in range";
                break;
            case CardType.Defend:
                cardName = "Defend";
                description = "Block the next enemy attack";
                break;
            case CardType.Medicine:
                cardName = "Medicine";
                description = "Restore 10 HP";
                break;
            case CardType.Charge:
                cardName = "Charge";
                description = "Move up to 5 tiles and deal 10 damage to adjacent enemy";
                break;
        }
    }

    // Constructor for equipment cards
    public Card(CardType type, EquipmentType equipment)
    {
        cardType = type;
        equipmentType = equipment;

        switch (equipment)
        {
            case EquipmentType.MekaLeg:
                cardName = "Meka Leg";
                description = "Allows multiple attacks per turn";
                maxUses = 0; // Infinite uses (passive effect)
                currentUses = 0;
                break;
            case EquipmentType.ProtectionGem:
                cardName = "Protection Gem";
                description = "33% chance to auto-defend when attacked";
                maxUses = 0; // Infinite uses (passive effect)
                currentUses = 0;
                break;
        }
    }

    public void PlayCard(DinoUnit caster, DinoUnit target = null, Tile targetTile = null)
    {
        switch (cardType)
        {
            case CardType.Attack:
                if (target != null && target.team != caster.team)
                {
                    Debug.Log($"{caster.dinoName} initiates Attack card on {target.dinoName}!");

                    // Use ReactionManager for attacks
                    if (ReactionManager.Instance != null)
                    {
                        ReactionManager.Instance.InitiateAttack(caster, target, caster.attackPower);
                    }
                    else
                    {
                        caster.Attack(target);
                    }
                }
                break;

            case CardType.Defend:
                Debug.Log($"{caster.dinoName} uses Defend card! Next attack will be blocked.");
                caster.isDefending = true;
                break;

            case CardType.Medicine:
                Debug.Log($"{caster.dinoName} uses Medicine card! +10 HP");
                caster.Heal(10);
                break;

            case CardType.Charge:
                Debug.Log($"{caster.dinoName} uses Charge card!");
                break;

            case CardType.Equipment:
                Debug.Log($"{caster.dinoName} equips {cardName}!");
                // Equipment is handled by EquipmentManager
                if (EquipmentManager.Instance != null)
                {
                    EquipmentManager.Instance.EquipCard(caster, this);
                }
                break;
        }
    }
}