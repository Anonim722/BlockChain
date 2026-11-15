
namespace BlockChain_1.Models
{
    public class RepairResult
    {
        public int BlocksRemined { get; set; }
        public long TotalAttempts { get; set; }
        public double DurationSeconds { get; set; }
        public bool IsValidAfter { get; set; }
    }
}
