using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockChain_1.Models;

namespace BlockChain_1
{
    public class Block
    {
        //Порядковий номер блоку в ланцюжку
        public int Index { get; set; }
        //Час створення блоку
        public DateTime TimeStamp { get; set; }
        //Дані, які зберігаються в блоці
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        //Автор запису


        //Хеш попереднього блоку
        public string PreviousHash { get; set; }
        //Хеш поточного блоку
        public string Hash { get; set; }

        //Число, яке підбирається для знаходження хешу (Proof of Work)
        public int Nonce { get; set; }

        //Скільки часу зайняв майнінг блоку (в секундах)
        public double MiningDuration { get; set; }

        //Складність на момент створення блоку
        public int DifficultyAtMining { get; set; }
        //Кількість спроб (перебраних Nonce)
        public long Attempts { get; set; }
    }
}