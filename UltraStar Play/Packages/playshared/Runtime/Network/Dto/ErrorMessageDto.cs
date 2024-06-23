public class ErrorMessageDto : JsonSerializable
{
    public string ErrorMessage { get; set; }

    public ErrorMessageDto()
    {
    }

    public ErrorMessageDto(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}
