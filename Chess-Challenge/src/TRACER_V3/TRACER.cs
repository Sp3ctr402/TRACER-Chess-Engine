using Chess_Challenge.src.TRACER;
using ChessChallenge.API;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Windows;

//TRACER_V1 NegaMax(+) | AlphaBeta(+) | Material Balance(+)
//TRACER_V2 PieceSquareTable (+) | Endgame Transition (+)               +/- 0 Elo
//TRACER_V3 QSearch(+) | DeltaPruning (+) | MoveOrdering(+)             +250 +/- 35 Elo   

public class TRACER_V3 : IChessBot
{
    //Constant Variables
    private const int DEPTH = 4; //Search depth
    private const int DEPTH_QSEARCH = 8; //QSearch depth
    private const int MATE = 30000; //Score for Mate

    //Global Variables
    private Move bestMove; //best Move that will be played

    //------------------------- #DEBUG -------------------------
    private int nodesSearched;
    private int qnodesSearched;
    private int deltaPruned;
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
        //------------------------- #DEBUGEND ----------------------

        Search(board, DEPTH, -MATE, MATE, 0);

        //------------------------- #DEBUG -------------------------
        //Console.WriteLine("NoPS/QSP: {0}/{2} | Eval: {1} | DeltaPruned: {3}"
        //    , nodesSearched, eval.EvaluatePosition(board), qnodesSearched, deltaPruned);
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
            if (alpha >= beta)
                return beta;
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
        if (ply >= DEPTH_QSEARCH)
            return standPat;

        //Check for beta cutoff
        if (standPat >= beta)
            return beta;

        //Delta-Pruning 
        //chessprogrammin.org/Delta_Pruning
        const int BIG_DELTA = 500; //High value near Queen
        if (standPat < alpha - BIG_DELTA)
        {
            deltaPruned++;
            return alpha;
        }

        //Update alpha if necessary
        if (alpha < standPat)
            alpha = standPat;  

        //Find all capture moves and order them
        Move[] qSearchMoves = board.GetLegalMoves(true);
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