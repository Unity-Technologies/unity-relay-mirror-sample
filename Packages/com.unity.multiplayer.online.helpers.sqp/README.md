# SQP Helper

## Installation

extract the contents of "com.unity.multiplayer.online.helpers.sqp.zip" to your projects Packages folder

Add the following line to your manifest.json:

"com.unity.multipayer.online.helpers.sqp": "file:com.unity.multiplayeronline.helpers.sqp"

Note: you may need a comma at the end of the line.

## How to Use

Thie package helps you handle SQP requests in a Unity game. 

Add the `ServerQueryManager` in your scene.

Instantiate the ServerQueryManager in your projects code.
 
Construct your QueryData and make a call to 'ServerQueryManager.ServerStart({QueryData}, {Protocol}, {QueryPort})' when starting your game server. 

Moreover, you can modify the game data by calling `ServerQueryManager.GetQueryData()` and `ServerQueryManager.UpdateQueryData()`.

