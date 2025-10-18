using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("Deck Settings")]
    public int cardsPerType = 10; // 10 of each card type

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

        // Add cards of each type
        for (int i = 0; i < cardsPerType; i++)
        {
            deck.Add(new Card(CardType.Attack));
            deck.Add(new Card(CardType.Defend));
            deck.Add(new Card(CardType.Medicine));
            deck.Add(new Card(CardType.Charge));
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