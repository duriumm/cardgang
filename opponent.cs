using UnityEngine;

public class opponent : MonoBehaviour
{
    public GameObject board_go;

    board boardScript;

    void Start()
    {
        boardScript = board_go.GetComponent<board>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            boardScript.addEnemyCard(Random.Range(0,3), transform.position);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            boardScript.deleteEnemyCard(boardScript.enemy_cards.Count - 1);
        }
    }
}
