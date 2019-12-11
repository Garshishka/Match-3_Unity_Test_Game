using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Match3Logic : MonoBehaviour
{
    private static Match3Logic _instance;

    public static Match3Logic Instance { get { return _instance; } }

    [SerializeField]
    Text MovesNumber;
    int MovesN;

    [SerializeField]
    Text DestroyedNumber;
    int DesN;

    [SerializeField]
    Text ComboNumber;
    int ComboN;

    public int Height = 16; //The dimension of the board
    public int Width = 10;

    public GameObject Highlighter; //Little piece that will be on the first selected tile

    public GameObject[] tilePieces; //Prefab list for tile types

    public Transform gameBoard;

    public Node firstSel;       //these will be used for selecting which tiles to swap
    public Node secondSel;
    
    public Node[,] board;        //Main array where information about our board will be stored

    public bool CanClick = true; //This bool is for stopping interaction on the board when something happening

    int FirstBlankX; int FirstBlankY;//This will be used for checking the board after filling up the blanks
    int LastBlank;

    private void Awake()
    {   //Singleton config
        if(_instance == null)
        {
            _instance = this;
        }
        else if (_instance == this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
     /*   if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _instance = this;
        }*/
    }

    private void Start()
    {
        FirstBlankX = 0;
        FirstBlankY = 0;
        LastBlank = 0;
        MovesN = 0;
        MovesNumber.text = MovesN.ToString();
        DesN = 0;
        DestroyedNumber.text = DesN.ToString();
        ComboN = 0;
        StartGame();
    }

    void StartGame()
    {   //Filling the board at the beginning
        board = new Node[Width, Height];        
        for (int y =0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int value = Random.Range(1, 6);
                value = CheckPlacing(value, x, y); 
                //after checking we create a new tile with the type
               GameObject NewTile = Instantiate(tilePieces[value - 1],gameBoard);

               NewTile.GetComponent<Tile>().Change(x, y, false); //and change its internal properties

               board[x, y] = new Node(value,NewTile); //then we place it in our table
            }
        }
    }

    int CheckPlacing(int val, int x, int y) 
    {   //Checking already placed tiles on the left and down so as not to create 3 matches on the creation
            if(x > 1 && board[x-1,y].type == val)
            {
                if (board[x - 2, y].type == val)
                {                                   //if two of the same tile types repeat
                    val = Random.Range(1, 6);       //we change the type of created tile to some other
                    val = CheckPlacing(val, x, y);  //and check again
                }
            }        
            if (y > 1 && board[x, y-1].type == val)
            {
                if (board[x, y-2].type == val)
                {
                    val = Random.Range(1, 6);
                    val = CheckPlacing(val, x, y);
                }
            }
        return val;
    }


    public IEnumerator CheckForBlanks()
    {   //Getting blank places on board into its own list
        bool FoundBlank = false;
        for (int y = Height-1; y > -1; y--) //going up to down
        {
            for (int x = 0; x < Width; x++)
            { //and left to right
                if (board[x, y].type == 0)
                {   //every tile up above blanks moves down
                    FoundBlank = true;
                    if (y != Height - 1)
                    {
                        board[x, y].type = board[x, y + 1].type; //we make upper node into this node
                        board[x, y].GO = board[x, y + 1].GO; 

                        board[x, y + 1].type = 0; //we make the upper node blank
                        board[x, y + 1].GO = null;

                        board[x, y].GO.GetComponent<Tile>().Change(x, y, true); //we make internal changes in this node and move it
                    }
                    else
                    { //if this is the last row, we make new tiles
                        int value = Random.Range(1, 6);
                        value = CheckPlacing(value, x, y);
                        //using the same idea as when creating a new board
                        GameObject NewTile = Instantiate(tilePieces[value - 1], gameBoard);
                        // we place a new piece one row higher than height 
                        NewTile.transform.localPosition = new Vector3(x * 100, (y + 1) * 100);
                        NewTile.GetComponent<Tile>().x = x;
                        NewTile.GetComponent<Tile>().y = y + 1;
                        NewTile.GetComponent<Tile>().Change(x, y, true); //so it will move down from up above
                        while (NewTile.GetComponent<Tile>().DoneMoving == false)
                        {
                            yield return new WaitForSeconds(0.1f);
                        }
                        board[x, y] = new Node(value, NewTile); //placing the new tile in the board
                    }
                    if (x < FirstBlankX) { FirstBlankX = x; } //finding the left-most and lowest blank the board
                    if (y < FirstBlankX) { FirstBlankY = y; } 
                    if (x > LastBlank) { LastBlank = x; }     //finding the x of right-most blank
                }
            }
        }               

        if (FoundBlank)
        { //if we had blanks we check again for new ones
         StartCoroutine( CheckForBlanks());
        }
        else
        { //if not we check every tile in the affected area for matches
            for (int y = FirstBlankY; y < Height - 1; y++)
            {
                for (int x = FirstBlankX; x < LastBlank; x++)
                {
                    if(board[x, y].GO.GetComponent<Tile>().Check3MatchAnBreak(x, y))
                    { //if we had matches - we have blanks
                        FoundBlank = true;
                    }                    
                }
            }
            if (FoundBlank) //if we got matches so we check again for blanks
            {
                StartCoroutine(CheckForBlanks());
            }
            else { CanClick =true; ComboN = 0; ComboNumber.text = " "; } //if not we can swap again
        }
    }

    public void OneMoreMove()
    { //Changes the number of moves
        MovesN++;
        MovesNumber.text = MovesN.ToString();
    }
    public void OneMoreDestroyed()
    { //Changes the number of desroyed tiles
        DesN++;
        DestroyedNumber.text = DesN.ToString();
    }

    public IEnumerator OneMoreCombo()
    { //Showes COMBO text and number
        ComboNumber.transform.localScale = new Vector3(0.5f, 0.5f);
        ComboN++;
        if (ComboN != 1)
        { //it's little janky but I like how it looks
            ComboNumber.text = "COMBO x"+ ComboN;
            ComboNumber.transform.localScale += new Vector3(1f, 1f);
            for (int i = 0; i < 2; i++)
            {
                ComboNumber.transform.localScale += new Vector3(0.1f, 0.1f);
                yield return new WaitForSeconds(0.01f);
            }
        }
    }
}

[System.Serializable]
public class Node //This clas governs tiles in the logic grid
{
    public int type;        //This have the type of a tile
    public GameObject GO;   //And this is a container for tile gameobject

    public Node(int i, GameObject T)
    {
        type = i; //1 - circle, 2 - triangle, 3 - diamond, 4 - square, 5 - hex, 6 - star
        GO = T;
    }
}

