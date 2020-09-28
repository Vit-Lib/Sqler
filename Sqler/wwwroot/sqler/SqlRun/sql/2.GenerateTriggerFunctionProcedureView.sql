-------------------
--2.生成触发器、函数、存储过程、视图的创建语句
--2.GenerateTriggerFunctionProcedureView.sql
-- by lith on 2020-09-28 v2.0
-------------------


--(1)指定列、行、表的分隔符，和返回的文件的名称
/*
<SqlRunConfig>

<fileName>2.CreateTriggerFunctionProcedureView.sql</fileName>

<tableSeparator></tableSeparator>
<rowSeparator></rowSeparator>
<fieldSeparator></fieldSeparator>

</SqlRunConfig>



-- 添加 G O（尚不使用）
<tableSeparator>*G</tableSeparator>
<tableSeparator>O*</tableSeparator>
<tableSeparator>/
G</tableSeparator>
<tableSeparator>O
</tableSeparator>
*/




 



--(2)获取数据的语句

declare @IDStart int;
declare @IDNext int;
--定义text 指针   
declare @ptrval BINARY(16)
declare @sqlNext varchar(8000)


SELECT  Identity(int,1,1) [ID],
    o.xtype,
(CASE o.xtype WHEN 'X' THEN '扩展存储过程' WHEN 'TR' THEN '触发器' WHEN 'PK' THEN '主键' WHEN 'F' THEN '外键' WHEN 'C' THEN '约束' WHEN 'V' THEN '视图' WHEN 'FN' THEN '函数-标量' WHEN 'IF' THEN '函数-内嵌' WHEN 'TF' THEN '函数-表值' ELSE '存储过程' END)
 AS [类型]
, o.name AS [对象名]
, o.crdate AS [创建时间]
, o.refdate AS [更改时间]
,convert(text,c.[text]) AS [声明语句]
into #tb
FROM dbo.sysobjects o LEFT OUTER JOIN
dbo.syscomments c ON o.id = c.id
WHERE (o.xtype IN ('X', 'TR', 'C', 'V', 'F', 'IF', 'TF', 'FN', 'P', 'PK')) AND
(OBJECTPROPERTY(o.id, N'IsMSShipped') = 0)
--order BY  o.xtype



while(1=1)
begin
set @IDStart=null;
 	select top 1 @IDStart=start.[ID], @IDNext=nex.[ID], @ptrval=TEXTPTR(start.[声明语句]),@sqlNext=convert(varchar(8000),nex.[声明语句])        
	from #tb start,#tb nex where start.[ID]<nex.[ID] and  start.[xtype]=nex.[xtype] and  start.[对象名]=nex.[对象名];

 	if( @IDStart is null) break;

	UPDATETEXT #tb.[声明语句] @ptrval NULL 0 @sqlNext;
	delete #tb where [id]=@IDNext;

end



  
--(x.1)触发器
select ('


/* 触发器 */') comment;
select [声明语句]  from #tb   where xtype='TR';







--(x.2)函数
select ('


/* 函数 */') comment;
select [声明语句]  from #tb   where xtype='FN';
select [声明语句]  from #tb   where xtype='TF';








--(x.3)存储过程
select ('


/* 存储过程 */') comment;
select [声明语句]   from #tb   where xtype='P';







--(x.4)视图 （考虑 依附关系）
select ('


/* 视图 */') comment;
select identity(int,1,1) [id],[对象名] [name], convert(smallint,null) SortCode  into #tmp_Enty  from #tb   where xtype='V'; 

 
SELECT distinct  o.[name],  p.[name]  dependOn
into #tmp_R
FROM sysobjects o 
INNER JOIN sysdepends d     ON d.id = o.id   
INNER JOIN sysobjects p     ON d.depid = p.id  and p.xtype='v'  and exists(select 1 from #tmp_Enty where p.[name] = #tmp_Enty.[name] )
where  o.xtype='v' and exists(select 1 from #tmp_Enty where o.[name] = #tmp_Enty.[name] );

 
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
update #tmp_Enty   set SortCode=@sc+1  where SortCode is null;
 
select [声明语句]  from #tb inner join #tmp_Enty on  #tb.对象名=#tmp_Enty.[name] order by SortCode;

drop table #tmp_Enty;
drop table #tmp_R;
 

 





drop table #tb;

 
