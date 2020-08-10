# Spaceracebot 1955

Epic space race facebook bot where you compete in a snake&ladder game to be selected as a crew to the moon.
Winning alone is not enough, you need to win together with a couple friends to have a successful lunar mission.
Comment on the newest facebook post to have the bot roll your dice.

## Install

### Requirement
1. Microsoft Azure subscription
2. .NET Core >2.2
3. [Azure Functions Visual Studio Code Extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions)

### Build

Rename `local.settings.example.json` to `local.settings.json` and open the `local.settings.json` env file.
Change the `SpaceRaceBotPage` and `SpaceRaceBotToken` values according to your bot page ID and token. Then:

```
dotnet restore
dotnet build
```

### Deploy
1. From Visual Studio Code press `Ctrl+Shift+A` then log in to your Azure account
2. Right click on your subscription then click "Create Function App in Azure" and finish setting up your function app
3. Click on "Deploy to Function App"
4. Right click on "Application Settings" then click "Upload Local Settings"

### Settings
#### State File
From your browser, navigate to `https://<yourfunctionappname>.scm.azurewebsites.net/DebugConsole`

```
cd site
touch spaceracestate.json
mkdir img && cd img && mkdir spaceracebot && cd spaceracebot
```

Drag and drop/upload your images in the project's `img` folder here.

## Bot page

[Spaceracebot 1955](https://www.facebook.com/pg/spaceracebot/)
