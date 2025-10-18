using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshPro

public class CardUI : MonoBehaviour
{
    [Header("UI References - TextMeshPro")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public Image cardImage;
    public Button playButton;

    [Header("Card Colors")]
    public Color attackColor = new Color(1f, 0.3f, 0.3f);
    public Color defendColor = new Color(0.3f, 0.5f, 1f);
    public Color medicineColor = new Color(0.3f, 1f, 0.3f);
    public Color chargeColor = new Color(1f, 0.8f, 0.2f);

    private Card card;
    private HandManager handManager;

    public void SetCard(Card newCard, HandManager manager)
    {
        card = newCard;
        handManager = manager;

        if (card != null)
        {
            if (cardNameText != null)
                cardNameText.text = card.cardName;

            if (descriptionText != null)
                descriptionText.text = card.description;

            // Set card color based on type
            Color cardColor = Color.white;
            switch (card.cardType)
            {
                case CardType.Attack:
                    cardColor = attackColor;
                    break;
                case CardType.Defend:
                    cardColor = defendColor;
                    break;
                case CardType.Medicine:
                    cardColor = medicineColor;
                    break;
                case CardType.Charge:
                    cardColor = chargeColor;
                    break;
            }

            if (cardImage != null)
                cardImage.color = cardColor;
        }

        // Setup button
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    void OnPlayButtonClicked()
    {
        if (handManager != null && card != null)
        {
            handManager.OnCardClicked(this, card);
        }
    }

    public Card GetCard()
    {
        return card;
    }
}