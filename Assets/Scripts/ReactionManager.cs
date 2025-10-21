using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ReactionManager : MonoBehaviour
{
    public static ReactionManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject reactionPanel; // Panel that appears when attacked
    public Transform reactionCardContainer; // Where to show defensive card options
    public TMPro.TextMeshProUGUI headerText;
    public TMPro.TextMeshProUGUI timerText;
    public TMPro.TextMeshProUGUI instructionText;
    public UnityEngine.UI.Button takeDamageButton;

    [Header("Settings")]
    public float reactionTimeLimit = 10f; // How long player has to respond

    private bool waitingForReaction = false;
    private DinoUnit attacker;
    private DinoUnit defender;
    private int incomingDamage;
    private float reactionTimer;
    private List<GameObject> reactionCardUIs = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Hide reaction panel by default
        if (reactionPanel != null)
        {
            reactionPanel.SetActive(false);
        }

        // Setup Take Damage button
        if (takeDamageButton != null)
        {
            takeDamageButton.onClick.AddListener(OnTakeDamageClicked);
        }
    }

    void Update()
    {
        if (waitingForReaction)
        {
            reactionTimer -= Time.deltaTime;

            // Update timer display
            if (timerText != null)
            {
                timerText.text = Mathf.Ceil(reactionTimer).ToString();
            }

            if (reactionTimer <= 0f)
            {
                // Time's up - take the damage
                Debug.Log("Reaction time expired! Taking damage...");
                ResolveAttack(false);
            }
        }
    }

    // Called when an attack is initiated
    public void InitiateAttack(DinoUnit attackingUnit, DinoUnit defendingUnit, int damage)
    {
        attacker = attackingUnit;
        defender = defendingUnit;
        incomingDamage = damage;

        Debug.Log($"{attacker.dinoName} attacks {defender.dinoName} for {damage} damage!");

        // Check if defender is player or AI
        if (defender.team == Team.Player)
        {
            // Check for Protection Gem - 33% chance to auto-defend
            bool hasProtectionGem = EquipmentManager.Instance != null &&
                                    EquipmentManager.Instance.HasEquipment(defender, EquipmentType.ProtectionGem);

            Debug.Log($"Has Protection Gem: {hasProtectionGem}");

            if (hasProtectionGem)
            {
                float roll = Random.value;
                Debug.Log($"Protection Gem roll: {roll} (needs < 0.33 to activate)");

                if (roll < 0.33f)
                {
                    // Protection Gem triggered!
                    Debug.Log($" {defender.dinoName}'s Protection Gem activated! Auto-defending!");
                    defender.isDefending = true;

                    // Show it in LastCardDisplay
                    if (LastCardDisplay.Instance != null)
                    {
                        // Create a temporary defend card to show
                        Card defendCard = new Card(CardType.Defend);
                        LastCardDisplay.Instance.ShowCard(defendCard, $"{defender.dinoName} (Protection Gem)");
                    }

                    ResolveAttack(true);
                    return; // STOP HERE - don't show reaction panel!
                }
            }

            // Player is being attacked - show reaction UI
            ShowReactionUI();
        }
        else
        {
            // Check for AI Protection Gem
            bool hasProtectionGem = EquipmentManager.Instance != null &&
                                    EquipmentManager.Instance.HasEquipment(defender, EquipmentType.ProtectionGem);

            if (hasProtectionGem && Random.value < 0.33f)
            {
                // Protection Gem triggered for AI!
                Debug.Log($"✨ AI {defender.dinoName}'s Protection Gem activated! Auto-defending!");
                defender.isDefending = true;

                // Show it in LastCardDisplay
                if (LastCardDisplay.Instance != null)
                {
                    Card defendCard = new Card(CardType.Defend);
                    LastCardDisplay.Instance.ShowCard(defendCard, $"Enemy {defender.dinoName} (Protection Gem)");
                }

                ResolveAttack(true);
                return;
            }

            // AI is being attacked - AI decides reaction
            StartCoroutine(AIReactionCoroutine());
        }
    }

    void ShowReactionUI()
    {
        waitingForReaction = true;
        reactionTimer = reactionTimeLimit;

        if (reactionPanel != null)
        {
            reactionPanel.SetActive(true);
        }

        // Update UI texts
        if (headerText != null)
        {
            headerText.text = $"{attacker.dinoName} is attacking!";
        }

        if (instructionText != null)
        {
            instructionText.text = $"Play a Defend card or take {incomingDamage} damage!";
        }

        // Get player's hand and show only Defend cards
        List<Card> playerHand = HandManager.Instance != null ? HandManager.Instance.GetPlayerHand() : new List<Card>();
        List<Card> defendCards = playerHand.Where(c => c.cardType == CardType.Defend).ToList();

        Debug.Log($"Player has {defendCards.Count} Defend cards available");

        // Clear previous reaction cards
        ClearReactionCards();

        // Create UI for each Defend card
        foreach (Card defendCard in defendCards)
        {
            CreateReactionCardUI(defendCard);
        }

        // If no defend cards, show message
        if (defendCards.Count == 0 && instructionText != null)
        {
            instructionText.text = $"No Defend cards! You will take {incomingDamage} damage!";
        }
    }

    void CreateReactionCardUI(Card card)
    {
        if (HandManager.Instance == null || HandManager.Instance.cardUIPrefab == null)
        {
            Debug.LogError("Cannot create reaction card UI!");
            return;
        }

        GameObject cardObj = Instantiate(HandManager.Instance.cardUIPrefab, reactionCardContainer);
        CardUI cardUI = cardObj.GetComponent<CardUI>();

        if (cardUI != null)
        {
            cardUI.SetCard(card, null); // Don't pass HandManager

            // Replace the button functionality to use defend
            UnityEngine.UI.Button button = cardUI.playButton;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnDefendCardClicked(card));
            }
        }

        reactionCardUIs.Add(cardObj);
    }

    void ClearReactionCards()
    {
        foreach (GameObject cardObj in reactionCardUIs)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }
        reactionCardUIs.Clear();
    }

    // Called when player clicks a Defend card
    public void OnDefendCardClicked(Card defendCard)
    {
        Debug.Log("Player uses Defend card to block attack!");

        // Play the defend card
        defendCard.PlayCard(defender, null, null);

        // Show in LastCardDisplay
        if (LastCardDisplay.Instance != null)
        {
            LastCardDisplay.Instance.ShowCard(defendCard, defender.dinoName);
        }

        // Remove card from player's hand
        HandManager.Instance.RemoveCardFromHand(defendCard);

        // Resolve attack (blocked)
        ResolveAttack(true);
    }

    // Player chooses to take damage without defending
    public void OnTakeDamageClicked()
    {
        Debug.Log("🔴 TAKE DAMAGE BUTTON CLICKED!");
        ResolveAttack(false);
    }

    IEnumerator AIReactionCoroutine()
    {
        yield return new WaitForSeconds(1f); // AI "thinks" for 1 second

        // Check if AI has Defend cards
        List<Card> aiHand = HandManager.Instance != null ? HandManager.Instance.GetAIHand() : new List<Card>();
        Card defendCard = aiHand.FirstOrDefault(c => c.cardType == CardType.Defend);

        // AI uses Defend if:
        // 1. It has a Defend card
        // 2. The attack would deal significant damage (more than 15)
        // 3. Random 70% chance
        bool shouldDefend = defendCard != null &&
                           incomingDamage > 15 &&
                           Random.value < 0.7f;

        if (shouldDefend)
        {
            Debug.Log($"AI {defender.dinoName} uses Defend card!");

            // Play defend card
            defendCard.PlayCard(defender, null, null);

            // Show in LastCardDisplay
            if (LastCardDisplay.Instance != null)
            {
                LastCardDisplay.Instance.ShowCard(defendCard, $"Enemy {defender.dinoName}");
            }

            // Remove from AI hand
            HandManager.Instance.AIPlayCard(defendCard, defender, null, null);

            ResolveAttack(true);
        }
        else
        {
            Debug.Log($"AI {defender.dinoName} takes the hit!");
            ResolveAttack(false);
        }
    }

    void ResolveAttack(bool wasBlocked)
    {
        waitingForReaction = false;

        // Hide reaction UI
        if (reactionPanel != null)
        {
            reactionPanel.SetActive(false);
        }

        ClearReactionCards();

        if (wasBlocked)
        {
            Debug.Log($"{defender.dinoName} blocked the attack!");
            // Damage already prevented by isDefending flag in DinoUnit
        }
        else
        {
            Debug.Log($"{defender.dinoName} takes {incomingDamage} damage!");
            defender.TakeDamage(incomingDamage);
        }

        // Check if attacker has Mecha Leg equipment
        bool hasMechaLeg = EquipmentManager.Instance != null &&
                           EquipmentManager.Instance.HasEquipment(attacker, EquipmentType.MekaLeg);

        // Only mark as attacked if NO Mecha Leg
        if (!hasMechaLeg)
        {
            attacker.hasAttacked = true;
            Debug.Log($"{attacker.dinoName} has attacked - can't attack again this turn");
        }
        else
        {
            Debug.Log($"{attacker.dinoName} has Mecha Leg - can attack again!");
        }

        // Notify GameManager that reaction is complete
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnReactionComplete();
        }
    }

    // These methods have been removed - we use HandManager.Instance directly

    public bool IsWaitingForReaction()
    {
        return waitingForReaction;
    }
}