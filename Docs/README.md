This project was developed using Unreal Engine [4.26.1 release](https://github.com/EpicGames/UnrealEngine/releases/tag/4.26.1-release). It is intended to showcase a variety of Unity services and how they can be integrated with UE4.

# Getting Started

## Unity Onboarding

This project makes use of three Unity services; Multiplay, Vivox, and Matchmaker. You will need to be onboarded to each in order to run this project successfully.

### Multiplay

Onboarding to Multiplay is not a self serve process. A Unity representative will need to generate service identifiers for you and will share them via a PDF. You will need these service identifiers to use the resources in the `Deployment/` folder. You will also need them to populate the `config.toml` that powers the `ShooterBackend`.

### Vivox

Onboarding to Vivox is a self serve process. You will need to navigate to the [developer portal](https://developer.vivox.com/) and create an app on the dashboard. This will give you environment details and API keys needed for the `config.toml` that powers the `ShooterBackend`.

### Matchmaker

Onboarding to Matchmaker is not a self serve process. A Unity representative will need to generate a client ID and client secret for you. In order for them to do this, you will need to navigate to the [Unity Dashboard](https://dashboard.unity3d.com/) and create a project. This project will have an associated Project ID, or UPID. Once you share this UPID with your Unity representative, they will be able to provide you with matchmaker credentials. These will also be used to populate the `config.toml` that powers the `ShooterBackend`.

## Running This Project

To get started with this example code and to run it locally, refer to the "Getting Started" folder.

### Shooter Backend

Some Unity services require an independent backend service that is hosted/managed by you. This example project is shipped with a backend that is compatible with the game client. This backend service is required for this demo project to work correctly. The game client hooks into it via the `UnityRpc` class. This allows for us to maintain an identity for a user in a centralized place, rather than client-side.

To run the SampleBackend, please refer to the `README.md` in the top-level "SampleBackend" folder. More high-level information about the backend can be found in this directory's "SampleBackend" folder.

### Multiplay

Instructions and references to example code for Multiplay can be found in the "Multiplay" folder.

### Vivox

Instructions and references to example code for Vivox can be found in the "Vivox" folder.

### Matchmaking

Instructions and references to example code for Matchmaking can be found in the "Matchmaking" folder.