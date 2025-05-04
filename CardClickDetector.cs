using UnityEngine;
using Unity.Netcode;
public class CardClickDetector : NetworkBehaviour
{
    private CardManager cardManager;

    private void Start()
    {
        cardManager = GameObject.Find("CardManager").GetComponent<CardManager>();

    }

    void Update()
    {

    }
    // When player card is clicked, we want to spawn clicked card in middle of the board
    private void OnMouseDown()
    {
        Debug.Log($"{gameObject.name} was clicked!");
        var cardData = gameObject.GetComponent<CardData>();
        cardManager.SelectCard(cardData);
    }


}
