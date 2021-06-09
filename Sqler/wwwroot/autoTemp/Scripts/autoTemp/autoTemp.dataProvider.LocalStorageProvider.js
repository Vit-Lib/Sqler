/*
 * autoTemp 扩展  
 * Date   : 2020-01-09
 * Version: 2.0
 * author : Lith
 * email  : serset@yeah.net
 */
; (function (scope) {

    /**
     * 列表数据    
     */
    scope.LocalStorageProvider = function () {
        var self = this;

        var idField = 'id';
        var pidField = 'pid';

        function getDataSource() {

            var dataSource = localStorage.getValue('dataSource');
            if (!dataSource) {
                dataSource = [];
                for (var t = 1; t <= 1000; t++) {
                    var item = { name: 'name' + t, age: 20, sex: '1' };
                    item.random = Math.random();
                    item[idField] = t;
                    item[pidField] = parseInt(t / 10);

                    dataSource.push(item);
                }
                localStorage.setValue('dataSource', dataSource);
            }
            return dataSource;
        }

        function setDataSource(dataSource) {
            localStorage.setValue('dataSource', dataSource);
        }

        /**
     * 
     * @param {} param
     * @param {function(ApiRet){}} callback   {success:true,data:controllerConfig }
     */
        self.getControllerConfig = function (param, callback) {

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
                treeField: 'name',
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


            if (document.url_GetCurArg('tree') == 'false') {
                controllerConfig.treeField = null;
            }

            callback({ success: true, data: controllerConfig });

        };


        /**
         * 
         * @param {queryParam} param  {page:{} ,filter:[],sort:[],arg:{}   }
         *         "page": {  "pageSize": 10,  "pageIndex": 1  }                    
         *         "filter": [ {  "field": "status",   "opt": "=",     "value": 1 }]
         *         "sort":   [ {  "field": "id",    "asc": false   }]               
         *         "arg" :    {isRoot:true,pid:5}
         * @param { function(ApiRet){}} callback   {success:true,data:{ "totalCount": 59,    "pageSize": 10, "pageIndex": 1,rows:[]  } }
         */
        self.getList = function (param, callback) {
            
            //(x.1)DataSource          
            var rows = getDataSource();
            var data = {};
            var apiRet = { success: true, data: data };
 

            //(x.2)filter
            if (param.filter && param.filter.length > 0) {
                var filters = param.filter;
                rows = rows.filter(function (item) {
                    for (var t in filters) {
                        var filter = filters[t];
                        if (filter.opt == '=') {
                            if (item[filter.field] != filter.value) return false;
                        } else if (filter.opt == 'Contains') {
                            if (item[filter.field].indexOf(filter.value)<0) return false;                            
                        }
                    }
                    return true;
                });
            }

            data.totalCount = rows.length;


            //(x.3)sort
            var sort = (param.sort || [])[0] || { "field": idField, "asc": true };
            var sortField = sort.field;
            var sortAsc = sort.asc;
            if (sortAsc) {
                rows = rows.sort(function (a, b) { return a[sortField] <= b[sortField] ? -1 : 1; });
            } else {
                rows = rows.sort(function (a, b) { return a[sortField] <= b[sortField] ? 1 : -1; });
            }

            //(x.4)page
            if (param.page) {

                var page = param.page;
                var startIndex = page.pageSize * (page.pageIndex - 1);
                rows = rows.slice(startIndex, startIndex + page.pageSize);

                data.pageSize = page.pageSize;
                data.pageIndex = page.pageIndex;
            }



            //(x.5)_childrenCount
            var dataSource = getDataSource();
            for (var t in rows) {
                var row = rows[t];
                row._childrenCount = dataSource.filter(function (item) { return item[pidField] == row[idField]; }).length;
            }


            //(x.6)返回数据
            data.rows = rows;
            callback(apiRet);
        };


        self.getModel = function (id, callback) {
            if (!id) return null;
            var dataSource = getDataSource();

            var model = null;

            for (var t in dataSource) {
                var item = dataSource[t];
                if (item[idField] == id) {
                    model = item;
                    break;
                }
            }

            if (model) {
                callback({ success: true, data: model });
            } else {
                callback({ success: false });
            }

        };
        /**
        * 
        * @param {any} model
        * @param {Function} callback  function(apiReturn){}
        */
        self.insert = function (model, callback) {

            var dataSource = getDataSource();
            model[idField] = dataSource[dataSource.length - 1][idField] + 1;
            dataSource.push(model);
            setDataSource(dataSource);

            callback({ success: true });
        };
        /**
         * 
         * @param {any} model
         * @param {function(ApiRet){}} callback  function(apiRet){}
         */
        self.update = function (model, callback) {
            var dataSource = getDataSource();

            for (var t in dataSource) {
                var item = dataSource[t];
                if (item[idField] == model[idField]) {
                    $.extend(item, model);
                    break;
                }
            }

            setDataSource(dataSource);

            callback({ success: true });
        };

        self.delete = function (id, callback) {
            var dataSource = getDataSource();

            var index;
            for (var t in dataSource) {
                var item = dataSource[t];
                if (item[idField] == id) {
                    index = t;
                    break;
                }
            }
            if (index) {
                dataSource.splice(index, 1);
                setDataSource(dataSource);
                callback({ success: true });
            } else {
                callback({ success: false, error: { errorMessage: '数据不存在' } });
            }
        };
    };


})(autoTemp.dataProvider || (autoTemp.dataProvider = {}));