using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

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
        //--------------------------------------------{ .,  P,   K,   B,   R,   Q,      K }
        private static readonly int[] mgPieceValues = { 0, 80, 305, 333, 460, 905, 100000 };
        private static readonly int[] thPieceValues = { 0, 90, 305, 333, 485, 905, 100000 };
        private static readonly int[] egPieceValues = { 0, 100, 305, 333, 515, 905, 100000 };
        private int[] pieceValues;


        // Helper Variables
        // File Mask to shift
        private ulong FileMask = 0x0101010101010101;


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
            int numberOfPawns = BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Pawn, true) |
                                                                  board.GetPieceBitboard(PieceType.Pawn, false));
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
                    whiteMaterial += GetFigureScore(board, squareIndex, piece, pieceValues, midGame, endGame, numberOfPawns);
                else
                    blackMaterial += GetFigureScore(board, squareIndex, piece, pieceValues, midGame, endGame, numberOfPawns);
            }


            //return Material balance
            return whiteMaterial - blackMaterial;
        }


        // Figure to get the value of a single piece regarding all parameters 
        public int GetFigureScore(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame, int noP)
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
                    figureScore += KnightEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame, noP);
                    break;


                case PieceType.Bishop:
                    figureScore += BishopEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame);
                    break;


                case PieceType.Rook:
                    figureScore += RookEvaluation(board, squareIndex, piece, pieceValues, midGame, endGame, noP);
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
            double pawnScore = 0;
            int index;
            Square square = new Square(squareIndex);
            bool isIsolated = IsolatedPawnDetection(board, square, piece);
            bool isConnected = !IsolatedPawnDetection(board, square, piece);
            bool isPassed = PassedPawnDetection(board, squareIndex, piece);
            bool isDoubled = DoubledPawnDetection(board, squareIndex, piece);


            // get the current score of a pawn
            pawnScore += pieceValues[(int)piece.PieceType];


            // get index of rank for black and white pieces 
            index = (piece.IsWhite ? square.Rank - 1 : 7 - square.Rank - 1);


            // get multiplier of pawn based on rank, file & gamePhase 
            // value multiplier by rank and file when pawn is not passed
            if (!isPassed)
            {
                // Evaluation of a pawn on a or h file
                if (square.File == 0 || square.File == 7)
                {
                    // Get multipliers for gamephase 
                    double[] midGameMultipliers =
                        {0.9, 0.9, 0.9, 0.97, 1.06, 1.21};
                    double[] endGameMultipliers =
                        {1.20, 1.20, 1.25, 1.33, 1.45, 1.55};


                    pawnScore *= (midGameMultipliers[index] * midGame + endGameMultipliers[index] * endGame);
                }

                //Evaluation of a pawn on b or g file
                else if (square.File == 1 || square.File == 6)
                {
                    // Get multipliers for gamephase 
                    double[] midGameMultipliers =
                        {0.95, 0.95, 0.95, 1.03, 1.12, 1.31};
                    double[] endGameMultipliers =
                        {1.05, 1.05, 1.10, 1.17, 1.29, 1.44};


                    pawnScore *= (midGameMultipliers[index] * midGame + endGameMultipliers[index] * endGame);
                }

                //Evaluation of a pawn on c or f file
                else if (square.File == 2 || square.File == 5)
                {
                    // Get multipliers for gamephase 
                    double[] midGameMultipliers =
                        {1.05, 1.05, 1.10, 1.17, 1.25, 1.41};
                    double[] endGameMultipliers =
                        {0.95, 0.95, 1.00, 1.07, 1.16, 1.33};


                    pawnScore *= (midGameMultipliers[index] * midGame + endGameMultipliers[index] * endGame);
                }

                //Evaluation of a pawn on d or e file
                else
                {
                    // Get multipliers for gamephase 
                    double[] midGameMultipliers =
                        {1.10, 1.15, 1.20, 1.27, 1.40, 1.51};
                    double[] endGameMultipliers =
                        {0.90, 0.90, 0.95, 1.00, 1.05, 1.22};


                    pawnScore *= (midGameMultipliers[index] * midGame + endGameMultipliers[index] * endGame);
                }
            }


            // check for pawn structures
            // First check for isolated pawns
            if (isIsolated)
            {
                // Depending on rank give a bonus or a penalty
                // an isolated pawn on your side is probably a weakness
                // while an isolated pawn on the enemy side can be an asset
                double[] isolatedMultipliers =
                    {0.80, 0.90, 1.05, 1.30, 2.1, 2.75};

                pawnScore *= isolatedMultipliers[index];
            }


            // check for pawn connections that are not passed pawns
            // a pawn is connected when its not isolated 
            if (isConnected && !isPassed)
            {
                // Depending on rank give a bonus or penalty 
                // connected pawns are alway strong but stronger in the opposing half
                double[] connectedMultiplier =
                    {1.00 ,1.05, 1.15, 1.35, 1.55, 1.75};

                pawnScore *= connectedMultiplier[index];
            }


            // check for passed pawns that are not connected
            if (!isConnected && isPassed)
            {
                // Depending on rank give a bonus or penalty 
                // passed pawns are stronger the deeper they are in enemy territory
                double[] passedMultiplier =
                    {1.00, 1.05, 1.30, 1.55, 1.85, 2.15};

                pawnScore *= passedMultiplier[index];
            }


            // check for connected passedpawns
            if (isConnected && isPassed)
            {
                // Depending on rank give a bonus or penalty 
                // passed pawns are stronger the deeper they are in enemy territory
                double[] passedMultiplier =
                    {1.00, 1.25, 1.55, 2.3, 3.5, 4.25};

                pawnScore *= passedMultiplier[index];
            }


            //check for double pawns
            if (isDoubled)
            {
                // a doubled pawn is always a weakness
                pawnScore *= 0.66;
            }


            return (int)pawnScore;
        }


        // Evaluate Knights
        // -Outposts
        // -Mobility minus squares attacked by enemy pawns
        // -decreasing value as pawns disappear
        private int KnightEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame, int noP)
        {
            double knightScore = 0;
            Square square = new Square(squareIndex);
            bool isOpponentHalf = piece.IsWhite ? squareIndex > 31 : squareIndex < 32;
            

            // get the current score of a knight
            knightScore += pieceValues[(int)piece.PieceType];

            // value of knights decreases as more pawns disappear
            // maximum of 16 Pawns will lead to no penalty
            knightScore -= (1 - noP / 16) * 35;

            // mobility bonus 
            // a bonus for each square the knight attacks
            // minus each square attacked by enemy pawn
            int numberOfSquares = 0;
            ulong knightAttacks = BitboardHelper.GetKnightAttacks(square);

            while (knightAttacks != 0) 
            {
                Square sq = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref knightAttacks));
                if(!board.SquareIsAttackedByOpponentPawn(sq))
                    numberOfSquares++;
            }     
            knightScore += numberOfSquares * 8;

            // Outpost bonus
            if(isOpponentHalf)
            {

            }


            return (int)knightScore;
        }


        // Evaluate Bishops
        private int BishopEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        {
            double bishopScore = 0;

            // get the current score of a bishop
            bishopScore += pieceValues[(int)piece.PieceType];

            return (int)bishopScore;
        }


        // Evaluate Rooks
        private int RookEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame, int noP)
        {
            double rookScore = 0;

            // get the current score of a rook
            rookScore += pieceValues[(int)piece.PieceType];

            return (int)rookScore;
        }


        // Evaluate Queens
        private int QueenEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        {
            double queenScore = 0;

            // get the current score of a queen
            queenScore += pieceValues[(int)piece.PieceType];

            return (int)queenScore;
        }


        // Evaluate Kings
        private int KingEvaluation(Board board, int squareIndex, Piece piece, int[] pieceValues, double midGame, double endGame)
        {
            double kingScore = 0;

            // get the current score of a king
            kingScore += pieceValues[(int)piece.PieceType];

            return (int)kingScore;
        }
        #endregion


        #region HelperFunctions
        // Function to check if a pawn is isolated
        private bool IsolatedPawnDetection(Board board, Square square, Piece piece)
        {
            bool isIsolated = false;
            ulong alliedPawnsBB = board.GetPieceBitboard(PieceType.Pawn, piece.IsWhite);

            // check if the pawn is isolated by checking on pawns on adjacent files
            ulong adjacentFileL = FileMask << Math.Min(0, square.File - 1);
            ulong adjacentFileR = FileMask >> Math.Max(7, square.File + 1);

            ulong adjacentFiles = adjacentFileL | adjacentFileR;


            // if the AND-Operation of adjacentFiled and the alliedPawnsBB 
            // gives a value of 0 than the pawn is isolated            
            if ((adjacentFiles & alliedPawnsBB) == 0)
                isIsolated = true;


            return isIsolated;
        }

        // Function to check if a pawn is passed
        private bool PassedPawnDetection(Board board, int squareIndex, Piece piece)
        {
            bool isPassed = false;
            ulong enemyPawnsBB = board.GetPieceBitboard(PieceType.Pawn, !piece.IsWhite);
            Square square = new Square(squareIndex);
            ulong passedFilesMask = FileMask << Math.Max(0, square.File - 1) |
                                    FileMask << square.File |
                                    FileMask << Math.Min(7, square.File + 1);
            ulong passedRanksMask = piece.IsWhite ? ulong.MaxValue << 8 * (square.Rank + 1) : ulong.MaxValue >> 8 * (8 - square.Rank);

            ulong passedMask = passedFilesMask & passedRanksMask;


            // if the &-Operation between the passedMask and the enemyPawnsBB
            // equals 0, the pawn is a passed pawn
            if ((passedMask & enemyPawnsBB) == 0)
                isPassed = true;

            return isPassed;
        }

        // Function to detect doubles pawns
        private bool DoubledPawnDetection(Board board, int squareIndex, Piece piece)
        {
            bool isDoubled = false;
            Square square = new Square(squareIndex);
            ulong pawnFile = FileMask << square.File;
            ulong alliedPawnsBB = board.GetPieceBitboard(PieceType.Pawn, piece.IsWhite);

            // if the &-Operation of the file BitMask and the alliedPawnsBB is != 0, then its a doubled pawn
            if ((alliedPawnsBB & pawnFile) != 0)
                isDoubled = true;

            return isDoubled;
        }

        #endregion


    }
}

