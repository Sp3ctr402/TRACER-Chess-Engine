using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER_V1
{
    internal class Evaluation
    {
        // Piece values:                                .,   P,   K,   B,   R,   Q,      K
        private static readonly int[] mgPieceValues = { 0,  80, 335, 363, 460, 940, 100000};
        private static readonly int[] egPieceValues = { 0, 100, 305, 333, 563, 950, 100000};

        //Endgame Transition
        private int egT(Board board)
        {
            return BitOperations.PopCount(board.AllPiecesBitboard);
        }
        //Get the value of a single piece regarding all parameters .TODO

        public int EvaluatePosition(Board board)
        {
            int whiteMaterial = 0;
            int blackMaterial = 0;

            for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                whiteMaterial += mgPieceValues[(int)pieceType] * BitOperations.PopCount(board.GetPieceBitboard(pieceType, true));
                blackMaterial += mgPieceValues[(int)pieceType] * BitOperations.PopCount(board.GetPieceBitboard(pieceType, false));
            }

            return whiteMaterial - blackMaterial;
        }
    }
}
