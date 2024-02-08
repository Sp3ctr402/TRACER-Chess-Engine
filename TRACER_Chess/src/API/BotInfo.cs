namespace ChessChallenge.API
{
    public struct BotInfo
    {

        public BotInfo(int depth = -1, 
                       int evaluation = 0, 
                       string move = "", 
                       int nodesSearched = 0, 
                       int nodesPruned = 0,
                       double fillPercentage = 0.0,
                       int nogoodmoves = 0)
        {
            Depth = depth;
            Evaluation = evaluation;
            Move = move;
            Positions = nodesSearched;
            PositionsPruned = nodesPruned;
            TTableFilled = fillPercentage;
            noGoodMove = nogoodmoves;
        }

        public int Depth = -1;
        public int Evaluation = 0;
        public string Move = "";
        public int Positions = 0;
        public int PositionsPruned = 0;
        public double TTableFilled = 0.0;
        public int noGoodMove = 0;

        // Everything else you want your Engine to report

        public bool IsValid => Depth >= 0;
    }
}
