# MHServerEmuMini

MHServerEmuMini is a more lightweight and flexible version of [MHServerEmu](https://github.com/Crypto137/MHServerEmu). It has fewer features compared to the main server, but can be more easily adapted to different versions of the game client.

The currently supported version of the client is **1.10.0.643**.

The latest builds are available [here](https://github.com/Crypto137/MHServerEmuMini/releases/latest).

## Setup

1. Make sure you have .NET Desktop Runtime 8 installed. You can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

2. Acquire a copy of Marvel Heroes, version 1.10.643. See below for Steam download instructions.

3. Open the ClientConfig.xml file located in `Marvel Heroes\Data\Configs\` with a text editor.

4. Replace `hq-gar-build04/bitraider/dev_alternate/SiteConfig.xml` with `localhost/SiteConfig.xml`.

### Downloading Older Versions from Steam

**NOTE:** This requires you to have the game in your Steam library.

1. Open Windows search, enter `run` and open the run application.

2. Enter `steam://open/console`. The Steam console should open.

3. Enter `download_depot 226320 226321 2968810361455133183` in the Steam console.

4. Wait for the client to finish downloading. There will be no visible progress indication until the download is complete.

5. Get client files from `C:\Program Files (x86)\Steam\steamapps\content\app_226320\depot_226321` (replace `C:\Program Files (x86)\Steam` if you have Steam installed somewhere else).

## Running

1. Run the included `StartServers.bat` file and wait for MHServerEmuMini to initialize.

2. Run `MarvelGame.exe` located in `Marvel Heroes\UnrealEngine3\Binaries\Win32\` with the following arguments: `-nobitraider -nosteam`.

3. Log in with any username and password.

4. When you are done, run `StopServers.bat` to stop the servers.
