using Chess_Challenge.src.TRACER_V5;
using ChessChallenge.API;
using System;
using System.Diagnostics;

public class TRACER_V5 : IChessBot
{
#if SHOW_INFO
    public BotInfo Info()
    {
        return new BotInfo(lastDepth,
                            currEval,
                            lastMove,
                            nodesSearched,
                            nodesPruned,
                            ttable.CalculateFillPercentage(),
                            nogoodmoves
                            );
    }
#endif


    //Constant Variables
    //Max Search depth
    private int MAX_DEPTH = 50;
    //Score for Mate
    private const int MATE = 30000;
    //Size of TTable in MB
    private const int TTABLE_SIZE_MB = 64;

    //Global Variables
    //best Move that will be played
    private Move bestMove;
    //best Move from the previous Iteration 
    private Move bestMovePrevIteration;
    //Depth we are currently searching 
    private int currentDepth;
    //Time the engine has to make a turn
    private int timeForTurn;



    //------------------------- #DEBUG -------------------------
    private string lastMove;
    private int lastDepth;
    private int currEval;
    private int nodesSearched;
    private int nodesPruned;
    private int nogoodmoves;
    //------------------------- #DEBUGEND ----------------------


    //Fill used classes
    Evaluation eval = new Evaluation();
    TranspositionTable ttable = new TranspositionTable(TTABLE_SIZE_MB);
    MoveOrdering order = new MoveOrdering();

    public Move Think(Board board, Timer timer)
    {
        //Reset bestMove at start of think method
        bestMove = Move.NullMove;
        //Reset bestMove previous Iteration 
        bestMovePrevIteration = Move.NullMove;

        //------------------------- #DEBUG ------------------------- 
        // Reset debug variables
        nodesSearched = 0;
        nodesPruned = 0;
        //------------------------- #DEBUGEND ----------------------


        timeForTurn = timer.MillisecondsRemaining / 60; //Make a function to determine if the Search time has been reached TODO


        for (currentDepth = 1; currentDepth <= MAX_DEPTH; currentDepth++)
        {

            Search(board, currentDepth, -MATE, MATE, 0);

            //Check if time for turn is up
            if (timer.MillisecondsElapsedThisTurn > timeForTurn)
            {
                break;
            }

            //update variables for UI
            lastDepth = currentDepth;
            lastMove = $"{bestMove}";
        }

        // If by all means we dont find a good move
        // just play the first legal move
        // we hopefully dont get here
        if (bestMove.IsNull)
        {
            nogoodmoves++;
            Move[] moves = board.GetLegalMoves();
            return moves[0];
        }

        return bestMove;
    }

    private int Search(Board board, int depth, int alpha, int beta, int ply)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        // Variables
        int score;
        ulong key = board.ZobristKey;


        // only look for draws or checkmates in future moves (when ply is larger than zero)
        if (ply > 0)
        {
            // If the position is draw then return 0
            if (board.IsDraw())
                return 0;

            // if a move is Checkmate return best possible value
            // encourage earlier checkmates by adding ply
            if (board.IsInCheckmate())
                return -MATE + ply;
        }


        //try to lookup current position in the transposition table.
        //if the current position has already been searched to at least an equal depth
        //to the search we're doing now, we can just use the recorded evaluation
        int ttVal = ttable.LookupEvaluation(key, depth, ply, alpha, beta);
        if (ttVal != TranspositionTable.LOOKUPFAILED)
        {
            if (ply == 0)
            {
                bestMove = ttable.TryGetStoredMove(key);
                score = ttable.entries[ttable.Index(key)].value;
            }
            return ttVal;
        }

        // when root is reached, evaluate the position
        if (depth == 0)
        {
            score = QSearch(board, alpha, beta);
            return score;
        }

        // Recursive call of Search function to find the best Move
        Move[] moves = board.GetLegalMoves();

        //Set bestMove prev Iteration for move ordering
        bestMovePrevIteration = ply == 0 ? bestMove : ttable.TryGetStoredMove(key);

        // Order moves to increase AB-Pruning efficiency
        order.OrderMoves(moves, board, bestMovePrevIteration);

        //evaluation bound stored in ttable
        int evaluationBound = TranspositionTable.UPPERBOUND;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -Search(board, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);

            // Move was *too* good, opponent will choose a different move earlier on to avoid this position.
            // Alpha-Beta Pruning
            if (score >= beta)
            {
                //------------------------- #DEBUG -------------------------
                nodesPruned++;
                //------------------------- #DEBUGEND ----------------------

                //Store evaluation in transposition Table
                ttable.StoreEvaluation(key, depth, ply, beta, TranspositionTable.LOWERBOUND, move);

                return beta;
            }

            //new bestMove was found
            if (score > alpha)
            {
                //set evaluationBound to Exact
                evaluationBound = TranspositionTable.EXACT;

                alpha = score;
                if (ply == 0)
                {
                    bestMove = move;
                    currEval = score;
                }
            }
        }

        //Store evaluation in transposition Table
        ttable.StoreEvaluation(key, depth, ply, alpha, evaluationBound, bestMove);

        return alpha;
    }


    private int QSearch(Board board, int alpha, int beta)
    {
        //------------------------- #DEBUG -------------------------
        nodesSearched++;
        //------------------------- #DEBUGEND ----------------------

        //Evaluate the position
        int standPat = eval.EvaluatePosition(board) * (board.IsWhiteToMove ? 1 : -1);

        //Check for beta cutoff
        if (standPat >= beta)
            return beta;

        //Delta-Pruning 
        //chessprogrammin.org/Delta_Pruning
        const int BIG_DELTA = 500; //High value near Queen
        if (standPat < alpha - BIG_DELTA)
        {
            //------------------------- #DEBUG -------------------------
            nodesPruned++;
            //------------------------- #DEBUGEND ----------------------
            return alpha;
        }

        //Update alpha if necessary
        if (alpha < standPat)
            alpha = standPat;

        //Find all capture moves and order them
        Move[] qSearchMoves = board.GetLegalMoves(true);

        //Order Moves   
        order.OrderMoves(qSearchMoves, board, Move.NullMove);

        //foreach capture move look further into the position
        foreach (Move move in qSearchMoves)
        {
            board.MakeMove(move);
            int score = -QSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }
}