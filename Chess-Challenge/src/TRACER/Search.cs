using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER
{
    internal class Search
    {
        //used Classes
        Evaluation eval = new Evaluation();

        //NegaMax AlphaBeta Pruning Algorith -> returns a high value for the current play (regardless of colour)
        public int NegaMaxAB(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
        {
            //Variables
            int moveScore; //score of the current move

            //if depth is reached then evaluate position
            if (depth == 0)
                return eval.EvaluatePosition(board);// * (maximizingPlayer ? 1 : -1);
            //if player is checkmated return corresponding value
            if (board.IsInCheckmate())
                return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
            //if game is a draw return equal
            if (board.IsDraw())
                return 0;

            //Get all legal moves
            Move[] moves = board.GetLegalMoves();

            //NegaMax Algorithm
            foreach (var move in moves)
            {
                board.MakeMove(move);
                int value = -NegaMaxAB(board, depth - 1, -beta, -alpha, !maximizingPlayer);
                board.UndoMove(move);

                alpha = Math.Max(alpha, value);

                if (alpha >= beta)
                {
                    break; // Beta-Cut-off
                }
            }

            return alpha;

        }
    }
}
