using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject cardUIPrefab;
    public Transform handContainer;

    [Header("Settings")]
    public int maxHandSize = 10;

    private List<Card> playerHand = new List<Card>();
    private List<CardUI> cardUIList = new List<CardUI>();
    private Card selectedCard;
    private CardUI selectedCardUI;
    private bool isInitialized = false;

    // AI hand (not shown in UI)
    private List<Card> aiHand = new List<Card>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Wait for DeckManager to initialize
        Invoke("Initialize", 0.5f);
    }

    void Initialize()
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager not found! Make sure DeckManager exists in scene.");
            Invoke("Initialize", 0.2f);
            return;
        }

        if (cardUIPrefab == null)
        {
            Debug.LogError("Card UI Prefab is not assigned!");
            return;
        }

        if (handContainer == null)
        {
            Debug.LogError("Hand Container is not assigned!");
            return;
        }

        isInitialized = true;
        Debug.Log("HandManager initialized successfully!");

        // Draw initial 4 cards for player
        DrawCards(4, false);

        // Draw initial 4 cards for AI
        DrawCards(4, true);
    }

    public void DrawCards(int count, bool isAI = false)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("HandManager not initialized yet!");
            return;
        }

        List<Card> targetHand = isAI ? aiHand : playerHand;

        for (int i = 0; i < count; i++)
        {
            if (targetHand.Count >= maxHandSize)
            {
                Debug.Log($"{(isAI ? "AI" : "Player")} hand is full!");
                break;
            }

            Card drawnCard = DeckManager.Instance.DrawCard();
            if (drawnCard != null)
            {
                targetHand.Add(drawnCard);

                // Only create UI for player cards
                if (!isAI)
                {
                    CreateCardUI(drawnCard);
                }
                else
                {
                    Debug.Log($"AI drew: {drawnCard.cardName}");
                }
            }
        }

        Debug.Log($"{(isAI ? "AI" : "Player")} hand now has {targetHand.Count} cards");
    }

    void CreateCardUI(Card card)
    {
        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("Card UI Prefab or Hand Container not assigned!");
            return;
        }

        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);
        CardUI cardUI = cardObj.GetComponent<CardUI>();

        if (cardUI != null)
        {
            cardUI.SetCard(card, this);
            cardUIList.Add(cardUI);
            Debug.Log($"Created UI for card: {card.cardName}");
        }
        else
        {
            Debug.LogError("CardUI component not found on prefab!");
        }
    }

    public void OnCardClicked(CardUI cardUI, Card card)
    {
        Debug.Log($"Card clicked: {card.cardName}");

        if (GameManager.Instance.currentState != GameState.PlayerTurn)
        {
            Debug.Log("Not your turn!");
            return;
        }

        if (GameManager.Instance.selectedUnit == null)
        {
            Debug.Log("Select a unit first!");
            return;
        }

        DinoUnit selectedUnit = GameManager.Instance.selectedUnit;

        bool hasMekaLeg = EquipmentManager.Instance != null &&
                   EquipmentManager.Instance.HasEquipment(selectedUnit, EquipmentType.MekaLeg);

        // Only check attack limit for Attack cards WITHOUT Meka Leg
        if (card.cardType == CardType.Attack && selectedUnit.hasAttacked && !hasMekaLeg)
        {
            Debug.Log("Already attacked this turn! Only ONE attack per turn.");
            return;
        }

        selectedCard = card;
        selectedCardUI = cardUI;

        Debug.Log($"Selected card: {card.cardName}");

        // Handle different card types
        switch (card.cardType)
        {
            case CardType.Attack:
                GameManager.Instance.SetCardMode(CardType.Attack);
                Debug.Log(">>> Click an enemy to attack! <<<");
                break;

            case CardType.Defend:
                Debug.Log(">>> Playing Defend card (no targeting needed) <<<");
                PlayCard(selectedUnit, null, null);
                break;

            case CardType.Medicine:
                Debug.Log(">>> Playing Medicine card (no targeting needed) <<<");
                PlayCard(selectedUnit, null, null);
                break;

            case CardType.Charge:
                GameManager.Instance.SetCardMode(CardType.Charge);
                GameManager.Instance.ShowChargeRange(selectedUnit);
                Debug.Log(">>> Click a tile next to an enemy to charge! <<<");
                break;

            case CardType.Equipment:
                Debug.Log(">>> Equipping card! <<<");
                PlayCard(selectedUnit, null, null);
                break;
        }
    }

    public void PlayCard(DinoUnit caster, DinoUnit target, Tile targetTile)
    {
        if (selectedCard == null)
        {
            Debug.LogWarning("No card selected!");
            return;
        }

        Debug.Log($"Playing card: {selectedCard.cardName}");

        // Play the card effect
        selectedCard.PlayCard(caster, target, targetTile);

        // *** ADD THIS: Show the card in LastCardDisplay ***
        if (LastCardDisplay.Instance != null)
        {
            LastCardDisplay.Instance.ShowCard(selectedCard, caster.dinoName);
        }

        // Remove card from hand
        playerHand.Remove(selectedCard);

        // Remove card UI
        if (selectedCardUI != null)
        {
            cardUIList.Remove(selectedCardUI);
            Destroy(selectedCardUI.gameObject);
        }

        // Discard card
        DeckManager.Instance.DiscardCard(selectedCard);

        Debug.Log($"Card '{selectedCard.cardName}' played! Hand now has {playerHand.Count} cards.");

        // Clear card selection
        selectedCard = null;
        selectedCardUI = null;

        // Reset card mode
        GameManager.Instance.ClearCardMode();
    }

    // AI plays a card
    public void AIPlayCard(Card card, DinoUnit caster, DinoUnit target, Tile targetTile)
    {
        if (card == null || !aiHand.Contains(card))
        {
            Debug.LogWarning("AI tried to play invalid card!");
            return;
        }

        Debug.Log($"AI plays: {card.cardName}");

        // Play the card effect
        card.PlayCard(caster, target, targetTile);

        // *** ADD THIS: Show the card in LastCardDisplay ***
        if (LastCardDisplay.Instance != null)
        {
            LastCardDisplay.Instance.ShowCard(card, $"Enemy {caster.dinoName}");
        }

        // Remove from AI hand
        aiHand.Remove(card);

        // Discard card
        DeckManager.Instance.DiscardCard(card);

        Debug.Log($"AI hand now has {aiHand.Count} cards");
    }

    public List<Card> GetAIHand()
    {
        return new List<Card>(aiHand);
    }

    public void ClearHand()
    {
        foreach (CardUI cardUI in cardUIList)
        {
            if (cardUI != null)
                Destroy(cardUI.gameObject);
        }

        cardUIList.Clear();
        playerHand.Clear();
    }

    public Card GetSelectedCard()
    {
        return selectedCard;
    }

    // ADD THESE TWO NEW METHODS HERE:
    public List<Card> GetPlayerHand()
    {
        return new List<Card>(playerHand);
    }

    public void RemoveCardFromHand(Card card)
    {
        Debug.Log($"🗑️ Attempting to remove {card.cardName} from hand");

        if (playerHand.Contains(card))
        {
            playerHand.Remove(card);

            CardUI cardUIToRemove = cardUIList.FirstOrDefault(ui => ui.GetCard() == card);
            if (cardUIToRemove != null)
            {
                cardUIList.Remove(cardUIToRemove);
                Destroy(cardUIToRemove.gameObject);
            }

            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.DiscardCard(card);
            }

            Debug.Log($"✓ Removed {card.cardName} from hand. Hand now has {playerHand.Count} cards");
        }
        else
        {
            Debug.LogWarning($"⚠️ Card {card.cardName} not found in hand!");
        }
    }
}