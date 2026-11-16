using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain_1.Models
{
    //Результат спроби створити блок
    public class BlockCreationResult
    {
        //Усі причини відмови (порожньо = все добре)
        public List<string> Errors { get; } = new List<string>();

        //Створений блок (null, якщо відмовлено)
        public Block? Block { get; set; }

        public bool Success => Errors.Count == 0;
    }
}