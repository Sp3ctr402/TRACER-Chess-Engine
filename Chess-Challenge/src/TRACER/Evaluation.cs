using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chess_Challenge.src.TRACER
{
    internal class Evaluation
    {
        // Piece values:                                .,   P,   K,   B,   R,   Q,      K
        private static readonly int[] mgPieceValues = { 0,  82, 337, 365, 477, 1025, 100000};
        private static readonly int[] egPieceValues = { 0,  94, 281, 297, 512,  936, 100000};

        //Center Bitboard
        public ulong centerBitboard = ((ulong)1 << 27) | ((ulong)1 << 28) | ((ulong)1 << 35) | ((ulong)1 << 36);

        //Midgame detection -> is 1 when all pieces are on the board
        private static int Mgd(Board board)
        {
            return BitOperations.PopCount(board.AllPiecesBitboard)/32;
        }
        //Endgame detection -> gets near 1 if less figures are on the board
        private static int Egd(Board board)
        {
            return 1 - Mgd(board);
        }

        //Get the value of a single piece regarding all parameters 
        public int GetFigureScore(Board board, int squareIndex, Piece piece)
        {
            
            int figureScore = 0;    //the Value the piece has on the given square
            int[] mgTable = mgPawnTableW;   //PiecesquareTable to use for midgame
            int[] egTable = egPawnTableW;   //PiecesquareTable to use for endgame
            int midGame = Mgd(board); //MidGame Value, to just calc once
            int endGame = Egd(board); //EndGame Value, to just calc once

            //Figure out which PieceSquare MidGame and EndGame Table to use
            switch (piece.PieceType) 
            {
                case PieceType.None:
                    figureScore = 0;
                    break;
                case PieceType.Pawn:
                    mgTable = piece.IsWhite ? mgPawnTableW : mgPawnTableB;
                    egTable = piece.IsWhite ? egPawnTableW : egPawnTableB;
                    break;
                case PieceType.Knight:
                    mgTable = piece.IsWhite ? mgKnightTableW : mgKnightTableB;
                    egTable = piece.IsWhite ? egKnightTableW : egKnightTableB;
                    break;
                case PieceType.Bishop:
                    mgTable = piece.IsWhite ? mgBishopTableW : mgBishopTableB;
                    egTable = piece.IsWhite ? egBishopTableW : egBishopTableB;
                    break;
                case PieceType.Rook:
                    mgTable = piece.IsWhite ? mgRookTableW : mgRookTableB;
                    egTable = piece.IsWhite ? egRookTableW : egRookTableB;
                    break;
                case PieceType.Queen:
                    mgTable = piece.IsWhite ? mgQueenTableW : mgQueenTableB;
                    egTable = piece.IsWhite ? egQueenTableW : egQueenTableB;
                    break;
                case PieceType.King:
                    mgTable = piece.IsWhite ? mgKingTableW : mgKingTableB;
                    egTable = piece.IsWhite ? egKingTableW : egKingTableB;
                    break;
                default:
                    break;
            }
            //Add the Value of the Piece in the given Stage
            figureScore += midGame * mgPieceValues[(int)piece.PieceType] + endGame * egPieceValues[(int)piece.PieceType];
            //Add the value of the square for the given piece to its value
            figureScore += midGame * mgTable[squareIndex] + endGame * egTable[squareIndex];
            //Add a factor to the piece value depending on its usage
            figureScore += FigureBonus(board, squareIndex, piece.IsWhite, piece.PieceType);

            //return final value for the piece
            return figureScore;
        }

        public int EvaluatePosition(Board board)
        {
            int whiteMaterial = 0;  //Value of white Material
            int blackMaterial = 0;  //Value of black Material
            int squareIndex;    //Square index (0-63) of the piece

            ulong pieces = board.AllPiecesBitboard; //bitboard where each square containing a piece is turned to 1

            //As long as there a pieces on the board (var pieces is unequal to zero) get the Index of that
            //square and calculate the pieces value and add it to its color Material
            while (pieces != 0)
            {
                Piece piece = board.GetPiece(new(squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces)));
                if (piece.IsWhite)
                    whiteMaterial += GetFigureScore(board, squareIndex, piece);
                else
                    blackMaterial += GetFigureScore(board, squareIndex, piece);
            }

            //return MAterial balance
            return whiteMaterial - blackMaterial;
        }

        //Depending on pieceType calculate a factor for the value of the piece depending on its use
        private int FigureBonus(Board board, int squareIndex, bool isWhite, PieceType pieceType)
        {
            int figureScore = 0;

            switch (pieceType)
            {
                case PieceType.None:
                    break;
                case PieceType.Pawn:
                    figureScore = PawnBonus(board, squareIndex, isWhite);
                    break;
                case PieceType.Knight:
                    figureScore = KnightBonus(board, squareIndex, isWhite);
                    break;
                case PieceType.Bishop:
                    figureScore = BishopBonus(board, squareIndex, isWhite);
                    break;
                case PieceType.Rook:
                    figureScore = RookBonus(board, squareIndex, isWhite);
                    break;
                case PieceType.Queen:
                    figureScore = QueenBonus(board, squareIndex, isWhite);
                    break;
                case PieceType.King:
                    figureScore = KingBonus(board, squareIndex, isWhite);
                    break;
                default:
                    break;
            }
            return figureScore;
        }

        #region FigureBonus Calculation
        //Calculate PawnBonus depending on
        //-Pawn Structure
        //-passed Pawns
        //-Pawn Center
        private int PawnBonus(Board board, int squareIndex, bool isWhite)
        {
            //Variables
            int pawnBonus = 1;          //Bonus the pawn gets
            int midGame = Mgd(board);   //MidGame detection to just calc one time 
            int endGame = Egd(board);   //EndGame detection to just calc one time 

            ulong PawnsBB = isWhite ? board.GetPieceBitboard(PieceType.Pawn, true) : board.GetPieceBitboard(PieceType.Pawn, false); //Bitboard of all pawns of corresponding colour
            ulong PawnATT = BitboardHelper.GetPawnAttacks(new Square(squareIndex), isWhite); //Bitboard of the specific pawn attacks
            ulong centerMask = ((ulong)1 << 27) | ((ulong)1 << 28) | ((ulong)1 << 35) | ((ulong)1 << 36); //Bitboard where center square are set to 1

            //Pawn structure calculation -> give Bonus when pawn is protected by another pawn
            if (isWhite)
            {
                if ((squareIndex - 9 >= 0) && BitboardHelper.SquareIsSet(PawnsBB, new Square(squareIndex - 9)))
                    pawnBonus += 12 * midGame + 14 * endGame;
                if ((squareIndex - 7 >= 0) && BitboardHelper.SquareIsSet(PawnsBB, new Square(squareIndex - 7)))
                    pawnBonus += 12 * midGame + 14 * endGame;
            }
            else 
            {
                if ((squareIndex + 9 <= 63) && BitboardHelper.SquareIsSet(PawnsBB, new Square(squareIndex + 9)))
                    pawnBonus += 12 * midGame + 14 * endGame;
                if ((squareIndex + 7 <= 63) && BitboardHelper.SquareIsSet(PawnsBB, new Square(squareIndex + 7)))
                    pawnBonus += 12 * midGame + 14 * endGame;
            }

            //Center Bonus Calculation -> give Bonus when being in the center and minor Bonus when attacking the Center (27,28,35,36)
            if (isWhite)
            {
                //reward being in own center
                if (squareIndex == 27 || squareIndex == 28)
                    pawnBonus += 10 * midGame + 7 * endGame;
                //reward being in enemy center
                if (squareIndex == 35 || squareIndex == 36)
                    pawnBonus += 12 * midGame + 9 * endGame;
            }
            else 
            {
                //reward being in own center
                if (squareIndex == 35 || squareIndex == 36)
                    pawnBonus += 10 * midGame + 7 * endGame;
                //reward being in enemy center
                if (squareIndex == 27 || squareIndex == 28)
                    pawnBonus += 12 * midGame + 9 * endGame;
            }
            //If pawnATT and centerMask Bitboard are AND-entangled and != 0, the pawn attacks a center square and gets the bonus
            //a pawn can only attack one center square simultaneously
            if((PawnATT & centerMask) != 0)
                pawnBonus += 5 * midGame + 3 * endGame;

            //passed Pawn calculation -> if the pawn is a passed pawn, give bonus depending on the rank he is on
            if (passedPawnDetection(board, new Square(squareIndex), isWhite) == true)
            {
                if (isWhite)
                {
                    if (new Square(squareIndex).Rank == 4)
                        pawnBonus += 25;
                    if (new Square(squareIndex).Rank == 5)
                        pawnBonus += 45;
                }
                else
                {
                    if (new Square(squareIndex).Rank == 5)
                        pawnBonus += 25;
                    if (new Square(squareIndex).Rank == 4)
                        pawnBonus += 45;
                }

            }

            return pawnBonus;
        }

        //Detect if the looked at pawn is a passed pawn (no enemy pawns on )
        private bool passedPawnDetection(Board board, Square square, bool isWhite)
        {
            bool isPassed = false;  //bool thats getting returned
            ulong enemyPawns = board.GetPieceBitboard(PieceType.Pawn, !isWhite);    //Bitboard of enemy pawns
            ulong passedFiles = 0;  //Bitboard of adjacent Files (if pawn on b file: a,b and c files are set to 1)
            ulong passedRanks = 0;  //Bitboard of every rank (greater if white, lower if black) then figure rank is set to 1
            ulong passedMask;   //Bitboard to detect if the pawn is a passed pawn



            passedMask = passedFiles & passedRanks;

            if((enemyPawns & passedMask) == 0)
                isPassed = true;

            return isPassed;
        }

        private int KnightBonus(Board board, int squareIndex, bool isWhite)
        {
            int knightFactor = 1;

            return knightFactor;
        }
        private int BishopBonus(Board board, int squareIndex, bool isWhite)
        {
            int bishopFactor = 1;

            return bishopFactor;
        }
        private int RookBonus(Board board, int squareIndex, bool isWhite)
        {
            int rookFactor = 1;

            return rookFactor;
        }
        private int QueenBonus(Board board, int squareIndex, bool isWhite)
        {
            int queenFactor = 1;

            return queenFactor;
        }
        private int KingBonus(Board board, int squareIndex, bool isWhite)
        {
            int kingFactor = 1;

            return kingFactor;
        }
        #endregion

        #region PieceSqaureTables - PESTOS Evaluation
        //Index 0-7 are squares a1 - h1, 8-15 a2-h2 etc
        private static readonly int[] mgPawnTableW =
        {
               0,   0,   0,   0,   0,   0,  0,   0,
             -35,  -1, -20, -23, -15,  24, 38, -22,
             -26,  -4,  -4, -10,   3,   3, 33, -12,
             -27,  -2,  -5,  12,  17,   6, 10, -25,
             -14,  13,   6,  21,  23,  12, 17, -23,
              -6,   7,  26,  31,  65,  56, 25, -20,
              98, 134,  61,  95,  68, 126, 34, -11,
               0,   0,   0,   0,   0,   0,  0,   0
        };
        private static readonly int[] mgPawnTableB = mgPawnTableW.Reverse().ToArray();
        private static readonly int[] egPawnTableW =
        {
               0,   0,   0,   0,   0,   0,   0,   0,
              13,   8,   8,  10,  13,   0,   2,  -7,
               4,   7,  -6,   1,   0,  -5,  -1,  -8,
              13,   9,  -3,  -7,  -7,  -8,   3,  -1,
              32,  24,  13,   5,  -2,   4,  17,  17,
              94, 100,  85,  67,  56,  53,  82,  84,
             178, 173, 158, 134, 147, 132, 165, 187,
               0,   0,   0,   0,   0,   0,   0,   0
        };
        private static readonly int[] egPawnTableB = egPawnTableW.Reverse().ToArray();
        private static readonly int[] mgKnightTableW =
        {
            -105, -21, -58, -33, -17, -28, -19,  -23,
             -29, -53, -12,  -3,  -1,  18, -14,  -19,
             -23,  -9,  15,  10,  19,  17,  25,  -16,
             -13,   4,  16,  13,  28,  19,  21,   -8,
              -9,  11,  19,  53,  37,  69,  12,   22,
             -47,  60,  37,  65,  84, 129,  73,   44,
             -73, -41,  72,  36,  23,  62,   7,  -17,
            -167, -89, -34, -49,  61, -97, -15, -107
        };
        private static readonly int[] mgKnightTableB = mgKnightTableW.Reverse().ToArray();
        private static readonly int[] egKnightTableW =
        {
            -29, -51, -23, -15, -22, -18, -50, -64,
            -42, -20, -10,  -5,  -2, -20, -23, -44,
            -23,  -3,  -1,  15,  10,  -3, -20, -22,
            -18,  -6,  16,  25,  16,  17,   4, -18,
            -17,   3,  22,  22,  22,  11,   8, -18,
            -24, -20,  10,   9,  -1,  -9, -19, -41,
            -25,  -8, -25,  -2,  -9, -25, -24, -52,
            -58, -38, -13, -28, -31, -27, -63, -99
        };
        private static readonly int[] egKnightTableB = egKnightTableW.Reverse().ToArray();
        private static readonly int[] mgBishopTableW =
        {
            -33,  -3, -14, -21, -13, -12, -39, -21,
              4,  15,  16,   0,   7,  21,  33,   1,
              0,  15,  15,  15,  14,  27,  18,  10,
             -6,  13,  13,  26,  34,  12,  10,   4,
             -4,   5,  19,  50,  37,  37,   7,  -2,
            -16,  37,  43,  40,  35,  50,  37,  -2,
            -26,  16, -18, -13,  30,  59,  18, -47,
            -29,   4, -82, -37, -25, -42,   7,  -8
        };
        private static readonly int[] mgBishopTableB = mgBishopTableW.Reverse().ToArray();
        private static readonly int[] egBishopTableW =
        {
            -23,  -9, -23,  -5, -9, -16,  -5, -17,
            -14, -18,  -7,  -1,  4,  -9, -15, -27,
            -12,  -3,   8,  10, 13,   3,  -7, -15,
             -6,   3,  13,  19,  7,  10,  -3,  -9,
             -3,   9,  12,   9, 14,  10,   3,   2,
              2,  -8,   0,  -1, -2,   6,   0,   4,
             -8,  -4,   7, -12, -3, -13,  -4, -14,
            -14, -21, -11,  -8, -7,  -9, -17, -24
        };
        private static readonly int[] egBishopTableB = egBishopTableW.Reverse().ToArray();
        private static readonly int[] mgRookTableW =
        {
            -19, -13,   1,  17, 16,  7, -37, -26,
            -44, -16, -20,  -9, -1, 11,  -6, -71,
            -45, -25, -16, -17,  3,  0,  -5, -33,
            -36, -26, -12,  -1,  9, -7,   6, -23,
            -24, -11,   7,  26, 24, 35,  -8, -20,
             -5,  19,  26,  36, 17, 45,  61,  16,
             27,  32,  58,  62, 80, 67,  26,  44,
             32,  42,  32,  51, 63,  9,  31,  43
        };
        private static readonly int[] mgRookTableB = mgRookTableW.Reverse().ToArray();
        private static readonly int[] egRookTableW =
        {
            -9,  2,  3, -1, -5, -13,   4, -20,
            -6, -6,  0,  2, -9,  -9, -11,  -3,
            -4,  0, -5, -1, -7, -12,  -8, -16,
             3,  5,  8,  4, -5,  -6,  -8, -11,
             4,  3, 13,  1,  2,   1,  -1,   2,
             7,  7,  7,  5,  4,  -3,  -5,  -3,
            11, 13, 13, 11, -3,   3,   8,   3,
            13, 10, 18, 15, 12,  12,   8,   5
        };
        private static readonly int[] egRookTableB = egRookTableW.Reverse().ToArray();
        private static readonly int[] mgQueenTableW =
        {
             -1, -18,  -9,  10, -15, -25, -31, -50,
            -35,  -8,  11,   2,   8,  15,  -3,   1,
            -14,   2, -11,  -2,  -5,   2,  14,   5,
             -9, -26,  -9, -10,  -2,  -4,   3,  -3,
            -27, -27, -16, -16,  -1,  17,  -2,   1,
            -13, -17,   7,   8,  29,  56,  47,  57,
            -24, -39,  -5,   1, -16,  57,  28,  54,
            -28,   0,  29,  12,  59,  44,  43,  45
        };
        private static readonly int[] mgQueenTableB = mgQueenTableW.Reverse().ToArray();
        private static readonly int[] egQueenTableW =
        {
            -33, -28, -22, -43,  -5, -32, -20, -41,
            -22, -23, -30, -16, -16, -23, -36, -32,
            -16, -27,  15,   6,   9,  17,  10,   5,
            -18,  28,  19,  47,  31,  34,  39,  23,
              3,  22,  24,  45,  57,  40,  57,  36,
            -20,   6,   9,  49,  47,  35,  19,   9,
            -17,  20,  32,  41,  58,  25,  30,   0,
             -9,  22,  22,  27,  27,  19,  10,  20
        };
        private static readonly int[] egQueenTableB = egQueenTableW.Reverse().ToArray();
        private static readonly int[] mgKingTableW =
        {
            -15,  36,  12, -54,   8, -28,  24,  14,
              1,   7,  -8, -64, -43, -16,   9,   8,
            -14, -14, -22, -46, -44, -30, -15, -27,
            -49,  -1, -27, -39, -46, -44, -33, -51,
            -17, -20, -12, -27, -30, -25, -14, -36,
             -9,  24,   2, -16, -20,   6,  22, -22,
             29,  -1, -20,  -7,  -8,  -4, -38, -29,
            -65,  23,  16, -15, -56, -34,   2,  13
        };
        private static readonly int[] mgKingTableB = mgKingTableW.Reverse().ToArray();
        private static readonly int[] egKingTableW =
        {
            -53, -34, -21, -11, -28, -14, -24, -43,
            -27, -11,   4,  13,  14,   4,  -5, -17,
            -19,  -3,  11,  21,  23,  16,   7,  -9,
            -18,  -4,  21,  24,  27,  23,   9, -11,
             -8,  22,  24,  27,  26,  33,  26,   3,
             10,  17,  23,  15,  20,  45,  44,  13,
            -12,  17,  14,  17,  17,  38,  23,  11,
            -74, -35, -18, -18, -11,  15,   4, -17
        };
        private static readonly int[] egKingTableB = egKingTableW.Reverse().ToArray();
        #endregion
    }
}

