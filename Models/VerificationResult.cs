namespace BlockChain_1.Models
{
    //Результат перевірки однієї транзакції
    public class VerificationResult
    {
        public string TransactionId { get; set; } = "";
        public bool IsValid { get; set; }
        public string Reason { get; set; } = "";
        public string Location { get; set; } = "";      //де транзакція зараз
        public DateTime CheckedAt { get; set; } = DateTime.Now;

        //"Valid" / "Invalid" — як вимагає завдання
        public string Status => IsValid ? "Valid" : "Invalid";
    }
}