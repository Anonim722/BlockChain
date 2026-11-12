using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using BlockChain_1.Models;

namespace BlockChain_1
{
    public class HashingService
    {
        public string ComputeHash(Block block)
        {
            var transactionRawData = string.Concat(block.Transactions.Select(t => t.ToRawString()));

            var blockData = $"{block.Index}{block.TimeStamp}{transactionRawData}{block.PreviousHash}{block.Nonce}{block.DifficultyAtMining}";
            return ComputeHash(blockData);
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
