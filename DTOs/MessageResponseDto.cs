namespace EconomyBackPortifolio.DTOs
{
    public class MessageResponseDto
    {
        public string Message { get; set; } = string.Empty;

        public MessageResponseDto() { }

        public MessageResponseDto(string message)
        {
            Message = message;
        }
    }
}
