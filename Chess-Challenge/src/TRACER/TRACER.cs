#define SHOW_INFO
using Chess_Challenge.src.TRACER;
using ChessChallenge.API;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Windows;
using System.Runtime.InteropServices;

public class TRACER : IChessBot
{
    #if SHOW_INFO
        public BotInfo Info()
        {
            return new BotInfo(lastDepth, lastEval, lastMove);
        }
    #endif


    //Constant Variables
    private int MAX_DEPTH = 40; //Max Search depth
    private const int QSEARCH_DEPTH = 8; //QSearch depth
    private const int MATE = 30000; //Score for Mate
    private const int TTABLE_SIZE_MB = 64; //Size of TTable in MB

    //Global Variables
    private Move bestMove; //best Move that will be played
    private Move bestMovePrevIteration; //best Move from the previous Iteration 
    private int currentDepth; //Depth we are currently searching 
    private int timeForTurn; //Time the engine has to make a turn

    //------------------------- #DEBUG -------------------------
    private string lastMove;
    private int lastDepth;
    private int lastEval;   
    private int nodesSearched;
    private int qnodesSearched;
    private int deltaPruned;
    private int alphaBetaPruned;
    private int ttableUsed;
    //------------------------- #DEBUGEND ----------------------


    //Used Classes
    Evaluation eval = new Evaluation();
    MoveOrdering order = new MoveOrdering();
    TranspositionTable ttable = new TranspositionTable(TTABLE_SIZE_MB);

    public Move Think(Board board, Timer timer)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched = 0; //Reset the numbers of nodes Searched
        qnodesSearched = 0; //Reset the numbers of nodes Searched
        deltaPruned = 0; //Reset delta pruned
        alphaBetaPruned = 0; //Reset alphaBetaPruned
        ttableUsed = 0;
        Console.WriteLine("-----------------------------------------------------------------");
        //------------------------- #DEBUGEND ----------------------

        bestMove = Move.NullMove; //Reset bestMove at start of think method
        bestMovePrevIteration = Move.NullMove; //Reset bestMove previous Iteration 

        //Set MaxDepth depending on pieces on the board TODO

        timeForTurn = timer.MillisecondsRemaining / 45; //Calculate time for turn TODO

        int score = 0;

        for (currentDepth = 0; currentDepth <= MAX_DEPTH; currentDepth++)
        {

            score = Search(board, currentDepth, -MATE, MATE, 0);

            //------------------------- #DEBUG -------------------------  
            Console.WriteLine("//////////");
            Console.WriteLine("NoPS/QSP:    {0}/{1}", nodesSearched, qnodesSearched);
            Console.WriteLine("AB-Pruned:   {0}", alphaBetaPruned);
            Console.WriteLine("Del-Pruned:  {0}", deltaPruned);
            Console.WriteLine("TTable-Used: {0}", ttableUsed);
            //------------------------- #DEBUGEND ----------------------
            //Set bestMove from previous Iteration
            bestMovePrevIteration = bestMove;

            //Check if time for turn is up
            if (timer.MillisecondsElapsedThisTurn > timeForTurn)
            {
                Console.WriteLine("times up");
                break;
            }
        }
        //update variables for UI
        lastDepth = currentDepth;
        lastEval = score;
        lastMove = $"{bestMove}";

        return bestMove;
    }

    private int Search(Board board, int depth, int alpha, int beta, int ply)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        // Variables
        int score;
        ulong zobristKey = board.ZobristKey;

        // when root is reached, evaluate the position
        if (depth == 0)
        {
            score = QSearch(board, alpha, beta, ply);
            return score;
        }
        // If in Mate return negative mate score adjusted by ply to encourage earlier checkmates
        if (board.IsInCheckmate())
            return -MATE + ply;
        // If the position is draw then return 0
        if (board.IsDraw())
            return 0;

        // Recursive call of Search function to find the best Move
        Move[] moves = board.GetLegalMoves();
        // Set the previous bestMove for Ordering
        order.SetPrevIterationBestMove(bestMovePrevIteration);
        // Order moves to increase AB-Pruning efficiency
        order.OrderMoves(moves, board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -Search(board, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);

            if (score > alpha)
            {
                alpha = score;
                if (ply == 0)
                {
                    bestMove = move;
                }
            }

            // Alpha-Beta Pruning
            if (alpha >= beta)
            {
                //------------------------- #DEBUG -------------------------
                alphaBetaPruned++;
                //------------------------- #DEBUGEND ----------------------
                return beta;
            }
        }
        return alpha;
    }


    private int QSearch(Board board, int alpha, int beta, int ply)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        qnodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        //Evaluate the position
        int standPat = eval.EvaluatePosition(board) * (board.IsWhiteToMove ? 1 : -1);

        //Check if search depth is reached
        if (ply >= QSEARCH_DEPTH)
            return standPat;

        //Check for beta cutoff
        if (standPat >= beta)
            return beta;

        //Delta-Pruning 
        //chessprogrammin.org/Delta_Pruning
        const int BIG_DELTA = 500; //High value near Queen
        if (standPat < alpha - BIG_DELTA)
        {
            //------------------------- #DEBUG -------------------------
            deltaPruned++;
            //------------------------- #DEBUGEND ----------------------
            return alpha;
        }

        //Update alpha if necessary
        if (alpha < standPat)
            alpha = standPat;  

        //Find all capture moves and order them
        Move[] qSearchMoves = board.GetLegalMoves(true);
        //Set the previous bestMove for Ordering
        order.SetPrevIterationBestMove(bestMovePrevIteration);
        order.OrderMoves(qSearchMoves, board);

        //foreach capture move look further into the position
        foreach (Move move in qSearchMoves)
        {
            board.MakeMove(move);
            int score = -QSearch(board, -beta, -alpha, ply + 1);
            board.UndoMove(move);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }
}