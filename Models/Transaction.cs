using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain_1.Models
{
    public class Transaction
    {
        public string Id { get; set; }
        public string From { get; set; }
        public decimal Amount { get; set; }
        public string To { get; set; }

        public DateTime TimeStamp { get; set; }

        public Transaction(string from, string to, decimal amount)
        {
            Id = Guid.NewGuid().ToString();
            From = from;
            To = to;
            Amount = amount;
            TimeStamp = DateTime.UtcNow;
        }

        //Рядок для хешування — тільки дані, через табуляцію
        public string ToRawString()
        {
            return $"{Id}\t{From}\t{To}\t{Amount}\t{TimeStamp}";
        }

        //Рядок для виводу людині
        public override string ToString()
        {
            return $"Transaction ID: {Id}, From: {From}, To: {To}, Amount: {Amount}, Timestamp: {TimeStamp}";
        }
    }
}
