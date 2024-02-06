using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Chess_Challenge.src.TRACER
{
    public class TranspositionTable
    {
        //inspiration from https://web.archive.org/web/20071031100051/http://www.brucemo.com/compchess/programming/hashing.htm

        //Variables
        public const int LOOKUPFAILED = -1;
        //The Value for this position is the exact value
        public const int EXACT = 0;
        // A move was found during the search that was too good, meaning the opponent will play a different move earlier on,
		// not allowing the position where this move was available to be reached. Because the search cuts off at
		// this point (beta cut-off), an even better move may exist. This means that the evaluation for the
		// position could be even higher, making the stored value the lower bound of the actual value.
        public const int LOWERBOUND = 1;
        // No move during the search resulted in a position that was better than the current player could get from playing a
		// different move in an earlier position (i.e eval was <= alpha for all moves in the position).
		// Due to the way alpha-beta search works, the value we get here won't be the exact evaluation of the position,
		// but rather the upper bound of the evaluation. This means that the evaluation is, at most, equal to this value.
		public const int UPPERBOUND = 2;

        //Array of entries
        public Entry[] entries;

        //count to calculate Index of an entry
        public readonly ulong count;


        //Function to calculate FillPercentage (DEBUG)
        public double CalculateFillPercentage()
        {
            // count occupied entries
            int occupiedEntries = 0;
            foreach (var entry in entries)
            {
                if (entry.key != 0)
                {
                    occupiedEntries++;
                }
            }

            // calculate filled percentage
            double fillPercentage = (double)occupiedEntries / entries.Length * 100;
            // round to two decimal 
            fillPercentage = Math.Round(fillPercentage, 2);
            
            return fillPercentage;
        }


        //Constructor
        public TranspositionTable(int sizeMB)
        {
            //the size of a single entry in bytes
            int ttEntrySizeBytes = System.Runtime.InteropServices.Marshal.SizeOf<TranspositionTable.Entry>();
            //the desired table size in bytes
            int desiredTableSizeBytes = sizeMB * 1024 * 1024;
            //the number of entries in the TranspositionTable
            int numEntries = desiredTableSizeBytes / ttEntrySizeBytes;

            count = (ulong)(numEntries);
            entries = new Entry[numEntries];
        }

        //Function to clear all entries
        public void Clear()
        {
            for(int i = 0; i < entries.Length; i++)
            {
                entries[i] = new Entry();
            }
        }

        //Function to get the Index of an entry
        public ulong Index(ulong key)
        {
            return key % count; 
        }

        //Function to get the stored Move from an entry
        public Move TryGetStoredMove(ulong key)
        {
            return entries[Index(key)].move;            
        }

        //Function to try to lookup eval
        public bool TryLookUpEvaluation(int depth, int plyFromRoot, int alpha, int beta, out int eval)
        {
            eval = 0;
            return false;
        }

        //Function to actually lookup the evaluation
        public int LookupEvaluation(ulong key,int depth, int plyFromRoot, int alpha, int beta)
        {
            //look at the entry of the index
            Entry entry = entries[Index(key)];

            //if the key is found then replace corresponding score
            if(entry.key == key)
            {
                //Only use stored evaluation if it has been searched to at least the same depth as would be searched now
                if(entry.depth >= depth)
                {
                    //exact value has been stored so return it
                    if(entry.nodeType == EXACT)
                    {
                        return entry.value;
                    }
                    //upperbound value has been stored. If its less than alpha we dont need to
                    //search the moves in this position as they wont interest us
                    //otherwise we have to search to find the exact value
                    if(entry.nodeType == UPPERBOUND && entry.value <= alpha)
                    {
                        return entry.value;
                    }
                    //lowerbound value has been stored. Only return if Beta-Cutoff
                    if(entry.nodeType == LOWERBOUND && entry.value >= beta)
                    {
                        return entry.value;
                    }
                }
            }
            //else return that the lookup failed
            return LOOKUPFAILED;
        }

        //function to store the evaluation
        public void StoreEvaluation(ulong key ,int depth, int plyFromRoot, int eval, int nodeType, Move move)
        {
            //index of the entry
            ulong index = Index(key);

            //write a new entry
            Entry entry = new Entry(key , eval, (byte)depth, (byte)nodeType, move);
            entries[Index(key)] = entry;
        }

        //Function to find an entry with the index calculated
        public Entry GetEntry(ulong zobristKey)
        {
            return entries[zobristKey % (ulong)entries.Length];
        }

        //structure of an entry
        public struct Entry
        {
            //Variables of an entry
            public readonly ulong key;
            public readonly int value;
            public readonly Move move;
            public readonly byte depth;
            public readonly byte nodeType;

            //contructor of an entry
            public Entry(ulong key, int value, byte depth, byte nodeType, Move move)
            {
                this.key = key;
                this.value = value;
                this.depth = depth;
                this.nodeType = nodeType;
                this.move = move;
            }

            public static int GetSize()
            {
                return System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
            }
        }
    }
}
