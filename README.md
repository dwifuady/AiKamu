# AiKamu
## Prerequisites
- .NET 8.0
- [Create Discord Bot here](https://discord.com/developers/applications)

## Running Locally
### Config
To run the project locally, you can store the config using user-secrets. 
#### Using Visual Studio
- Right click on the AiKamu Project, click Manage User Secrets
- The value should look like this
```json
{
  "DiscordBotConfig:Token": "YOUR_DISCORD_TOKEN",
  "DiscordBotConfig:BotManagementServerGuild": "YOUR_SERVER_ID",
}
```

#### Using CLI
- navigate to AiKamu project directory 
- run below command  
`dotnet user-secrets set "DiscordBotConfig:Token" "YOUR_TOKEN"`
`dotnet user-secrets set "DiscordBotConfig:BotManagementServerGuild" "YOUR_SERVER_ID"`

Alternatively, you can put the config on the appsettings.json

Run the project using F5 if you are using Visual Studio, or execute `dotnet run`

## Command 
### Command Management
This command should only be available on your server. This is to add/remove guild command to other server, since it's faster than Global Command.  
You need to put your server guild id to the `DiscordBotConfig:BotManagementServerGuild` config.  
You can get the ID by right clicking on your server, and select Copy Server ID


### OpenAi
- Register an account to the openai platform
- Put the token to the appsettings.json > OpenAIConfig>Token  
  or set it to user secrets  
  `dotnet user-secrets set "OpenAIConfig:Token" "YOUR_TOKEN"`

### SiCepat
- Set the BaseUrl to the appsettings or user secrets. 