using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public enum CardType
{
    Attack,
    Defend,
    Medicine,
    Charge
}

[System.Serializable]
public class Card
{
    public CardType cardType;
    public string cardName;
    public string description;
    public Sprite cardSprite;

    public Card(CardType type)
    {
        cardType = type;

        switch (type)
        {
            case CardType.Attack:
                cardName = "Attack";
                description = "Attack the enemy ";
                break;
            case CardType.Defend:
                cardName = "Defend";
                description = "Block the next enemy attack";
                break;
            case CardType.Medicine:
                cardName = "Medicine";
                description = "Restore 1 HP";
                break;
            case CardType.Charge:
                cardName = "Charge";
                description = "Move up to 5 tiles and deal 10 damage to adjacent enemy";
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

                    // Use ReactionManager instead of direct attack
                    if (ReactionManager.Instance != null)
                    {
                        ReactionManager.Instance.InitiateAttack(caster, target, caster.attackPower);
                    }
                    else
                    {
                        // Fallback if ReactionManager doesn't exist
                        caster.Attack(target);
                    }
                }
                break;

            case CardType.Defend:
                Debug.Log($"{caster.dinoName} uses Defend card! Next attack will be blocked.");
                caster.isDefending = true;
                break;

            case CardType.Medicine:
                Debug.Log($"{caster.dinoName} uses Medicine card! +1 HP");
                caster.Heal(1);
                break;

            case CardType.Charge:
                // Charge is handled by GameManager for movement
                // Damage is dealt automatically to adjacent enemies
                Debug.Log($"{caster.dinoName} uses Charge card!");
                break;
        }
    }
}