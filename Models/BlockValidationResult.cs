
namespace BlockChain_1.Models
{
    public class BlockValidationResult
    {
        public int BlockIndex { get; set; } = -1;
        public ValidationError Error { get; set; } = ValidationError.None;
        public string Message { get; set; } = "Ланцюг валідний";

        //Обчислювана властивість: валідний, якщо помилки немає
        public bool IsValid => Error == ValidationError.None;
    }
}
