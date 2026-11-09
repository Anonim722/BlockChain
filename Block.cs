using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP32
{
    public class Block
    {
        //Порядковий номер блоку в ланцюжку
        public int Index { get; set; }
        //Час створення блоку
        public DateTime TimeStamp { get; set; }
        //Дані, які зберігаються в блоці
        public string Data { get; set; }
        //Автор запису
        public string Author { get; set; }

        //Хеш попереднього блоку
        public string PreviousHash { get; set; }
        //Хеш поточного блоку
        public string Hash { get; set; }
    }
}