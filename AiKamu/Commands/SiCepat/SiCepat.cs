using AiKamu.Common;
using Discord.WebSocket;

namespace AiKamu.Commands.SiCepat;

public class SiCepat : ICommand
{
    private readonly ISiCepatApi _siCepatApi;

    public SiCepat(ISiCepatApi siCepatApi)
    {
        _siCepatApi = siCepatApi;
    }

    public bool IsPrivateResponse(CommandArgs commandArgs)
    {
        return true;
    }

    public async Task<IResponse> GetResponseAsync(DiscordSocketClient discordSocketClient, CommandArgs commandArgs)
    {
        var trackingNumber = commandArgs.Args[SlashCommandConstants.OptionNameTrackingNumber] as string;

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            return new TextResponse(true, $"Tracking number can't be empty");
        }

        var response = await _siCepatApi.CheckAwbAsync(trackingNumber);
        var message = await GetMessage(response?.Sicepat?.Result);
        return new TextResponse(true, message);
    }

    private static async Task<string> GetMessage(Result? result)
    {
        if (result == null)
        {
            return "Can't get tracking details from SiCepat";
        }

        if (result?.Delivered ?? false)
        {
            return await Task.Run(() =>
            $"""
            {result.WaybillNumber}
            From {result.Sender} - {result.SenderAddress} at {result.SendDate} has been Delivered.
            {result.PODReceiver} : {result.PODReceiverTime}
            """
            );
        }

        return await Task.Run(() =>
            $"""
            {result!.WaybillNumber}
            From {result.Sender} - {result.SenderAddress} at {result.SendDate} 
            Current status
            {result!.LastStatus?.DateTime}: {result!.LastStatus?.ReceiverName ?? result!.LastStatus?.City}
            """   
        );
    }
}
