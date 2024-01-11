using Chess_Challenge.src.TRACER;
using ChessChallenge.API;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Windows;

//TRACER_V1 MiniMax(-) | AlphaBeta(-) | Material Balance(-)

public class TRACER : IChessBot
{
    //Constant Variables
    private const int DEPTH = 4; //Search depth

    //Used classes
    Search findMove = new Search();

    public Move Think(Board board, Timer timer)
    {
        //Variables
        int moveScore; //Score a certain move has
        int bestScore = int.MinValue; //Score of the current best move (low value on start to guarantee update)
        Move bestMove; //Current best move to be played

        //Generate all legal possible moves
        Move[] moves = board.GetLegalMoves();
        bestMove = moves[0];

        foreach (Move currentMove in moves)
        {
            //for every move -> make move virtually and evaluate how good it is
            board.MakeMove(currentMove);
            moveScore = findMove.NegaMaxAB(board, DEPTH, int.MinValue, int.MaxValue, board.IsWhiteToMove);
            board.UndoMove(currentMove);

            //if the score of this move is better than the current best -> switch
            if (moveScore > bestScore) 
            {
                bestScore = moveScore;
                bestMove = currentMove;
            }
            Debug.WriteLine("currentMove:{0} | moveScore:{1}", currentMove, moveScore);
        }
        Debug.WriteLine("---- bestMove:{0} | bestScore:{1} ----", bestMove, bestScore);
        return bestMove;
    }
}