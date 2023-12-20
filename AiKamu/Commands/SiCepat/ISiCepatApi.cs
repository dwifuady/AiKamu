namespace AiKamu.Commands.SiCepat;

using Refit;

public interface ISiCepatApi
{
    [Get("/public/check-awb/{id}")]
    Task<SiCepatDto> CheckAwbAsync(string id);
}