namespace BlockChain_1.Models
{
    //Результат однієї перевірки аудиту
    public class AuditResult
    {
        public string Name { get; }
        public bool Passed { get; }
        public string Details { get; }

        public AuditResult(string name, bool passed, string details)
        {
            Name = name;
            Passed = passed;
            Details = details;
        }
    }
}