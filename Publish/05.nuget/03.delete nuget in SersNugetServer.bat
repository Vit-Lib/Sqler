@echo off 

set version=1.0.13.93

dotnet nuget delete ServiceAdaptor.NetCore %version% -k ee28314c-f7fe-2550-bd77-e09eda3d0119  -s http://nuget.sers.cloud:8 --non-interactive

dotnet nuget delete ServiceAdaptor.NetCore.Consul %version% -k ee28314c-f7fe-2550-bd77-e09eda3d0119  -s http://nuget.sers.cloud:8 --non-interactive

dotnet nuget delete ServiceAdaptor.NetCore.MinHttp %version% -k ee28314c-f7fe-2550-bd77-e09eda3d0119  -s http://nuget.sers.cloud:8 --non-interactive

dotnet nuget delete ServiceAdaptor.NetCore.Sers %version% -k ee28314c-f7fe-2550-bd77-e09eda3d0119  -s http://nuget.sers.cloud:8 --non-interactive

dotnet nuget delete ServiceAdaptor.NetCore.Be.Eureka %version% -k ee28314c-f7fe-2550-bd77-e09eda3d0119  -s http://nuget.sers.cloud:8 --non-interactive

echo 'delete succeed£¡'
 

 