using Tasky.Api.Dtos;

namespace Tasky.Api.Services;

public interface IProxyService
{
    Task<ExecuteResponseDto> ExecuteAsync(string key, ExecuteRequestDto request, string? clientHost, CancellationToken ct);
}
