echo '�����ļ�'

::��������
mklink /j "..\..\06.Docker\��������\sqler\root\app\Sqler" "..\..\04.����վ��\Sqler" 


:: �����ļ�
mklink /j "..\..\06.Docker\�����ļ�\Data" "..\..\04.����վ��\Sqler\Data" 
xcopy  "..\..\04.����վ��\Sqler\appsettings.json" "..\..\06.Docker\�����ļ�" 

