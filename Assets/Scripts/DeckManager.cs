using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("Deck Composition")]
    public int attackCardCount = 10;
    public int defendCardCount = 10;
    public int medicineCardCount = 10;
    public int chargeCardCount = 10;

    [Header("Equipment Card Counts")]
    public int mekaLegCount = 3;
    public int protectionGemCount = 3;

    private List<Card> deck = new List<Card>();
    private List<Card> discardPile = new List<Card>();

    void Awake()
    {
        Instance = this;
        InitializeDeck();
    }

    void InitializeDeck()
    {
        deck.Clear();

        // Add Attack cards
        for (int i = 0; i < attackCardCount; i++)
        {
            deck.Add(new Card(CardType.Attack));
        }

        // Add Defend cards
        for (int i = 0; i < defendCardCount; i++)
        {
            deck.Add(new Card(CardType.Defend));
        }

        // Add Medicine cards
        for (int i = 0; i < medicineCardCount; i++)
        {
            deck.Add(new Card(CardType.Medicine));
        }

        // Add Charge cards
        for (int i = 0; i < chargeCardCount; i++)
        {
            deck.Add(new Card(CardType.Charge));
        }

        // Add Mecha Leg equipment
        for (int i = 0; i < mekaLegCount; i++)
        {
            deck.Add(new Card(CardType.Equipment, EquipmentType.MekaLeg));
        }

        // Add Protection Gem equipment
        for (int i = 0; i < protectionGemCount; i++)
        {
            deck.Add(new Card(CardType.Equipment, EquipmentType.ProtectionGem));
        }

        ShuffleDeck();

        Debug.Log($"Deck initialized with {deck.Count} cards");
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        Debug.Log("Deck shuffled");
    }

    public Card DrawCard()
    {
        // If deck is empty, shuffle discard pile back into deck
        if (deck.Count == 0)
        {
            if (discardPile.Count > 0)
            {
                Debug.Log("Deck empty! Shuffling discard pile into deck");
                deck.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck();
            }
            else
            {
                Debug.LogWarning("No cards left to draw!");
                return null;
            }
        }

        Card drawnCard = deck[0];
        deck.RemoveAt(0);

        Debug.Log($"Drew card: {drawnCard.cardName}");
        return drawnCard;
    }

    public void DiscardCard(Card card)
    {
        if (card != null)
        {
            discardPile.Add(card);
            Debug.Log($"Discarded card: {card.cardName}");
        }
    }

    public int GetDeckCount()
    {
        return deck.Count;
    }

    public int GetDiscardCount()
    {
        return discardPile.Count;
    }
}