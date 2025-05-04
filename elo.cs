using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class elo : MonoBehaviour
{

    Vector2 temp;


    List<Vector2> players = new List<Vector2>();
    int nrPlayers = 10;

    public TMP_Text board;

    private void Start()
    {
        for(int i = 0; i < nrPlayers; i++)
        {
            players.Add(new Vector2(i, 1400));
        }

        writeBoard();
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            for (int i = 0; i < nrPlayers; i++)
                play();
        }
        if (Input.GetMouseButtonDown(1))
        {
            matchMaking();
        }
    }

    void matchMaking()
    {
        for (int i = 0; i < nrPlayers; i++)
        {
            float tempDif = 9000;
            int bestMatch = 0;
            for (int x = 0; x < nrPlayers; x++)
            {
                if(x != i)
                {
                    //Debug.Log(x + " - " + i);
                    if (tempDif > Mathf.Abs(players[i].y - players[x].y))
                    {
                        tempDif = Mathf.Abs(players[i].y - players[x].y);
                        bestMatch = x;
                    }
                }
            }
            playTo(i, bestMatch);
        }
    }

    void playTo(int player, int other)
    {
        Debug.Log("Player: " + player + " vs player: " + other + " elo: " + players[player].y + " - " + players[other].y);
    }

    void play()
    {
        int player1 = Random.Range(0, nrPlayers);
        int rNr = 0;
        int player2 = -1;
        while (player2 == -1)
        {
            rNr = Random.Range(0, nrPlayers);
            if (rNr != player1)
                player2 = rNr;
        }


        bool player1Won = false;

        if (Random.Range(0, player1) > Random.Range(0, player2))
            player1Won = true;


        temp = UpdatePlayerRankings(players[player1].y, players[player2].y, 10, player1Won);

        players[player1] = new Vector2(player1, temp.x);
        players[player2] = new Vector2(player2, temp.y);

        writeBoard();
    }

    void writeBoard()
    {
        string tempText = "";
        for (int i = 0; i < nrPlayers; i++)
        {
            tempText += "Player: " + players[i].x + " : " + players[i].y + "\n";
        }
        board.text = tempText;
    }

    public static float GetProbabilityWinning(float ratingPlayer1, float ratingPlayer2)
    {
        return 1f / (1f + Mathf.Pow(10f, (ratingPlayer2 - ratingPlayer1) / 400f));
    }

    public static Vector2 UpdatePlayerRankings(float ratingPlayer1, float ratingPlayer2, float multiplier, bool isPlayer1Winner)
    {

        float probabilityWinPlayer1 = GetProbabilityWinning(ratingPlayer1, ratingPlayer2);

        if (isPlayer1Winner)
        {
            ratingPlayer1 += multiplier * (1 - probabilityWinPlayer1);
            ratingPlayer2 += multiplier * (probabilityWinPlayer1 - 1);

        }
        else
        {
            ratingPlayer1 += multiplier * (-probabilityWinPlayer1);
            ratingPlayer2 += multiplier * probabilityWinPlayer1;
        }

        return new Vector2(ratingPlayer1, ratingPlayer2);
    }
}
