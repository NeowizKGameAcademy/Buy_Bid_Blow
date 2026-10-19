@echo off
rem Stops everything LocalPlaytest.bat started.
taskkill /IM BuyBidBlowServer.exe /F >nul 2>&1
taskkill /IM BuyBidBlow.exe /F >nul 2>&1
