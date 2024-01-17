using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER_V4
{
    internal class MoveOrdering
    {
        //Classes used
        Evaluation eval = new Evaluation();

        //Global variables
        private Move prevIterationBestMove;
        private static readonly int[] PieceValues = { 0, 94, 281, 297, 512, 936, 100000 };

        public void SetPrevIterationBestMove(Move move)
        {
            prevIterationBestMove = move;
        }

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

            //If its the best move from pevius iteration it needs to be sorted first
            if (move == prevIterationBestMove)
                moveScoreGuess += 1000;

            //If move is already stored in TTable then look at it first du get cutoffs quicker TODO

            //Winning captures are good
            if (move.CapturePieceType != 0)
                moveScoreGuess +=
                    10 * eval.GetFigureScore(board, targetSquare.Index, targetPiece) //Score of the targetted piece
                    - eval.GetFigureScore(board, startSquare.Index, movedPiece);    //Score of the moved Piece

            //Promoting Pawns is usually a good idea
            if (move.IsPromotion)
                moveScoreGuess += PieceValues[(int)move.PromotionPieceType];

            //Putting figures into the attack of opposite pawns is usually bad
            if (board.SquareIsAttackedByOpponentPawn(targetSquare))
                moveScoreGuess -= eval.GetFigureScore(board, startSquare.Index, movedPiece);

            return moveScoreGuess;
        }
    }
}

