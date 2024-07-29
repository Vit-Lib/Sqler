/*
 * autoTemp 扩展  
 * Date   : 2020-01-09
 * Version: 2.0
 * author : Lith
 * email  : LithWang@outlook.com 
 * 
 */
; (function (scope) {

    var apiRoute = document.url_GetCurArg('apiRoute') || '/autoTemp/{url.template}/{action}';

    // ajax({ url:'http://a.com/a',type:'POST',header:{},data:{},onSuc:function(apiRet){ } });
    // ajax({ action:'config',type:'POST',header:{},data:{},onSuc:function(apiRet){ } });
    function ajax(param) {

        var url = param.url, type = param.type || 'GET', header = param.header, data = param.data, onSuc = param.onSuc;

        if (!url) {
            url = apiRoute;
        }

        url = url.replace('{action}', param.action);

        if (type == 'GET') {
            if (typeof (data) == 'object') {
                for (var k in data) {
                    var v = data[k];
                    if (typeof (v) != 'string') {
                        data[k] = JSON.stringify(v);
                    }
                }
            }
        }

        if (data != null && data != undefined && type != 'GET') {
            if (typeof (data) != 'string') {
                data = JSON.stringify(data);
            }
        }

        $.ajax({
            type: type,
            data: data,
            url: url,
            contentType: 'application/json',
            dataType: "json",
            // 允许携带证书
            //xhrFields: {
            //    withCredentials: true
            //},
            // 允许跨域
            crossDomain: true,
            headers: header,
            //beforeSend: function (request) {
            //    if (header) {
            //        for (var key in header) {
            //            request.setRequestHeader(key, header[key]);
            //        }
            //    }
            //},
            success: onSuc,
            error: function (XMLHttpRequest, textStatus, errorThrown) {
                //通常情况下textStatus和errorThrown只有其中一个包含信息
                //this为调用本次ajax请求时传递的options参数
                var apiRet = {
                    success: false, error: { errorMessage: XMLHttpRequest.responseText || 'http出错，请重试。', errorCode: XMLHttpRequest.status }
                }
                onSuc(apiRet);
            }
        });
    }

    scope.ajax = ajax;



    /**
     * 列表数据    
     */
    scope.ApiProvider = function () {
        var self = this;

        /**
         * 
         * @param {} param
         * @param {function(ApiRet){}} callback   {success:true,data:controllerConfig }
         */
        self.getControllerConfig = function (param, callback) {

            ajax({ action: 'getConfig', type: 'GET', data: {}, onSuc: callback });

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
            ajax({ action: 'getList', type: 'GET', data: param, onSuc: callback });
        };


        self.getModel = function (id, callback) {
            ajax({ action: 'getModel', type: 'GET', data: { id: id }, onSuc: callback });
        };

        /**
        * 
        * @param {any} model
        * @param {Function} callback  function(apiReturn){}
        */
        self.insert = function (model, callback) {
            ajax({ action: 'insert', type: 'POST', data: model, onSuc: callback });
        };

        /**
         * 
         * @param {any} model
         * @param {function(ApiRet){}} callback  function(apiRet){}
         */
        self.update = function (model, callback) {
            ajax({ action: 'update', type: 'PUT', data: model, onSuc: callback });
        };

        self.delete = function (id, callback) {
            ajax({ action: 'delete', type: 'DELETE', data: { id: id }, onSuc: callback });
        };

    };


})(autoTemp.dataProvider || (autoTemp.dataProvider = {}));