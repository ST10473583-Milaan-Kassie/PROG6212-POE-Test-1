namespace RaceDay.Api.DTOs.Responses;

//the shape every message and error comes back in, the endpoints build it as { message } or { message, errors }
//this class is mainly here so swagger can show that shape on each response
public class MessageResponse
{
    public string Message { get; set; } = string.Empty;

    //only there when fields fail validation, each field name with its errors
    public Dictionary<string, string[]>? Errors { get; set; }
}