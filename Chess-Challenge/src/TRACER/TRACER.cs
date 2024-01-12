using Chess_Challenge.src.TRACER;
using ChessChallenge.API;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Windows;

//TRACER_V1 NegaMax(+) | AlphaBeta(+) | Material Balance(+)

public class TRACER : IChessBot
{
    //Constant Variables
    private const int DEPTH = 4; //Search depth
    private const int MATE = 30000; //Score for Mate

    //Global Variables
    Move bestMove; //best Move that will be played

    //Used Classes
    Evaluation eval = new Evaluation();

    public Move Think(Board board, Timer timer)
    {
        Search(board, DEPTH, -MATE, MATE, 0);
        return bestMove;
    }

    private int Search(Board board, int depth, int alpha, int beta, int ply)
    {
        //when root is reached evaluate the position
        //relative score -> if white is winning and its blacks turn Evaluation should return a negative score
        //since my Evaluation returns the score from a watchers perspective we need to multiply evaluation by whos turn it is
        if (depth == 0)
            return eval.EvaluatePosition(board) * (board.IsWhiteToMove ? 1 : -1);
        //If in Mate return negative mate score adjusted by ply to encourage earlier checkmates
        if (board.IsInCheckmate())
            return -MATE + ply;
        //If the position is draw then return deadequal
        if (board.IsDraw())
            return 0;

        //Recursive call of Search function to find best Move
        //In all lega moves
        Move[] moves = board.GetLegalMoves();
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
}