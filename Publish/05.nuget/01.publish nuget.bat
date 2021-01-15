title "publish nuget"

cd /d ../../Library

 

echo 'pack Vit.Db.DbMng'
cd /d Vit.Db.DbMng
dotnet build --configuration Release
dotnet pack --configuration Release --output ..\..\Publish\05.nuget\nuget  
cd /d ..


cd /d ..\Publish\05.nuget

echo 'publish nuget succeed£¡' 