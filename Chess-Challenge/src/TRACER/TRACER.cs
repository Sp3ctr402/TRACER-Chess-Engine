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
    //Constant Variables
    private int MAX_DEPTH = 5; //Max Search depth
    private const int QSEARCH_DEPTH = 8; //QSearch depth
    private const int MATE = 30000; //Score for Mate

    //Global Variables
    private Move bestMove; //best Move that will be played
    private Move bestMovePrevIteration; //best Move from the previous Iteration 
    private int currentDepth; //Depth we are currently searching 
    private int timeForTurn; //Time the engine has to make a turn

    //------------------------- #DEBUG -------------------------
    private int nodesSearched;
    private int qnodesSearched;
    private int deltaPruned;
    private int alphaBetaPruned;
    //------------------------- #DEBUGEND ----------------------


    //Used Classes
    Evaluation eval = new Evaluation();
    MoveOrdering order = new MoveOrdering();

    public Move Think(Board board, Timer timer)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched = 0; //Reset the numbers of nodes Searched
        qnodesSearched = 0; //Reset the numbers of nodes Searched
        deltaPruned = 0; //Reset delta pruned
        alphaBetaPruned = 0; //Reset alphaBetaPruned
        Console.WriteLine("-----------------------------------------------------------------");
        //------------------------- #DEBUGEND ----------------------

        bestMove = Move.NullMove; //Reset bestMove at start of think method
        bestMovePrevIteration = Move.NullMove; //Reset bestMove previous Iteration 

        //Set MaxDepth depending on pieces on the board TODO


        timeForTurn = timer.MillisecondsRemaining / 45; //Calculate time for turn TODO

        for (currentDepth = 0; currentDepth <= MAX_DEPTH; currentDepth++)
        {
            Search(board, currentDepth, -MATE, MATE, 0);

            //------------------------- #DEBUG -------------------------  
            Console.WriteLine("NoPS/QSP: {0}/{2} | AB-Pruned:{5} | DeltaPruned: {3} | Eval: {1} | Depth: {4}"
                , nodesSearched, eval.EvaluatePosition(board), qnodesSearched, deltaPruned, currentDepth, alphaBetaPruned);
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
        //------------------------- #DEBUG -------------------------
        Console.WriteLine("-----------------------------------------------------------------");
        //------------------------- #DEBUGEND ----------------------
        return bestMove;
    }

    private int Search(Board board, int depth, int alpha, int beta, int ply)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        //when root is reached evaluate the position
        //relative score -> if white is winning and its blacks turn Evaluation should return a negative score
        //since my Evaluation returns the score from a watchers perspective we need to multiply evaluation by whos turn it is
        if (depth == 0)
            return QSearch(board, alpha, beta, ply);
        //If in Mate return negative mate score adjusted by ply to encourage earlier checkmates
        if (board.IsInCheckmate())
            return -MATE + ply;
        //If the position is draw then return deadequal
        if (board.IsDraw())
            return 0;

        //Recursive call of Search function to find best Move
        //In all legal moves
        Move[] moves = board.GetLegalMoves();
        //Set the previous bestMove for Ordering
        order.SetPrevIterationBestMove(bestMovePrevIteration);
        //Order moves to increase AB-Puning efficiency
        order.OrderMoves(moves, board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            //decrement depth, increment ply, swap and negate alpha and beta
            //negate the returned score since we switched colors/sides
            int score = -Search(board, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);

            if (score > alpha)
            {
                alpha = score;
                //only set the move at the root node
                if (ply == 0)
                    bestMove = move;
            }
            //AlphaBeta Pruning
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