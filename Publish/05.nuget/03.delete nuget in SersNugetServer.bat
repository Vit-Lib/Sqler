@echo off 

set version=2.1.5

dotnet nuget delete Vit.Db.DbMng %version% -k ee28314c-f7fe-2550-bd77-e09eda3d0119  -s http://nuget.sers.cloud:8 --non-interactive

echo 'delete succeed£¡'
 

 