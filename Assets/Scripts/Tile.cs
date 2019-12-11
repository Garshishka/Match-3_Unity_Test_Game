using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    //showing the place on a board
    public int x;
    public int y;

    public bool DoneMoving = true; //used to stop stuff until piece is moving

    public void OnClick()
    {   //  Checking if this is the second clicked tile and its adjacent to a highlighted tile
        if (Match3Logic.Instance.CanClick)
        {
            if (x > 0 && Match3Logic.Instance.board[x - 1, y] == Match3Logic.Instance.firstSel)
            {
                SwapTiles("Right");
            }
            else
            {
                if (x < Match3Logic.Instance.Width - 1 && Match3Logic.Instance.board[x + 1, y] == Match3Logic.Instance.firstSel)
                {
                    SwapTiles("Left");
                }
                else
                {
                    if (y > 0 && Match3Logic.Instance.board[x, y - 1] == Match3Logic.Instance.firstSel)
                    {
                        SwapTiles("Up");
                    }
                    else
                    {
                        if (y < Match3Logic.Instance.Height - 1 && Match3Logic.Instance.board[x, y + 1] == Match3Logic.Instance.firstSel)
                        {
                            SwapTiles("Down");
                        }
                        else
                        {
                            NewPos(); //If this is first clicked of not an adjacent to other selected tile
                        }
                    }
                }
            }
        }
    }

    public void SwapTiles(string WhereTo ) //Swapping with adajacent tile
    {
        Match3Logic.Instance.CanClick = false; 
        int SwapToX = x; int SwapToY = y;
        int SwapFromX = x; int SwapFromY = y; //Those can be changed by Change fucntion, so we save them separately
        switch (WhereTo)
        { //we change where to move coordinate
            case "Right":
                SwapToX = x - 1; 
                break;
            case "Left":
                SwapToX = x + 1;
                break;
            case "Up":
                SwapToY = y - 1;
                break;
            case "Down":
                SwapToY = y + 1;
                break;
        }
        Match3Logic.Instance.secondSel = Match3Logic.Instance.board[x, y];

        Match3Logic.Instance.board[SwapToX, SwapToY].GO.GetComponent<Tile>().Change(x, y,true); //Changing the  first gameobject to reflect its new placement
        Match3Logic.Instance.board[SwapToX, SwapToY] = Match3Logic.Instance.secondSel; //Second tile moves to new place
        Match3Logic.Instance.board[SwapToX, SwapToY].GO.GetComponent<Tile>().Change(SwapToX, SwapToY, true); //Changing the second tile gameobject to reflect its new place
        Match3Logic.Instance.board[SwapFromX, SwapFromY] = Match3Logic.Instance.firstSel; //Moving the first tile(that already changed GO) to new place

        HideHl();
        StartCoroutine(WaitingForMove(SwapToX, SwapToY, SwapFromX, SwapFromY)); //now we wait for moving
    }
    IEnumerator WaitingForMove(int SFX, int SFY, int STX, int STY)
    {
        while (DoneMoving == false)
        {
            yield return new WaitForSeconds(0.1f); //Waiting for tiles to stop moving;
        }
        Matching(SFX, SFY, STX, STY);
    }
    void Matching(int SwapFromX, int SwapFromY, int SwapToX, int SwapToY)
    {   //checking for matches from both moved tiles
        bool Check1 = Check3MatchAnBreak(SwapFromX, SwapFromY);
        bool Check2 = Check3MatchAnBreak(SwapToX, SwapToY);
        if (Check1||Check2 )
        { //if at least one of them got something we check for blanks
           StartCoroutine(Match3Logic.Instance.CheckForBlanks());
        }
        else { Match3Logic.Instance.CanClick = true; }
        Match3Logic.Instance.OneMoreMove();
    }
       
   public void Change(int nx, int ny, bool NeedMove) //Changes inside coordinates, name and postition to a new one
    {
        if (NeedMove)
        {  StartCoroutine(MoveTile(nx - x, ny - y));} //for moving tiles when swapping
        else { transform.localPosition = new Vector3(nx * 100, ny * 100); } //for when we just create new tiles
        x = nx; y = ny;
        transform.name = "[" + x + "," + y + "]";
    }

   IEnumerator MoveTile(int dx, int dy)
    {
        DoneMoving = false;
        for (int i = 0; i < 4; i++)
        {
            transform.localPosition += new Vector3(dx*25,dy*25);
            yield return new WaitForSeconds(0.02f);
        }
        DoneMoving = true;
    }

   public bool Check3MatchAnBreak(int fx, int fy)
    {   //Finding all 3 matches around the tile and breaking them
        Node[,] board = Match3Logic.Instance.board;
        int val1 = board[fx, fy].type;

        //Creating lists for matched tiles in both directions 
        List<Node> MatchListH = new List<Node>();
        List<Node> MatchListV = new List<Node>();

        if (HorizontalCheck(MatchListH, fx, fy, board, val1))
        {
            BreakTiles(MatchListH);
        }
        //finding matches,filling up match lists and breaking them
        if (VerticalCheck(MatchListV, fx, fy, board, val1))
        {
            BreakTiles(MatchListV);
        }

        if(MatchListH.Count>2|| MatchListV.Count > 2)
        { //if we had even 1 match
            return true;
        }
        else { return false; }
    }

   void BreakTiles(List<Node> MatchList)
    {   //Breaking tiles in one row
            foreach (Node tile in MatchList) //we destroy all of them if they are still not destroyed
            {
                if (tile.type != 0)
                {
                    tile.type = 0; //we make the type 0 so finding blanks func will know that this node is dead
                tile.GO.GetComponent<Tile>().DestroyThatTile();
                }
            }
        StartCoroutine(Match3Logic.Instance.OneMoreCombo());
    }

   public void DestroyThatTile()
    { //just to be safe we create a function to call coroutine
        StartCoroutine(DestroyThisTile());
    }
   IEnumerator DestroyThisTile()
    {   
        for (int i = 0; i < 5; i++)
        {   //tile will become smaller and then destroy
            transform.localScale -= new Vector3(0.1f, 0.1f);
            yield return new WaitForSeconds(0.02f);
        }
        Match3Logic.Instance.OneMoreDestroyed();
        Destroy(gameObject);
    }

   bool HorizontalCheck(List<Node> MatchList,int wx,int wy, Node[,] board, int val)
    { //checking left and right from the chosen tile
        MatchList.Add(board[wx, wy]); //we add starting tile to the matched list
        int i = wx-1;
        while (i>-1&& board[i, wy].type == val)
        { //while tile types matches we go left
            MatchList.Add(board[i, wy]);
            i--; //and add every tile to a list
        }
        i = wx+1;
        while (i < Match3Logic.Instance.Width && board[i, wy].type == val)
        {//same but to the right
            MatchList.Add(board[i, wy]);
            i++;
        }
        if (MatchList.Count > 2) { return true; } //if we have 3 or more matched tiles - sucess
        else { return false; }
    }

   bool VerticalCheck(List<Node> MatchList, int wx, int wy, Node[,] board, int val)
    { //checking up and down from the chosen tile
        MatchList.Add(board[wx, wy]);  //we add starting tile to the matched list
        int i = wy - 1;
        while (i > -1 && board[wx, i].type == val)
        {  //while tile types matches we go down
            MatchList.Add(board[wx, i]);
            i--; //and add every tile to a list
        }
        i = wy + 1;
        while (i < Match3Logic.Instance.Height && board[wx, i].type == val)
        { //same but up
            MatchList.Add(board[wx, i]);
            i++;
        }
        if (MatchList.Count > 2) {return true; } //if we have 3 or more matched tiles - sucess
        else { return false; }
    }

   void NewPos() //Showing the selected tile
    {
        Match3Logic.Instance.firstSel = Match3Logic.Instance.board[x, y];
        var HL = GameObject.Find("HLighter").transform;
        HL.position = transform.position;
    }

   void HideHl() //Clearing the clicked tiles state and hiding highlighter
    {
        Match3Logic.Instance.firstSel = null;
        Match3Logic.Instance.secondSel = null;
        var HL = GameObject.Find("HLighter").transform;
        HL.position = new Vector3(-1000,-1000);
    }      
}
