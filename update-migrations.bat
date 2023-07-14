@echo off
dotnet tool restore
dotnet ef migrations add -p Cachr.Core -s Cachr.Server %1