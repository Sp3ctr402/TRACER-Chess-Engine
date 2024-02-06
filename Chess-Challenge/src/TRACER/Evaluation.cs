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
        private static readonly int[] mgPieceValues = { 0, 80, 305, 333, 460, 905, 100000 };
        private static readonly int[] thPieceValues = { 0, 90, 305, 333, 485, 905, 100000 };
        private static readonly int[] egPieceValues = { 0, 100, 305, 333, 515, 905, 100000 };
        private int[] pieceValues;


        // A function to multiply Middle Game Values with
        // gets closer to zero the less enemy pieces are on the board
        private double MidGameValue(int numEnemyPieces)
        {
            return numEnemyPieces / 16.0;
        }


        // A function to multiply End Game Values with
        // gets closer to one the less enemy pieces are on the board
        private double EndGameValue(int numEnemyPieces)
        {
            return 1 - MidGameValue(numEnemyPieces);
        }


        // Function to detect GamePhases according to Larry Kaufman and adjust pieceValues
        // https://en.wikipedia.org/wiki/Chess_piece_relative_value
        private void DetectPieceValues(int numWhiteQueens, int numBlackQueens)
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


        // Function to calculate Material Balance
        public int EvaluatePosition(Board board)
        {
            // Value of white Material
            int whiteMaterial = 0;
            // Value of black Material
            int blackMaterial = 0;
            // Square index (0-63) of the piece
            int squareIndex;
            // number of enemy pieces depending on which player is to move
            int numberOfEnemyPieces = board.IsWhiteToMove ? BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard) :
                                                            BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard);
            // MidGame multiplier
            double midGame = MidGameValue(numberOfEnemyPieces);
            // EndGame multiplier
            double endGame = EndGameValue(numberOfEnemyPieces);


            //bitboard where each square containing a piece is turned to 1
            ulong pieces = board.AllPiecesBitboard;


            //Detect Game Phase of Evaluation to get the proper piece values
            DetectPieceValues(BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, true)),
                              BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, false)));


            // As long as there a pieces on the board (var pieces is unequal to zero) get the Index of that
            // square and calculate the pieces value and add it to its color Material
            // ClearAndGetIndex will set the corresponding bit to 0 so we dont look at the same piece twice
            while (pieces != 0)
            {
                Piece piece = board.GetPiece(new(squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces)));
                if (piece.IsWhite)
                    whiteMaterial += GetFigureScore(board, squareIndex, piece, pieceValues, midGame, endGame);
                else
                    blackMaterial += GetFigureScore(board, squareIndex, piece, pieceValues, midGame, endGame);
            }


            //return Material balance
            return whiteMaterial - blackMaterial;
        }


        // Figure to get the value of a single piece regarding all parameters 
        public int GetFigureScore(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        {
            // the Value the piece has on the given square
            int figureScore = 0;


            //Figure out which PieceSquare MidGame and EndGame Table to use
            switch (piece.PieceType)
            {
                case PieceType.None:
                    figureScore += 0;
                    break;


                case PieceType.Pawn:
                    figureScore += PawnEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                case PieceType.Knight:
                    figureScore += KnightEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                case PieceType.Bishop:
                    figureScore += BishopEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                case PieceType.Rook:
                    figureScore += RookEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                case PieceType.Queen:
                    figureScore += QueenEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                case PieceType.King:
                    figureScore += KingEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                default:
                    break;
            }


            // return final value for the piece
            return figureScore;
        }


        // Piece Evaluation based on 
        // https://www.chessprogramming.org/Evaluation_of_Pieces#Queen
        // and https://en.wikipedia.org/wiki/Chess_piece_relative_value
        #region Piece Evaluation


        // Evaluate Pawns
        // -PawnValue MG/EG (depending on File and Rank)
        // -Pawn structure  (Doubled/Passed/Connected/Isolated)
        private int PawnEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        { 
            // Variables
            int pawnScore = 0;
            bool isIsolated = false;
            bool isConnected = false;
            bool isPassed = false;


            // get the current score of a pawn
            pawnScore += pieceValues[(int)piece.PieceType];  



            return pawnScore;
        }


        // Evaluate Knights
        private int KnightEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        { }


        // Evaluate Bishops
        private int BishopEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        { }


        // Evaluate Rooks
        private int RookEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        { }


        // Evaluate Queens
        private int QueenEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        { }


        // Evaluate Kings
        private int KingEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        { }
        #endregion
    }
}

