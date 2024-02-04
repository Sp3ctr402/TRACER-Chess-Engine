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
            return new BotInfo(lastDepth, lastEval, lastMove, nodesSearched);
        }
    #endif


    //Constant Variables
    //Max Search depth
    private int MAX_DEPTH = 40; 
    //Score for Mate
    private const int MATE = 30000; 
    //Size of TTable in MB
    private const int TTABLE_SIZE_MB = 64; 

    //Global Variables
    //best Move that will be played
    private Move bestMove; 
    //best Move from the previous Iteration 
    private Move bestMovePrevIteration; 
    //Depth we are currently searching 
    private int currentDepth; 
    //Time the engine has to make a turn
    private int timeForTurn; 



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
    Evaluation eval;
    TranspositionTable ttable;
    MoveOrdering order;

    public Move Think(Board board, Timer timer)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched = 0; //Reset the numbers of nodes Searched
        qnodesSearched = 0; //Reset the numbers of nodes Searched
        deltaPruned = 0; //Reset delta pruned
        alphaBetaPruned = 0; //Reset alphaBetaPruned
        ttableUsed = 0; //Reset ttableUsed
        Console.WriteLine("-----------------------------------------------------------------");
        //------------------------- #DEBUGEND ----------------------

        //Fill used classes
        eval = new Evaluation();
        ttable = new TranspositionTable(board, TTABLE_SIZE_MB);
        order = new MoveOrdering();

        //Reset bestMove at start of think method
        bestMove = Move.NullMove; 
        //Reset bestMove previous Iteration 
        bestMovePrevIteration = Move.NullMove; 


        timeForTurn = timer.MillisecondsRemaining / 45; //Make a function to determine if the Search time has been reached TODO

        int score = 0;

        for (currentDepth = 0; currentDepth <= MAX_DEPTH; currentDepth++)
        {

            score = Search(board, currentDepth, -MATE, MATE, 0);

            //------------------------- #DEBUG -------------------------  
            Console.WriteLine("//////////");
            Console.WriteLine("NoPS/QSP:    {0}/{1}", nodesSearched, qnodesSearched);
            Console.WriteLine("AB-Pruned:   {0}", alphaBetaPruned);
            Console.WriteLine("Del-Pruned:  {0}", deltaPruned);
            Console.WriteLine("TTable-Used: {0}\n", ttableUsed);
            //------------------------- #DEBUGEND ----------------------
            //Set bestMove from previous Iteration
            bestMovePrevIteration = bestMove;

            //Check if time for turn is up
            if (timer.MillisecondsElapsedThisTurn > timeForTurn)
            {
                Console.WriteLine("times up");
                break;
            }

            //update variables for UI
            lastDepth = currentDepth;
            lastEval = score;
            lastMove = $"{bestMove}";
        }

        return bestMove;
    }

    private int Search(Board board, int depth, int alpha, int beta, int ply)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        // Variables
        int score;

        // If in Mate return negative mate score adjusted by ply to encourage earlier checkmates
        if (board.IsInCheckmate())
            return -MATE + ply;
        // If the position is draw then return 0
        if (board.IsDraw())
            return 0;

        //try to lookup current position in the transposition table.
        //if the current position has already been searched to at least an equal depth
        //to the search we're doing now, we can just use the recorded evaluation
        int ttVal = ttable.LookupEvaluation(depth, ply, alpha, beta);
        if(ttVal != TranspositionTable.LOOKUPFAILED)
        {
            //------------------------- #DEBUG -------------------------
            ttableUsed++;
            //------------------------- #DEBUGEND ---------------------- 
            if(ply == 0)
            {   
                bestMove = ttable.TryGetStoredMove();
                score = ttable.entries[ttable.Index].value;
            }
            return ttVal;
        }

        // when root is reached, evaluate the position
        if (depth == 0)
        {
            score = QSearch(board, alpha, beta);
            return score;
        }

        // Recursive call of Search function to find the best Move
        Move[] moves = board.GetLegalMoves();
        // Set the previous bestMove for Ordering
        order.SetPrevIterationBestMove(bestMovePrevIteration);
        // Order moves to increase AB-Pruning efficiency
        order.OrderMoves(moves, board);

        //evaluation bound stored in ttable
        int evaluationBound = TranspositionTable.UPPERBOUND;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -Search(board, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);

            // Move was *too* good, opponent will choose a different move earlier on to avoid this position.
            // Alpha-Beta Pruning
            if (score >= beta)
            {
                //------------------------- #DEBUG -------------------------
                alphaBetaPruned++;
                //------------------------- #DEBUGEND ----------------------

                //Store evaluation in transposition Table
                ttable.StoreEvaluation(depth, ply, beta, TranspositionTable.LOWERBOUND, move);

                return beta;
            }

            //new bestMove was found
            if (score > alpha)
            {
                //set evaluationBound to Exact
                evaluationBound = TranspositionTable.EXACT;

                alpha = score;
                if (ply == 0)
                {
                    bestMove = move;
                }
            }
        }

        //Store evaluation in transposition Table
        ttable.StoreEvaluation(depth, ply, alpha, evaluationBound, bestMove);

        return alpha;
    }


    private int QSearch(Board board, int alpha, int beta)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        qnodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        //Evaluate the position
        int standPat = eval.EvaluatePosition(board) * (board.IsWhiteToMove ? 1 : -1);

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
            int score = -QSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }
}