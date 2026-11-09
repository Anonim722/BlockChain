using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP32
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
            Console.WriteLine($"Data: {block.Data}");
            Console.WriteLine($"Author: {block.Author}");
            Console.WriteLine($"Previous Hash: {block.PreviousHash}");
            Console.WriteLine($"Hash: {block.Hash}");
            Console.WriteLine(new string('-', 50));
        }
    }
}
