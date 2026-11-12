using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain_1
{
    public class BlockChainDisplayService
    {
        public void DisplayChain(BlockChain blockChain)
        {
            Console.WriteLine("Blockchain:");
            foreach (var block in blockChain.Chain)
            {
                DisplayBlock(block);
            }


        }
        public void DisplayBlock(Block block)
        {
            Console.WriteLine($"Index: {block.Index}");
            Console.WriteLine($"Timestamp: {block.TimeStamp}");
            Console.WriteLine($"Nonce: {block.Nonce}");
            Console.WriteLine($"Mining Duration: {block.MiningDuration} seconds");
            Console.WriteLine($"Transactions ({block.Transactions.Count}):");
            foreach (var tx in block.Transactions)
            {
                Console.WriteLine($"   - {tx.ToRawString()}");
            }
            Console.WriteLine($"Previous Hash: {block.PreviousHash}");
            Console.WriteLine($"Hash: {block.Hash}");
            Console.WriteLine(new string('-', 50));
        }
        public void PrintDifficultyHistory(BlockChain blockChain)
        {
            Console.WriteLine("Історія складності:");
            Console.WriteLine($"{"Index",-6} | {"Difficulty",-10} | {"Attempts",-12} | {"Duration (s)",-12} | Hash");
            Console.WriteLine(new string('-', 80));

            foreach (var block in blockChain.Chain)
            {
                Console.WriteLine(
                    $"{block.Index,-6} | {block.DifficultyAtMining,-10} | {block.Attempts,-12} | " +
                    $"{block.MiningDuration,-12:F3} | {block.Hash.Substring(0, 16)}...");
            }
        }
    }
}

