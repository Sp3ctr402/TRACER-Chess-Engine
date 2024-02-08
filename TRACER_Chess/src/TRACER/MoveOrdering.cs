using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER
{
    internal class MoveOrdering
    {
        // Piece values:
        // https://en.wikipedia.org/wiki/Chess_piece_relative_value - Kaufmans System (eg Piece Values)
        // should make no difference which piece values we take because they will be used to determine 
        // how good a capture is so as long as P < Kn < B < R < Q < K all should be good
        //---------------------------------------------.,  P,   K,   B,   R,   Q,      K
        private static readonly int[] pieceValues = { 0, 100, 305, 333, 515, 905, 100000 };


        // Global constant Variables
        // values to add to a move score depending on what type of move it is
        // including all moves listed in: https://www.chessprogramming.org/Move_Ordering
        private const int HASHMOVE = 1_000_000;
        private const int WINNINGCAPTURES = 500_000;
        private const int PROMOTIONS = 100_000;
        private const int KILLER = 10_000;  //Detect KillerMoves TODO
        private const int EQUALCAPTURES = 5_000;
        private const int LOOSINGCAPTURES = 1_000;

        //Order Moves based on evaluation
        public void OrderMoves(Move[] moves, Board board, Move thisMoveFirst)
        {
            // Sort array based on score
            Array.Sort(moves, (move1, move2) =>
            {
                int score1 = EvaluateMove(move1, board, thisMoveFirst);
                int score2 = EvaluateMove(move2, board, thisMoveFirst);

                // sort array descending order
                return score2.CompareTo(score1); 
            });
        }


        // Give Moves a score which is based on assumptions on which moves are good
        // the order in which moves will be sorted is based on following article
        // https://www.chessprogramming.org/Move_Ordering
        private int EvaluateMove(Move move, Board board, Move thisMoveFirst)
        {
            // the final score of the move 
            int moveScore = 0;
            
            PieceType movingPieceType = move.MovePieceType;
            PieceType capturePieceType = move.CapturePieceType;
            PieceType promotionPieceType = move.PromotionPieceType;


            // Guarantee to look at the hashMove first (bestMove prev Search Iteration)
            if(move == thisMoveFirst)
            {
                moveScore += HASHMOVE;
            }


            // Look at capture moves
            // capture high material with low material
            // due to calculation better captures will automatically be sorted higher
            // than worse captures
            if(move.IsCapture)
            {
                // Get values of pieces
                int movingPieceValue = pieceValues[(int)movingPieceType];
                int capturePieceValue = pieceValues[(int)capturePieceType];


                // Test on winning captures
                if(capturePieceValue > movingPieceValue)
                {
                    moveScore += capturePieceValue - movingPieceValue + WINNINGCAPTURES;
                }

                
                // Test on equal captures
                else if(capturePieceValue == movingPieceValue)
                {
                    moveScore += EQUALCAPTURES;
                }


                // Loosing captures 
                // if the capture is no winning capture and no equal capture
                // it has to be a loosing capture
                else
                {
                    moveScore += capturePieceValue - movingPieceValue + LOOSINGCAPTURES;
                }
            }


            // Look at Promotions
            // Promotions to higher valued pieces will be sorted higher
            if(move.IsPromotion)
            {
                // Get promotion piece Value
                int promotionPieceValue = pieceValues[(int)promotionPieceType];


                moveScore += promotionPieceValue + PROMOTIONS;
            }


            return moveScore;
        }
    }
}

