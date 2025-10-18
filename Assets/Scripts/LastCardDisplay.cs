using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LastCardDisplay : MonoBehaviour
{
    public static LastCardDisplay Instance { get; private set; }

    [Header("UI References")]
    public GameObject cardUIPrefab; // Reuse the same prefab!
    public Transform cardContainer; // Where to spawn the card
    public TextMeshProUGUI headerText; // Shows who played it

    [Header("Display Settings")]
    public float displayDuration = 3f; // How long to show the card

    private GameObject currentCardDisplay;
    private float hideTimer = 0f;
    private bool isShowing = false;

    void Awake()
    {
        Instance = this;

        // Hide panel by default
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (isShowing)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                HideCard();
            }
        }
    }

    public void ShowCard(Card card, string playerName)
    {
        if (card == null || cardUIPrefab == null || cardContainer == null)
        {
            Debug.LogWarning("Cannot show card - missing references!");
            return;
        }

        // Destroy old card if exists
        if (currentCardDisplay != null)
        {
            Destroy(currentCardDisplay);
        }

        // Create new card using the prefab
        currentCardDisplay = Instantiate(cardUIPrefab, cardContainer);

        // Set card data
        CardUI cardUI = currentCardDisplay.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetCard(card, null); // Pass null for HandManager since we don't need clicks

            // Disable the button so it's not clickable
            Button button = currentCardDisplay.GetComponentInChildren<Button>();
            if (button != null)
            {
                button.interactable = false;
                Destroy(button); // Remove button functionality
            }
        }

        // Update header text
        if (headerText != null)
        {
            headerText.text = $"{playerName} played:";
        }

        // Show the panel
        gameObject.SetActive(true);
        isShowing = true;
        hideTimer = displayDuration;

        Debug.Log($"Showing last card played: {card.cardName} by {playerName}");
    }

    public void HideCard()
    {
        gameObject.SetActive(false);
        isShowing = false;

        if (currentCardDisplay != null)
        {
            Destroy(currentCardDisplay);
            currentCardDisplay = null;
        }
    }

    public void ShowCardPermanent(Card card, string playerName)
    {
        ShowCard(card, playerName);
        isShowing = false; // Don't auto-hide
    }
}