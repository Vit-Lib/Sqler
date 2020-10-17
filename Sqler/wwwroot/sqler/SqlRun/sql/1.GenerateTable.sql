-------------------
--1.生成建表语句
--1.GenerateTable.sql
-- 含 表字段、字段备注、默认值约束 、unique约束、primary key约束
-- by lith on 2020-10-17 v2.1
-------------------


--(1)指定列、行、表的分隔符，和返回的文件的名称
/*
<SqlRunConfig>

<fileName>1.CreateTable.sql</fileName>

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

create table #Proc_S_TableStruct_ColInfo([col_id] int,[col_name] varchar(200),[col_typename] varchar(200),[col_len] int,[col_identity] int,[col_seed] int,[col_increment] int,[collation] varchar(200),[col_null] int,[col_DefaultValue] varchar(2000),[ConstraintName_DefaultValue]  varchar(200),[ExtendedProperty] varchar(4000),[ConstraintName_PrimaryKey] varchar(200),[ConstraintName_Unique] varchar(200))
 
create table #Proc_S_TableStruct_MShelpcolumns([col_name] varchar(200),[col_id] int,[col_typename] varchar(200),[col_len] int,[col_prec] varchar(200),[col_scale] varchar(200),[col_basetypename] varchar(200),[col_defname] varchar(200),[col_rulname] varchar(200),[col_null] int,[col_identity] int,[col_flags] int,[col_seed] int,[col_increment] int,[col_dridefname] varchar(200),[text] text ,[col_iscomputed] varchar(200),[col_text] varchar(200),[col_NotForRepl] int,[col_fulltext] int,[col_AnsiPad] int,[col_DOwner] int,[col_DName] varchar(200),[col_ROwner] int,[col_RName] varchar(200),[collation] varchar(200),[ColType] varchar(200),[column1] int ,[column2] int)

create table #Proc_S_TableStruct_SqlCreateTb([id] int identity(1,1),sql varchar(700));

 




--(3)循环处理各个表
select  [Name] into #Proc_S_TableStruct_tbName from sysobjects where [type] = 'U'  and [Name]!='dtproperties';

declare @tbName varchar(100); 
declare @tbCount int; 
declare @tbIndex int; 

set @tbCount=(select count(*) from #Proc_S_TableStruct_tbName);
set @tbIndex=0;

while 1=1
begin

	set @tbIndex=@tbIndex+1;


    --(x.2)获取表信息

	set @tbName=( SELECT top 1  [Name] from #Proc_S_TableStruct_tbName)
	if @tbName is null 
		break;	 
	delete  #Proc_S_TableStruct_tbName  where [Name]=@tbName;


	--(x.2.1) 获取字段基础信息 
	insert into #Proc_S_TableStruct_MShelpcolumns  exec sp_MShelpcolumns @tbName;  
 
	select [col_id],[col_name],col_typename,col_len,col_identity,col_seed,col_increment,collation   
	       ,col_null,[text] col_DefaultValue,col_dridefname [ConstraintName_DefaultValue]  
	into #Proc_S_TableStruct_Col
	from #Proc_S_TableStruct_MShelpcolumns;

	truncate table #Proc_S_TableStruct_MShelpcolumns;


	--（x.2.2）  获取字段的备注
	select objname [col_name],[value] [ExtendedProperty] into #Proc_S_TableStruct_Property  from ::fn_listextendedproperty(null,N'user',N'dbo',N'table',@tbName,N'column',null) where 1=1


	--（x.2.3）主码 和 唯一 约束 。 CONSTRAINT_TYPE：  'PRIMARY KEY' 和 'UNIQUE'
	select  t1.COLUMN_NAME [col_name],t2.CONSTRAINT_TYPE ,t1.Constraint_Name
	into #Proc_S_TableStruct_Constraint
	from information_schema.key_column_usage t1 
	left join information_schema.table_constraints t2 on t1.Constraint_Name=t2.Constraint_Name 
	where t1.TABLE_NAME=@tbName

	--（x.2.4） 合并最终结构数据
	insert into #Proc_S_TableStruct_ColInfo
	select c.*
	,convert(varchar(8000) , p.[ExtendedProperty])
	,conPrimary.Constraint_Name [ConstraintName_PrimaryKey]
	,conUnique.Constraint_Name [ConstraintName_Unique]
	from  #Proc_S_TableStruct_Col c
	left join #Proc_S_TableStruct_Property p on c.[col_name] Collate Database_Default =p.[col_name]
	left join #Proc_S_TableStruct_Constraint conPrimary on c.[col_name]=conPrimary.[col_name] and conPrimary.[CONSTRAINT_TYPE]='PRIMARY KEY'
	left join #Proc_S_TableStruct_Constraint conUnique on c.[col_name]=conUnique.[col_name]  and conUnique.[CONSTRAINT_TYPE]='UNIQUE'

 

	--(x.2.5)清理数据
	drop  table #Proc_S_TableStruct_Col;
	drop  table #Proc_S_TableStruct_Property;
	drop  table #Proc_S_TableStruct_Constraint;



	--(x.3)输出
	select ('




/* ['+  CONVERT(varchar(10),@tbIndex)+'/'+ CONVERT(varchar(10),@tbCount) +']创建表 '+@tbName+' */
') comment;



	--(x.3.1)建表
	select ('
/* 创建表字段 */') comment;

	insert into #Proc_S_TableStruct_SqlCreateTb(sql)
	select '
create table [dbo].['+@tbName+'] ( ';


	insert into #Proc_S_TableStruct_SqlCreateTb(sql) 
	select 
	' ['+[col_name]+'] ['+[col_typename]+']'

	-- [类型] (长度)
	+(case when(0!=charindex('char',col_typename)) then (case when [col_len]<=0 then '(MAX)' else ' ('+convert(varchar(100),[col_len])+')' end)  else '' end)  

	-- IDENTITY(2010,100)
	+(case when(1=col_identity) then ' IDENTITY('+convert(varchar(100),[col_seed]) +','+ convert(varchar(100),[col_increment]) +')' else '' end)  

	-- COLLATE Chinese_PRC_CI
	+(case when(collation is not null) then ' COLLATE '+[collation] else '' end)  

	-- NOT NULL  
	+(case when(1=col_null) then ' NULL' else ' NOT NULL' end)  
	+'
	,'
	from #Proc_S_TableStruct_ColInfo;


	update #Proc_S_TableStruct_SqlCreateTb set sql=(isnull(left(sql,len(sql)-1),'')+'); ') where [ID]= (select max([ID]) from #Proc_S_TableStruct_SqlCreateTb);


	select sql from #Proc_S_TableStruct_SqlCreateTb;
	truncate table #Proc_S_TableStruct_SqlCreateTb;    



 


	--(.3.2)默认值约束
	select ('
/* 默认值约束 */') comment;
	select ('
alter table '+ quotename(@tbName)
	+' add constraint '+quotename(ConstraintName_DefaultValue)
	+' default'+col_DefaultValue
	+' for ' + quotename([col_name])
	+';') [sql]
	from #Proc_S_TableStruct_ColInfo
	where  ConstraintName_DefaultValue is not null;

 
 
	--(.3.3)unique约束              ALTER TABLE table_a ADD unique(aID);
	select ('
/* unique约束 */') comment;
	select ('
alter table '+ quotename(@tbName)
	+' add unique( '+quotename([col_name])+')'
	+';') [sql]
	from #Proc_S_TableStruct_ColInfo
	where  ConstraintName_Unique is not null


 
	--(.3.3)primary key约束
	--'ALTER TABLE ['+@tbName+'] ADD CONSTRAINT PK__'+@tbName+'_'+@colName+'__lit17032317 PRIMARY KEY CLUSTERED (['+@colName+']) '
	select ('
/* primary key约束 */') comment;
	select('
alter table '+ quotename(@tbName)
	+' add constraint '+quotename(ConstraintName_PrimaryKey)
	+' PRIMARY KEY CLUSTERED ('+quotename([col_name])+')'
	+';') [sql]
	from #Proc_S_TableStruct_ColInfo
	where  ConstraintName_PrimaryKey is not null;
	


	truncate table #Proc_S_TableStruct_ColInfo;



end




drop table #Proc_S_TableStruct_tbName;
drop table #Proc_S_TableStruct_MShelpcolumns;
drop table #Proc_S_TableStruct_ColInfo;
drop table #Proc_S_TableStruct_SqlCreateTb;

 