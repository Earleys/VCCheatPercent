# VCCheatPercent
A small project that allows Twitch viewers to enter cheats into someone's game.

### How does it work?
This tool will connect to a channel, and read every message in that channel. Once the user types a cheat (for example: ```!aspirine```), the code will be written to memory. The final key will be sent as a key. GTA: Vice City reads cheats on key press, so the final key can't just be sent to memory.

### How to use it?
The tool is very easy to use. Once  you launch the .exe, you will be able to enter a channel name you want to join.  If you have not yet configured the config file, you will be prompted to do so. Just open the config file (named ```config.txt```) that has been created, and fill it in. After that you can continue using the exe file. Be sure to check the 'Activate' checkbox. 
Also, both 'bigbang' and 'icanttakeitanymore' will have a shared cooldown of 5 minutes. This can be changed in the code, though.

### Where to find it?
Just browse through the directory. Go to ```VCCheatPercent -> bin -> Debug -> ``` and there you will find an exe. You can copy that exe to where-ever you want, but be aware it will also require a config file in the same folder.
