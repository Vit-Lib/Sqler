-------------------
--2.���ɴ��������������洢���̡���ͼ�Ĵ������
--2.GenerateTriggerFunctionProcedureView.sql
-------------------



--(1)ָ���С��С���ķָ������ͷ��ص��ļ������ƣ�Ĭ��Ϊsql.txt��
select '' fieldSeparator,'
/*G'+'O*/'+' 
G'+'O'+'
' rowSeparator,'
/*G'+'O*/'+' 
G'+'O'+'
' tableSeparator,'2.CreateTriggerFunctionProcedureView.sql' [fileName];



--(2)��ȡ���ݵ����

declare @IDStart int;
declare @IDNext int;
 --����text ָ��   
 DECLARE @ptrval BINARY(16)
declare @sqlNext varchar(8000)


SELECT  Identity(int,1,1) [ID],
    o.xtype,
(CASE o.xtype WHEN 'X' THEN '��չ�洢����' WHEN 'TR' THEN '������' WHEN 'PK' THEN '����' WHEN 'F' THEN '���' WHEN 'C' THEN 'Լ��' WHEN 'V' THEN '��ͼ' WHEN 'FN' THEN '����-����' WHEN 'IF' THEN '����-��Ƕ' WHEN 'TF' THEN '����-��ֵ' ELSE '�洢����' END)
 AS [����]
, o.name AS [������]
, o.crdate AS [����ʱ��]
, o.refdate AS [����ʱ��]
,convert(text,c.[text]) AS [�������]
into #tb
FROM dbo.sysobjects o LEFT OUTER JOIN
dbo.syscomments c ON o.id = c.id
WHERE (o.xtype IN ('X', 'TR', 'C', 'V', 'F', 'IF', 'TF', 'FN', 'P', 'PK')) AND
(OBJECTPROPERTY(o.id, N'IsMSShipped') = 0)
--order BY  o.xtype


 
while(1=1)
begin
set @IDStart=null;
 	select top 1 @IDStart=start.[ID], @IDNext=nex.[ID], @ptrval=TEXTPTR(start.[�������]),@sqlNext=convert(varchar(8000),nex.[�������])        
	from #tb start,#tb nex where start.[ID]<nex.[ID] and  start.[xtype]=nex.[xtype] and  start.[������]=nex.[������]


 	if( @IDStart is null) break;


	UPDATETEXT #tb.[�������] @ptrval NULL 0 @sqlNext;
	delete #tb where [id]=@IDNext;

end



  
--������
select [�������]  from #tb   where xtype='TR';

--����
select [�������]  from #tb   where xtype='FN';
select [�������]  from #tb   where xtype='TF';

--�洢����
select [�������]   from #tb   where xtype='P';




--��ͼ ������ ������ϵ��
select identity(int,1,1) [id],[������] [name], convert(smallint,null) SortCode  into #tmp_Enty  from #tb   where xtype='V'; 

 
SELECT distinct  o.[name],  p.[name]  dependOn
into #tmp_R
FROM sysobjects o 
INNER JOIN sysdepends d     ON d.id = o.id   
INNER JOIN sysobjects p     ON d.depid = p.id  and p.xtype='v'  and exists(select 1 from #tmp_Enty where p.[name] = #tmp_Enty.[name] )
where  o.xtype='v' and exists(select 1 from #tmp_Enty where o.[name] = #tmp_Enty.[name] )

 
declare @sc int;
set @sc=1;

while 1=1
begin
	set @sc=@sc+1;
	update #tmp_Enty  set SortCode=@sc  from #tmp_Enty enty   where SortCode is null 
	and not exists(select 1 from #tmp_R r inner join  #tmp_Enty parent on  r.[dependOn]=parent.[name]  where r.[name]=enty.[name] and parent.SortCode is null  )	
	if(0=@@ROWCOUNT)
	 	break;
	
end
update #tmp_Enty   set SortCode=@sc+1  where SortCode is null
 
select [�������]  from #tb inner join #tmp_Enty on  #tb.������=#tmp_Enty.[name] order by SortCode;

drop table #tmp_Enty;
drop table #tmp_R;
 

 





drop table #tb

 
