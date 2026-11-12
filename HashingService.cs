using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace BlockChainP32
{
    public class HashingService
    {
        public string ComputeHash(Block block)
        {
            var blockData = $"{block.Index}{block.TimeStamp}{block.Data}{block.Author}{block.PreviousHash}{block.Nonce}{block.DifficultyAtMining}";
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
