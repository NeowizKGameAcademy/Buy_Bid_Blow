@echo off
rem Local playtest: dedicated server + headless test bots + one windowed client.
rem Usage: Tools\LocalPlaytest.bat [bot count, default 3]
rem Build first with Tools > Buy Bid Blow > Build (Dedicated Server, Client).
setlocal
set BOTS=%1
if "%BOTS%"=="" set BOTS=3
set /a PLAYERS=%BOTS%+1
set BUILDS=%~dp0..\Builds

start "BuyBidBlow Server" "%BUILDS%\Server\BuyBidBlowServer.exe" -port 7777 -minPlayers %PLAYERS%
timeout /t 2 /nobreak >nul
for /l %%i in (1,1,%BOTS%) do start "" /min "%BUILDS%\Client\BuyBidBlow.exe" -batchmode -nographics -bot -name Bot%%i -address 127.0.0.1 -port 7777 -logFile "%BUILDS%\bot%%i.log"
timeout /t 3 /nobreak >nul
start "" "%BUILDS%\Client\BuyBidBlow.exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 720
