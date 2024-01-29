//TRACER_V1 NegaMax(+) | AlphaBeta(+) | Material Balance(+)

//TRACER_V2 PieceSquareTable(+) | Endgame Transition(+)                     +/- 0 Elo

//TRACER_V3 QSearch(+) | DeltaPruning(+) | MoveOrdering(+)                  +250 +/- 35 Elo

//TRACER_V4 Iterative Deepening(+) | MoveOrdering improvements(+)           +231 +/- 36 Elo | Lichess implemented (+)
//Best Move prev Iteration as first move to look at

//TRACER_V4.1 | Evaluation improvements(-)                                  +XXX +/- XX Elo
//Get additional figure Score for specific type
//Pawn:  (-)| Pawn Stucture | passed pawns | Pawn center    
//+XXX +/- XX Elo
//Knight:(-)| decrease Value the more pawns disappear | Mobility bonus for squares not attacked by pawns | marginal bonus for defended by pawn  
//+XXX +/- XX Elo
//Bishop:(-)| Mobility | Bishop Pair Bonus | Color weakness (bonus for colored bishop that the opponent doesnt have)
//+XXX +/- XX Elo
//Rook:  (-)| increase value the more pawns disappear | Open File bonus | Small Bonus doubled rook | Minimal Bonus for rook and queen on same file
//+XXX +/- XX Elo
//Queen: (-)| Punishment for early development
//+XXX +/- XX Elo
//King:  (-)| King safety in Middle Game | King centralization in End Game | Penalty for no pawn shield | Penalty for not using Castle right
//+XXX +/- XX Elo