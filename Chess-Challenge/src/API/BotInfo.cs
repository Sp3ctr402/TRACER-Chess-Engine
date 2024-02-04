namespace ChessChallenge.API
{
    public struct BotInfo
    {

        public BotInfo(int depth = -1, int evaluation = 0, string move = "", int nodesSearched = 0)
        {
            Depth = depth;
            Evaluation = evaluation;
            Move = move;
            Positions = nodesSearched;
        }

        public int Depth = -1;
        public int Evaluation = 0;
        public string Move = "";
        public int Positions = 0;

        // Everything else you want your Engine to report

        public bool IsValid => Depth >= 0;
    }
}
