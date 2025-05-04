using UnityEngine;
using System.Collections.Generic;

public class cardInfo : MonoBehaviour
{
    public enum CardType
    {
        rock,
        paper,
        scissors,
    }
    public CardType cardType;

    public List<Material> materials = new List<Material>();

    void Start()
    {
        // int temp = Random.Range(0, 3);

        // //cardType = (CardType)temp;

        // GetComponent<Renderer>().material = materials[(int)cardType];
    }

    // Update is called once per frame
    void Update()
    {

    }
}
