
:: 部署文件
xcopy  "..\ReleaseFile\docker-deploy" "..\release\release\docker-deploy"  /e /i /r /y
xcopy "..\release\release\publish\Sqler\Data"  "..\release\release\docker-deploy\sqler\Data" /e /i /r /y
xcopy "..\release\release\publish\Sqler\appsettings.json" "..\release\release\docker-deploy\sqler"  /y


echo %~n0.bat 执行成功！

