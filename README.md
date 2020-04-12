# wdDNC
A .NET Core 3.1 Implementation of the Astrometry.NET Nova-API for EKOS

This Project implements the parts of the API needed for EKOS (Kstars EKOS). 
It works on a Raspberry Pi.

Quick installation guide:

* Clone https://github.com/dstndstn/astrometry.net.git, build and install
* Clone this Repository
* Install .Net Core and the EntityFramework tools
* Install Postgresql
* set password for user "postgres"
* create empty DB "wd"
* set Database Password in 
  * wdApi/appsettings.json
  * wdWorker/appsettings.json
  * wdDB/Model/wdDBModel.cs
  * Yes, I need to work on this :-)
* go to ./wdApi
* dotnet ef database update
* run in a terminal (SCREEN/TMUX): dotnet run
* in another terminal: cd wdWorker, change settings (paths to scripts etc) in appsettings.json
* dotnet run
* in EKOS as astrometry.net URL enter: http://IP-OF-DEVICE:5000
* Happy Stargazing!
