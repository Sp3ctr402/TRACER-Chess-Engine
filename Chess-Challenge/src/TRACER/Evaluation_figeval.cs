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
    internal class Evaluation_figeval
    {
        // Piece values:
        //https://en.wikipedia.org/wiki/Chess_piece_relative_value - Alpha Zero Piece Values for Mid Game
        //EndGame Values will be determined by Figure evals
        //                                            .,   P,   K,   B,   R,    Q,      K
        private static readonly int[] pieceValues = { 0, 100, 305, 333, 563, 950, 100000 };

        //public Bitboards
        public ulong centerBitboard = ((ulong)1 << 27) | ((ulong)1 << 28) | ((ulong)1 << 35) | ((ulong)1 << 36); //Mask of the center

        //Midgame detection -> is 1 when all pieces are on the board
        private static double Mgd(Board board)
        {
            double popCount = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);
            return popCount / 32.0;
        }
        //Endgame detection -> gets near 1 if less figures are on the board
        private static double Egd(Board board)
        {
            return 1 - Mgd(board);
        }

        //Get the value of a single piece regarding all parameters 
        public int GetFigureScore(Board board, int squareIndex, Piece piece)
        {

            int figureScore = 0;    //the Value the piece has on the given square
            int[] mgTable = mgPawnTableW;   //PiecesquareTable to use for midgame
            int[] egTable = egPawnTableW;   //PiecesquareTable to use for endgame
            double midGame = Mgd(board); //MidGame Value, to just calc once
            double endGame = Egd(board); //EndGame Value, to just calc once

            //Figure out which PieceSquare MidGame and EndGame Table to use
            //and eval the figure itself
            switch (piece.PieceType)
            {
                case PieceType.None:
                    figureScore = 0;
                    break;
                case PieceType.Pawn:
                    mgTable = piece.IsWhite ? mgPawnTableW : mgPawnTableB;
                    egTable = piece.IsWhite ? egPawnTableW : egPawnTableB;
                    figureScore += PawnBonus(board, squareIndex, piece.IsWhite, midGame, endGame);
                    break;
                case PieceType.Knight:
                    mgTable = piece.IsWhite ? mgKnightTableW : mgKnightTableB;
                    egTable = piece.IsWhite ? egKnightTableW : egKnightTableB;
                    figureScore += KnightBonus(board, squareIndex, piece.IsWhite, midGame, endGame);
                    break;
                case PieceType.Bishop:
                    mgTable = piece.IsWhite ? mgBishopTableW : mgBishopTableB;
                    egTable = piece.IsWhite ? egBishopTableW : egBishopTableB;
                    figureScore += BishopBonus(board, squareIndex, piece.IsWhite, midGame, endGame);
                    break;
                case PieceType.Rook:
                    mgTable = piece.IsWhite ? mgRookTableW : mgRookTableB;
                    egTable = piece.IsWhite ? egRookTableW : egRookTableB;
                    figureScore += RookBonus(board, squareIndex, piece.IsWhite, midGame, endGame);
                    break;
                case PieceType.Queen:
                    mgTable = piece.IsWhite ? mgQueenTableW : mgQueenTableB;
                    egTable = piece.IsWhite ? egQueenTableW : egQueenTableB;
                    figureScore += QueenBonus(board, squareIndex, piece.IsWhite, midGame, endGame);
                    break;
                case PieceType.King:
                    mgTable = piece.IsWhite ? mgKingTableW : mgKingTableB;
                    egTable = piece.IsWhite ? egKingTableW : egKingTableB;
                    figureScore += KingBonus(board, squareIndex, piece.IsWhite, midGame, endGame);
                    break;
                default:
                    break;
            }
            //Add the Value of the Piece in the given Stage
            figureScore += pieceValues[(int)piece.PieceType];
            //Add the value of the square for the given piece to its value
            figureScore += (int)(midGame * mgTable[squareIndex] + endGame * egTable[squareIndex]);

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

        //Detect square colour
        //returns true if white square and false when black square
        private bool ColorDetection(int squareIndex)
        {
            Square square = new Square(squareIndex);
            int rank = square.Rank;
            int file = square.File;

            //if white square -> (rank + file) % 2 == 1
            //else its a black square
            if ((rank + file) % 2 == 1)
                return true;

            return false;
        }

        #region FigureBonus Calculation
        //Calculate PawnBonus depending on
        //-Pawn Structure
        //-passed Pawns
        //-Isolated Pawns
        //-connected Bonus
        //-doubled pawn punishment
        //specific values can be found here https://en.wikipedia.org/wiki/Chess_piece_relative_value
        private int PawnBonus(Board board, int squareIndex, bool isWhite, double midGame, double endGame)
        {
            //Variables
            int pawnBonus = 0;          //Bonus the pawn gets
            const int DOUBLED_PAWN_PUNISH = 30; //doubled pawn values 
            int connectedBonus = (int)(15 * midGame + 35 * endGame); //connectedpawn values
            Square square = new Square(squareIndex);    //square of the looked at Pawn
            ulong PawnATT = BitboardHelper.GetPawnAttacks(new Square(squareIndex), isWhite); //Bitboard of the specific pawn attacks
            //Bitboard of all pawns of corresponding colour
            ulong PawnsBB = isWhite ? board.GetPieceBitboard(PieceType.Pawn, true) : board.GetPieceBitboard(PieceType.Pawn, false);
            bool isPassed = PassedPawnDetection(board, square, isWhite);  //passed pawn bool
            bool isConnected = ConnectedPawnDetection(board, square, isWhite, PawnsBB); //connected pawn bool
            bool isIsolated = IsolatedPawnDetection(board, square, isWhite, PawnsBB); //Isolated pawn bool

            //Pawn structure calculation -> give Bonus when pawn is protected by another pawn
            //passed Pawn calculation -> if the pawn is a passed pawn, give bonus depending on the rank he is on
            //values - https://en.wikipedia.org/wiki/Chess_piece_relative_value
            if (isPassed && isConnected)
            {
                if (isWhite)
                {
                    if (square.Rank == 4)
                        pawnBonus += 55;
                    if (square.Rank == 5)
                        pawnBonus += 130;
                }
                else
                {
                    if (square.Rank == 5)
                        pawnBonus += 55;
                    if (square.Rank == 4)
                        pawnBonus += 130;
                }
            }

            if (isPassed)
            {
                if (isWhite)
                {
                    if (square.Rank == 4)
                        pawnBonus += 30;
                    if (square.Rank == 5)
                        pawnBonus += 55;
                }
                else
                {
                    if (square.Rank == 5)
                        pawnBonus += 30;
                    if (square.Rank == 4)
                        pawnBonus += 55;
                }
            }

            if (isConnected)
            {
                pawnBonus += 15 * square.Rank;
            }

            if (isIsolated)
            {
                if (isWhite)
                {
                    if (square.Rank == 3)
                        pawnBonus -= 25;
                    if (square.Rank == 4)
                        pawnBonus += 5;
                    if (square.Rank == 5)
                        pawnBonus += 30;
                }
                else
                {
                    if (square.Rank == 5)
                        pawnBonus -= 25;
                    if (square.Rank == 4)
                        pawnBonus += 5;
                    if (square.Rank == 3)
                        pawnBonus -= 30;
                }
            }

            //doubled Pawn punsihment
            if (isWhite)
            {
                if (BitboardHelper.SquareIsSet(PawnsBB, new Square(squareIndex + 8)))
                    pawnBonus -= DOUBLED_PAWN_PUNISH;
            }
            else
            {
                if (BitboardHelper.SquareIsSet(PawnsBB, new Square(squareIndex - 8)))
                    pawnBonus -= DOUBLED_PAWN_PUNISH;
            }

            return pawnBonus;
        }

        //Detect if the looked at pawn is a passed pawn (no enemy pawns on)
        private bool PassedPawnDetection(Board board, Square square, bool isWhite)
        {
            bool isPassed = false;  //bool thats getting returned
            ulong enemyPawns = board.GetPieceBitboard(PieceType.Pawn, !isWhite);    //Bitboard of enemy pawns
            ulong passedFiles = 0;  //Bitboard of adjacent Files (if pawn on b file: a,b and c files are set to 1)
            ulong passedRanks = 0;  //Bitboard of every rank (greater if white, lower if black) then figure rank is set to 1
            ulong passedMask;   //Bitboard to detect if the pawn is a passed pawn
            ulong fileMask = 0x0101010101010101; //Mask of a single file - to help create passed Files

            //The plan:
            //Make a passed Files Bitboard where each bit is set to one across the whole board, that is the same or adjacent file like the looked at pawn
            //eg wPawn on File C -> complete B,C and D Files are set to 1
            //Make a passed Ranks Bitboard where each complete Rank till 6 or 1 (board goes 0-7) ahead of the looked at Pawn is set to 1
            //eg wPawn on 5th Rank -> Rank 6 and 7 (5 and 6) will bet set to one
            //Combine these Bitboards via "&" to get a passedMask Bitboard which we can again "&"-Combine with the Bitboard of enemy pawns
            //to see if it is passed

            //Shift the File Mask accordingly
            passedFiles = fileMask << Math.Max(0, square.File - 1) | fileMask << square.File | fileMask << Math.Min(7, square.File + 1);

            if (isWhite)
            {
                passedRanks = ulong.MaxValue >> 8 * (square.Rank + 1); ;
            }
            else
            {
                passedRanks = ulong.MaxValue >> 8 * (7 - (square.Rank - 1));
            }

            passedMask = passedFiles & passedRanks;

            //If the "&"-Combination of the passedMask and enemyPawns Bitboard returns zero -> it is a passed pawn
            if ((enemyPawns & passedMask) == 0)
                isPassed = true;

            return isPassed;
        }

        //Detect if a pawn is connected by another pawn
        private bool ConnectedPawnDetection(Board board, Square square, bool isWhite, ulong PawnsBB)
        {
            //Variables
            bool isConnected = false;

            if (isWhite)
            {
                if ((square.Index - 9 >= 0) && BitboardHelper.SquareIsSet(PawnsBB, new Square(square.Index - 9)))
                    isConnected = true;
                if ((square.Index - 7 >= 0) && BitboardHelper.SquareIsSet(PawnsBB, new Square(square.Index - 7)))
                    isConnected = true;
            }
            else
            {
                if ((square.Index + 9 <= 63) && BitboardHelper.SquareIsSet(PawnsBB, new Square(square.Index + 9)))
                    isConnected = true;
                if ((square.Index + 7 <= 63) && BitboardHelper.SquareIsSet(PawnsBB, new Square(square.Index + 7)))
                    isConnected = true;
            }

            return isConnected;
        }

        //Detect if Pawn is Isolated
        private bool IsolatedPawnDetection(Board board, Square square, bool isWhite, ulong PawnsBB)
        {
            //Variables
            bool isIsolated = false;
            ulong fileMask = 0x0101010101010101; //Mask of a single file - to help create passed Files
            ulong isolatedFiles = 0;  //Bitboard of adjacent Files (if pawn on b file: a,b and c files are set to 1)

            //Shift the File Mask accordingly
            isolatedFiles = fileMask << Math.Max(0, square.File - 1) | fileMask << Math.Min(7, square.File + 1);

            if((isolatedFiles & PawnsBB) == 0)
                isIsolated = true;

            return isIsolated;
        }

        //Calculate KnightBonus depending on
        //-decrease value the less enemy pawns are on the field
        //-squares to move to, that are not attacked by enemy pawns
        //-marginal bonus if defended by pawn
        private int KnightBonus(Board board, int squareIndex, bool isWhite, double midGame, double endGame)
        {
            //Variables
            int knightBonus = 0;    //the Bonus that will be returned -> reset to 0 at when called
            int moveSquareIndex;    //the square Indexes of enemy pawns
            ulong knightAtt = BitboardHelper.GetKnightAttacks(new Square(squareIndex)); //the attack Bitboard of the knight 
            ulong enemyPawnsAtt = 0;    //Bitboard where each square attacked by enemy pawn is set to one
            ulong alliedPawnsBB = board.GetPieceBitboard(PieceType.Pawn, isWhite);  //allied pawns Bitboard
            ulong enemyPawnsBB = board.GetPieceBitboard(PieceType.Pawn, !isWhite);  //enemy pawns Bitboard
            bool isProtected = ConnectedPawnDetection(board, new Square(squareIndex), isWhite, alliedPawnsBB);

            //Give penalty the less enemy pawns are on the field
            //there are max 8 enemy pawns on the field so we calc the max value and subtract accordingly
            //             5*8  pen number of enemy Pawns
            knightBonus -= 80 - 10 * BitboardHelper.GetNumberOfSetBits(enemyPawnsBB);

            //marginal Bonus if Knight is defended by a pawn
            if (isProtected)
                knightBonus += 10;

            //Mobility Bonus for each square the knight can move to that is not attacked by enemy pawn
            while (enemyPawnsBB != 0)
            {
                moveSquareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref enemyPawnsBB);
                enemyPawnsAtt |= BitboardHelper.GetPawnAttacks(new Square(moveSquareIndex), !isWhite);
            }
            //all squares the knight can move to subtracted by the number of the squares the knight can attack that are attacked by enemy pawn
            knightBonus += 10 * (BitboardHelper.GetNumberOfSetBits(knightAtt)
                            - BitboardHelper.GetNumberOfSetBits(knightAtt & enemyPawnsAtt)) * 10;

            return knightBonus;
        }

        //Calculate BishopBonus depending on
        //-Bonus for each square attacked (encourage to control long diagonals)
        //-Bishop Pair Bonus
        //-Bonus if enemy player doesnt have colored bishop (increasing in late game)
        private int BishopBonus(Board board, int squareIndex, bool isWhite, double midGame, double endGame)
        {
            int bishopBonus = 0;    //Bonus the Bishop gets
            int pawnVal = pieceValues[(int)PieceType.Pawn];
            ulong alliedBishopBB = board.GetPieceBitboard(PieceType.Bishop, isWhite);
            ulong enemyBishopBB = board.GetPieceBitboard(PieceType.Bishop, !isWhite);
            ulong BishopAttBB = BitboardHelper.GetSliderAttacks(PieceType.Bishop, new Square(squareIndex), board);    //Bitboard of BishopAttacks
            ulong alliedPawnsBB = board.GetPieceBitboard(PieceType.Pawn, isWhite);

            //Mobility Bonus calculation with punishment for getting blocked by own pawns
            bishopBonus += 8 * BitboardHelper.GetNumberOfSetBits(BishopAttBB)
                         - 8 * BitboardHelper.GetNumberOfSetBits(BishopAttBB & alliedPawnsBB);

            //Bishop Pair Bonus
            if (BitboardHelper.GetNumberOfSetBits(alliedBishopBB) == 2)
                //https://www.chessprogramming.org/Bishop_Pair - Larry Kaufmanns System
                bishopBonus = (int)(15 * midGame + 25 * endGame);

            //Color weakness
            //can only occur if there are less than 4 Bishops on the field
            //except promotion of course -> a case i dont think will occur often so im leaving it out
            if (BitboardHelper.GetNumberOfSetBits(alliedBishopBB | enemyBishopBB) < 4)
            {
                int alliedBishopCount = BitboardHelper.GetNumberOfSetBits(alliedBishopBB);
                int enemyBishopCount = BitboardHelper.GetNumberOfSetBits(enemyBishopBB);
                //Just give bonus if you have more Bishops than the enemy or when you have the same amount of bishops but they are different colour
                if (alliedBishopCount > enemyBishopCount)
                    bishopBonus += (int)(25 * endGame);
                if (alliedBishopCount == enemyBishopCount)
                {
                    //we need the square index of the enemy bishop to detect which square color it has
                    int enemyBishopSquareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref enemyBishopBB);
                    //we dont need our square index since we already have it
                    //detect if they are opposite colours and give Bonus accordingly
                    if(ColorDetection(squareIndex) != ColorDetection(enemyBishopSquareIndex))
                        bishopBonus += (int)(25 * endGame);
                }

            }
            return bishopBonus;
        }

        //Calculate RookBonus depending on
        //-increasing value the more pawns disappear
        //-Bonus if standing on open File
        //-Bonus if Rooks are connected
        //-Bonus if protecting a passed pawn in EndGame
        private int RookBonus(Board board, int squareIndex, bool isWhite, double midGame, double endGame)
        {
            //Variables
            int rookBonus = 0;  //rook Bonus that will get returned
            Square square = new Square(squareIndex);    //the square the looked at rook stands on
            ulong enemyPawnsBB = board.GetPieceBitboard(PieceType.Pawn, !isWhite);  //enemy pawns Bitboard
            ulong alliedPawnsBB = board.GetPieceBitboard(PieceType.Pawn, isWhite);  //allied pawns Bitboard
            ulong allPawnsBB = enemyPawnsBB | alliedPawnsBB;    //bitboard of all pawns
            ulong alliedRooksBB = board.GetPieceBitboard(PieceType.Rook, isWhite);  //allied Rooks Bitboard
            ulong rookAttBB = BitboardHelper.GetSliderAttacks(PieceType.Rook, square, board);  //looked at rooks attack Bitboard
            ulong fileMask = 0x0101010101010101; //Mask of a single file - to help look for open files
            ulong rookFile = 0;

            //Give bonus the less enemy pawns are on the field
            //there are max 8 enemy pawns on the field so we calc the max value and subtract accordingly
            //           5*8  pen number of enemy Pawns
            rookBonus += 80 - 10 * BitboardHelper.GetNumberOfSetBits(enemyPawnsBB);

            //give Bonus if standing on Open File
            //detect open Files or semi open files via fileMask and enemy/allied pawns bitboard
            rookFile = fileMask << square.File;
            //open File (no Paws on the file)
            if ((rookFile & allPawnsBB) == 0)
                rookBonus += 50;
            //semi-open File (only enemy pawns on the file)
            if (((rookFile & alliedPawnsBB) == 0) && ((rookFile & enemyPawnsBB) != 0))
                rookBonus += 25;

            //Give Bonus if the Rooks are connected
            //Clear the looked at rooks square since we want to "&"-Operate with the Bitboard
            BitboardHelper.ClearSquare(ref alliedRooksBB, square);
            //If alliedRooksBB & rookAtt != 0 -> the rooks can attack each other
            if ((alliedRooksBB & rookAttBB) != 0)
                rookBonus += 25;

            //Bonus if Rook protects a passed Pawn in the EndGame
            ulong temp = rookAttBB & alliedPawnsBB;
            if ((temp != 0) && (PassedPawnDetection(board, new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref temp)), isWhite) == true))
                rookBonus += (int)(50 * endGame);

            return rookBonus;
        }
        
        //Calculate QueenBonus depending on
        //-punishment for early development
        private int QueenBonus(Board board, int squareIndex, bool isWhite, double midGame, double endGame)
        {
            //Variables
            int queenBonus = 0; //bonus that the queen gets after this function
            int devScore = 0;   //the score given for development

            //Punish for early development
            devScore = 50 - board.PlyCount * 5;
            queenBonus -= Math.Min(0, devScore);

            return queenBonus;
        }
        private int KingBonus(Board board, int squareIndex, bool isWhite, double midGame, double endGame)
        {
            int kingFactor = 0;

            return kingFactor;
        }
        #endregion

        #region PieceSqaureTables
        //Index 0-7 are squares a1 - h1, 8-15 a2-h2 etc
        //I tried to make kingsside more passive and queens side more aggressive
        private static readonly int[] mgPawnTableW =
        {
            0,  0,  0,  0,  0,  0,  0,  0,
            5,  0,  5,-20,-20, 10, 10,  5,
           10, 15,-10,  5,  5,-10, -5, 10,
            5,  5, 20, 25, 25, 20,  0,  5,
            5,  0, 10, 30, 30, 10,  0,  5,
           10, 10, 20, 30, 30, 20, 10, 10,
           50, 50, 50, 50, 50, 50, 50, 50,
            0,  0,  0,  0,  0,  0,  0,  0
        };
        private static readonly int[] mgPawnTableB = mgPawnTableW.Reverse().ToArray();
        private static readonly int[] egPawnTableW =
        {
            0,  0,  0,  0,  0,  0,  0,  0,
           10, 10, 10, 10, 10, 10, 10, 10,
           10, 10, 10, 10, 10, 10, 10, 10,
           20, 20, 20, 20, 20, 20, 20, 20,
           30, 30, 30, 30, 30, 30, 30, 30,
           55, 55, 55, 50, 50, 55, 55, 55,
           75, 75, 75, 70, 70, 75, 75, 75,
            0,  0,  0,  0,  0,  0,  0,  0
        };
        private static readonly int[] egPawnTableB = egPawnTableW.Reverse().ToArray();
        private static readonly int[] mgKnightTableW =
        {
          -50,-40,-30,-30,-30,-30,-40,-50,
          -40,-20,  0,  0,  0,  0,-20,-40,
          -30,  0, 15, 10, 10, 15,  0,-30,
          -30,  0,  5, 25, 25,  5,  0,-30,
          -30, 10,  0, 25, 25,  0, 10,-30,
          -30, 20, 15, 10, 10, 15, 20,-30,
          -40,-20,  0,-10,-10,  0,-20,-40,
          -50,-40,-30,-30,-30,-30,-40,-50
        };
        private static readonly int[] mgKnightTableB = mgKnightTableW.Reverse().ToArray();
        private static readonly int[] egKnightTableW =
        {
          -50,-40,-30,-30,-30,-30,-40,-50,
          -40,-20,-10,-10,-10,-10,-20,-40,
          -30,  0, 10, 10, 10, 10,  0,-30,
          -30,  0,  5, 20, 20,  5,  0,-30,
          -30, 10, 10, 20, 20, 10, 10,-30,
          -30, 20, 20, 10, 10, 20, 20,-30,
          -40,-20,  5,-10,-10,  5,-20,-40,
          -50,-40,-30,-30,-30,-30,-40,-50
        };
        private static readonly int[] egKnightTableB = egKnightTableW.Reverse().ToArray();
        private static readonly int[] mgBishopTableW =
        {
          -30,-10,-20,-10,-10,-20,-10,-30,
          -10, 35,  0,  0,  0,  0, 35,-10,
          -10,  0,  5, 10, 10,  5,  0,-10,
          -10,  5,  5, 10, 10,  5,  5,-10,
          -10,  0, 10, 10, 10, 10,  0,-10,
          -10, 10, 10, 10, 10, 10, 10,-10,
          -10,  5,  0,  0,  0,  0,  5,-10,
          -30,-10,-20,-10,-10,-20,-10,-30
        };
        private static readonly int[] mgBishopTableB = mgBishopTableW.Reverse().ToArray();
        private static readonly int[] egBishopTableW =
        {
          -30,-10,-20,-10,-10,-20,-10,-30,
          -10, 25,  0,  0,  0,  0, 25,-10,
          -10,  0, 15, 10, 10, 15,  0,-10,
          -10,  5, 15, 20, 20, 15,  5,-10,
          -10,  0, 15, 20, 20, 15,  0,-10,
          -10, 15, 10, 10, 10, 10, 15,-10,
          -10,  5,  0,  0,  0,  0,  5,-10,
          -30,-10,-20,-10,-10,-20,-10,-30
        };
        private static readonly int[] egBishopTableB = egBishopTableW.Reverse().ToArray();
        private static readonly int[] mgRookTableW =
        {
          -20,  0, 30, 40, 40, 30,  0,-20,
           -5,  0,  0,  0,  0,  0,  0, -5,
           -5,  0,  0,  0,  0,  0,  0, -5,
           -5,  0,  0,  0,  0,  0,  0, -5,
           -5,  0,  0,  0,  0,  0,  0, -5,
           -5,  0,  0,  0,  0,  0,  0, -5,
           20, 20, 20, 20, 20, 20, 20, 20,
            0,  0,  0,  0,  0,  0,  0,  0
        };
        private static readonly int[] mgRookTableB = mgRookTableW.Reverse().ToArray();
        private static readonly int[] egRookTableW =
        {
          -40,  0,  0, 20, 20,  0,  0,-40,
            0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,
           30, 30, 30, 30, 30, 30, 30, 30,
            0,  0,  0,  0,  0,  0,  0,  0
        };
        private static readonly int[] egRookTableB = egRookTableW.Reverse().ToArray();
        private static readonly int[] mgQueenTableW =
        {
          -20,-10,-10, -5, -5,-10,-10,-20,
          -10,  0,  5,  0,  0,  0,  0,-10,
          -10,  5,  5,  5,  5,  5,  0,-10,
            0,  0,  5,  5,  5,  5,  0, -5,
           -5,  0,  5,  5,  5,  5,  0, -5,
          -10,  0,  5,  5,  5,  5,  0,-10,
          -10,  0,  0,  0,  0,  0,  0,-10,
          -20,-10,-10, -5, -5,-10,-10,-20,
        };
        private static readonly int[] mgQueenTableB = mgQueenTableW.Reverse().ToArray();
        private static readonly int[] egQueenTableW =
        {
          -20,-10,-10, -5, -5,-10,-10,-20,
          -10,  0,  5,  0,  0,  5,  0,-10,
          -10,  5,  5, 10, 10,  5,  5,-10,
            0,  5, 10, 15, 15, 10,  5, -5,
           -5,  5, 10, 15, 15, 10,  5, -5,
          -10,  0,  5, 10, 10,  5,  0,-10,
          -10,  0,  0,  0,  0,  0,  0,-10,
          -20,-10,-10, -5, -5,-10,-10,-20,
        };
        private static readonly int[] egQueenTableB = egQueenTableW.Reverse().ToArray();
        private static readonly int[] mgKingTableW =
        {
           20, 30, 10,  0,  0, 10, 30, 20,
           20, 20,  0,  0,  0,  0, 20, 20,
          -10,-20,-20,-20,-20,-20,-20,-10,
          -20,-30,-30,-40,-40,-30,-30,-20,
          -30,-40,-40,-50,-50,-40,-40,-30,
          -30,-40,-40,-50,-50,-40,-40,-30,
          -30,-40,-40,-50,-50,-40,-40,-30,
          -30,-40,-40,-50,-50,-40,-40,-30
        };
        private static readonly int[] mgKingTableB = mgKingTableW.Reverse().ToArray();
        private static readonly int[] egKingTableW =
        {
          -50,-30,-30,-30,-30,-30,-30,-50,
          -30,-30,  0,  0,  0,  0,-30,-30,
          -30, 10, 20, 35, 35, 20, 10,-30,
          -30, 10, 35, 45, 45, 35, 10,-30,
          -30, 10, 35, 45, 45, 35, 10,-30,
          -30, 10, 20, 35, 35, 20, 10,-30,
          -30,-20,-10,  0,  0,-10,-20,-30,
          -50,-40,-30,-20,-20,-30,-40,-50
        };
        private static readonly int[] egKingTableB = egKingTableW.Reverse().ToArray();
        #endregion

    }

}

