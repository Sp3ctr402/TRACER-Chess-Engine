using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER
{
    internal class MoveOrdering
    {
        //Classes used
        Evaluation eval = new Evaluation();

        //Order Moves based on evaluation
        public void OrderMoves(Move[] moves, Board board)
        {
            Array.Sort(moves, (move1, move2) =>
            {
                int score1 = EvaluateMove(move1, board);
                int score2 = EvaluateMove(move2, board);

                // Sort in descending order (highest score first)
                return score2.CompareTo(score1);
            });
        }

        //Evaluate the moves by guesses
        private int EvaluateMove(Move move, Board board)
        {
            int moveScoreGuess = 0; //Score the move gets. The higher the score the earlier it gets sorted in the Array
            Square startSquare = move.StartSquare; //Starting square of the move
            Square targetSquare = move.TargetSquare; //Target square of the move
            Piece movedPiece = board.GetPiece(startSquare); //moved Piece
            Piece targetPiece = board.GetPiece(startSquare); //target Piece

            //Moves capturing high value pieces with low value pieces are usually good
            if (move.CapturePieceType != 0)
                moveScoreGuess +=
                    10 * eval.GetFigureScore(board, targetSquare.Index, targetPiece) //Score of the targetted piece
                    - eval.GetFigureScore(board, startSquare.Index, movedPiece);    //Score of the moved Piece

            //Promoting Pawns is usually a good idea
            if (move.IsPromotion)
                moveScoreGuess +=
                    //score of a queen which the Engine will promote to
                    eval.GetFigureScore(board, targetSquare.Index, new Piece(PieceType.Queen, board.IsWhiteToMove, targetSquare));

            //Putting figures into the attack of opposite pawns is usually bad
            if (board.SquareIsAttackedByOpponentPawn(targetSquare))
                moveScoreGuess -= eval.GetFigureScore(board, startSquare.Index, movedPiece);

            return moveScoreGuess;
        }
    }
}

