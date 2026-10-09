namespace Tasky.Api.Dtos;

/// <summary>Body of POST /api/sources/{key}/execute. Inputs are the values the user typed.</summary>
public class ExecuteRequestDto
{
    public Dictionary<string, string>? Inputs { get; set; }
}
