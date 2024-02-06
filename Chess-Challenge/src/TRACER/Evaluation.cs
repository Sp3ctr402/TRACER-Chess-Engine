using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER
{
    internal class Evaluation
    {
        // Piece values:
        // https://en.wikipedia.org/wiki/Chess_piece_relative_value - Larry Kaufman's values 
        // (for rooks/queens add both and divide by 2)
        // middlegame is when each side has the same amount of queens
        // threshold is when there is an imbalance of queens
        // endgame is when there are no queens on the board
        // Queen values dont change since when there a equal numbers they cancel out
        //--------------------------------------------{ .,    P,   K,   B,   R,   Q,      K}
        private static readonly int[] mgPieceValues = { 0,   80, 305, 333, 460, 905, 100000};
        private static readonly int[] thPieceValues = { 0,   90, 305, 333, 485, 905, 100000};
        private static readonly int[] egPieceValues = { 0,  100, 305, 333, 515, 905, 100000};
        private int[] pieceValues;


        //Function to detect GamePhases according to Larry Kaufman
        //https://en.wikipedia.org/wiki/Chess_piece_relative_value
        public void DetectGamePhase(Board board, int numWhiteQueens, int numBlackQueens)
        {
            // middle game (both sides have queens)
            // end game (both sides have no queens)
            //meaning if both sides are equal we just have to look, 
            //if there is a queen on the board to determine game phase
            if (numWhiteQueens == numBlackQueens)
            {
                if (numWhiteQueens != 0)
                {
                    // middle game pieceValue
                    pieceValues = mgPieceValues;
                }

                //since both sides have equal number of queens 
                //and there are no queens on the board, its end game
                else
                {                 
                    pieceValues = egPieceValues;
                }

            }

            //since there is an queen imbalance (both sides dont have equal nums of queens)
            //it must be threshold
            else
            {
                pieceValues = thPieceValues;
            }
            
        }


        //Function to calculate Material Balance
        public int EvaluatePosition(Board board)
        {
            // Value of white Material
            int whiteMaterial = 0;
            //Value of black Material
            int blackMaterial = 0;
            //Square index (0-63) of the piece
            int squareIndex;

            //bitboard where each square containing a piece is turned to 1
            ulong pieces = board.AllPiecesBitboard; 

            //Detect Game Phase of Evaluation to get the proper piece values
            DetectGamePhase(board, BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, true)),
                                   BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, false)));

            //As long as there a pieces on the board (var pieces is unequal to zero) get the Index of that
            //square and calculate the pieces value and add it to its color Material
            while (pieces != 0)
            {
                Piece piece = board.GetPiece(new(squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces)));
                if (piece.IsWhite)
                    whiteMaterial += GetFigureScore(board, squareIndex, piece, pieceValues);
                else
                    blackMaterial += GetFigureScore(board, squareIndex, piece, pieceValues);
            }

            //return Material balance
            return whiteMaterial - blackMaterial;
        }


        //Figure to get the value of a single piece regarding all parameters 
        public int GetFigureScore(Board board, int squareIndex, Piece piece, int[] pieceValues)
        {
            
            int figureScore = 0;    //the Value the piece has on the given square

            //Figure out which PieceSquare MidGame and EndGame Table to use
            switch (piece.PieceType)
            {
                case PieceType.None:
                    figureScore = 0;
                    break;
                case PieceType.Pawn:
                    figureScore += pieceValues[(int)PieceType.Pawn];
                    break;
                case PieceType.Knight:
                    figureScore += pieceValues[(int)PieceType.Knight];
                    break;
                case PieceType.Bishop:
                    figureScore += pieceValues[(int)PieceType.Bishop];
                    break;
                case PieceType.Rook:
                    figureScore += pieceValues[(int)PieceType.Rook];
                    break;
                case PieceType.Queen:
                    figureScore += pieceValues[(int)PieceType.Queen];
                    break;
                case PieceType.King:
                    figureScore += pieceValues[(int)PieceType.King];
                    break;
                default:
                    break;
            }

            //return final value for the piece
            return figureScore;
        }
    }
}

