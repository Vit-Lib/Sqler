autoTemp 2.0.2(2022-02-10)

可通过修改数据库字段的注释 来配置字段控件属性。如：
 
    [fieldIgnore]        忽略当前字段
    [idField]            手动指定当前列为id列,默认为数据库主键
	[field:{path}=value] 直接把值设置到field中，path可以为多级路径，如：[field:title=登录名]
    [field:title=登录名]     字段的title为登录名，即标题显示登录名
    [field:visiable=false]   字段不可见
    [field:editable=false]   字段不可编辑
    [field:list_width=200]   列表页中此字段的宽度为200
	[field:field=name]       字段的field为name，同时为ig-id的值
	[field:ig-class=TextArea]字段的ig-class为 TextArea,不指定则为Text
    [field:ig-param={height:100,width:100}]    ig-param的值，一般为对象


    树形列表：  必须同时指定pid列和treeField列，否则作为普通列表数据展示
    [pidField]          当前列为pid
    [treeField]         列表页中当前列作为树形展示
 
    [rootPidValue:0]    树控件根节点的pid的值，默认为0,在任一列的注释中指定皆可


    列表页数据筛选条件：  同一字段可指定多个筛选条件
    [filter:开始时间,=]   当前列作为筛选条件，筛选条件名称为开始时间(若不指定则为当前列title)，筛选方式为"="
    
	[controller:path=value] 直接把值设置到controllerConfig中，path可以为多级路径，如：[controller:permit.delete=false]
	[controller:list.title=SqlText] 设置list页面标题为SqlText

 

	设置不可删除（其他同理: insert、update、show、delete）
		[controller:permit.delete=false]

	设置list页面rowButtons：
		[controller:list.rowButtons=&#91;{text:'查看id',handler:'function(callback,id){  callback();alert(id); }' }&#93;] 
									


field:
{ 'ig-class': 'TextArea', field: 'name', title: '<span title="装修商名称">装修商</span>', list_width: 200 ,visiable:false,editable:false  }

filter:
 { field: 'name', title: '装修商',filterOpt:'=' }


 注释：

 1.值中若出现中括号，可用以下转义。
 [  \x5B --- 左中括号  
 ]  \x5D --- 右中括号  


 2.xml转义符号
  <     &lt;	
  >		&gt;	
  &		&amp;	
  '		&apos;	
  "		&quot;	
  空格  &nbsp;	





	demo：
	http://localhost:4570/autoTemp/Scripts/autoTemp/list.html?dataProvider=LocalStorageProvider
    http://localhost:4570/autoTemp/Scripts/autoTemp/list.html?dataProvider=LocalStorageProvider&tree=false


	http://localhost:4570/autoTemp/Scripts/autoTemp/list.html?apiRoute=/autoTemp/data/demo_list/{action}
	http://localhost:4570/autoTemp/Scripts/autoTemp/list.html?apiRoute=/autoTemp/data/demo_repository_list/{action}
	http://localhost:4570/autoTemp/Scripts/autoTemp/list.html?apiRoute=/autoTemp/data/demo_tree/{action}






var controllerConfig = {
       dependency: {
	    css: [],
	    js: []
	},

	/* 添加、修改、查看、删除 等权限,可不指定。 默认值均为true  */
	'//permit': {
	    insert: false,
	    update: false,
	    show: false,
	    delete: false
	},

	idField: 'id',
	pidField: 'pid',
	//treeField: 'name',
	rootPidValue: '0',

	list: {
	    title: 'autoTemp-demo',
	    buttons: [
		{ text: '执行js', handler: 'function(callback){  setTimeout(callback,5000); }' },
		//{ text: '调用接口', ajax: { type: 'GET', url: '/autoTemp/demo_list/getConfig' } }
	    ],
	    rowButtons: [
		{ text: '查看id', handler: 'function(callback,id){  callback();alert(id); }' },
		//{ text: '调用接口', ajax: { type: 'GET', url: '/autoTemp/{template}/getConfig?name={id}' } }
	    ]
	},


	fields: [
	    { 'ig-class': 'Text', field: 'name', title: '<span title="装修商名称">装修商</span>', list_width: 200, editable: false },
	    { 'ig-class': 'Text', field: 'sex', title: '性别', list_width: 80, visiable: false },
	    { 'ig-class': 'TextArea', field: 'random', title: 'random', list_width: 150, 'ig-param': {height:300} },
	    { 'ig-class': 'Text', field: 'random2', title: 'random2', list_width: 150 }
	],

	filterFields: [
	    { 'ig-class': 'Text', field: 'name', title: '装修商', filterOpt: 'Contains' },
	    { 'ig-class': 'Text', field: 'sex', title: '性别' },
	    { 'ig-class': 'Text', field: 'random', title: 'random' }
	]

    };




