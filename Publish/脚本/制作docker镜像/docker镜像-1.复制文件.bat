echo '�����ļ�'

::��������
xcopy "..\..\04.����վ��\Sqler"  "..\..\06.Docker\��������\sqler\app"  /e /i /r /y


:: �����ļ�
xcopy "..\..\04.����վ��\Sqler\Data"  "..\..\06.Docker\�����ļ�\Data" /e /i /r /y
xcopy  "..\..\04.����վ��\Sqler\appsettings.json" "..\..\06.Docker\�����ļ�" 

