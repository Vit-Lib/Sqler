/*
 * autoTemp 扩展  
 * Date   : 2020-01-09
 * Version: 2.0
 * author : Lith
 * email  : sersms@163.com

 */ 
; (function (scope) {


    function getOptHtml(row, controller) {
        var html = '';
        var id = row[controller.controllerConfig.idField];
        for (var t in controller.rowButtons) {
            var item = controller.rowButtons[t];
            html += " <a  href=\"javascript: controller.rowButtons['" + t + "'].onclick('" + id + "');\" >" + String.toXmlStr(item.text) + "</a> ";
        }
        return html;
    }


    function buildGridConfig(controller) {

        //self.gridConfig = {
        //    "title":"xxxx",
        //    "idField":"id",
        //    "treeField":"name",  //若指定 则代表为树形控件
        //    "toolbar":[   {text:'刷新',  iconCls:'icon-reload',  handler:function(){}   },   '-' ]
        //
        //    "frozenColumns": [[
        //        //{ field: 'ck', checkbox: true },
        //        { field: 'username', title: '装修商', align: "center", width: 250, formatter: outSpan, sortable: true }

        //    ]],
        //    "columns": [[
        //        { field: 'askCount', title: '询单数', align: "center", width: 80, formatter: outSpan, sortable: true }
        //    ]]
        //};


        var controllerConfig = controller.controllerConfig;

        var gridConfig = {};

        //(x.1)idField treeField
        gridConfig.idField = controllerConfig.idField;
        gridConfig.treeField = controllerConfig.treeField;
   

        //(x.2)title
        if (controllerConfig.list) {
            gridConfig.title = controllerConfig.list.title;
        }


        //(x.3)toolbar (from buttons)
        if (controllerConfig.list && controllerConfig.list.buttons) {

            var toolbar = gridConfig.toolbar = [];

            var buttons = controllerConfig.list.buttons;
            for (var t in buttons) {
                buildButton(buttons[t]);
            }


            function buildButton(button) {
                toolbar.push({
                    text: button.text, handler: function () {

                        //(x.1)执行js
                        if (button.handler) {
                            //button    {text:'执行js',    handler:'function(callback){  setTimeout(callback,5000); }'    },

                            if (typeof (button.handler) == 'string') {
                                button.handler = eval('(' + button.handler + ')');
                            }
                            theme.progressStart(button.text);                         
                       
                            button.handler(theme.progressStop);
                            return;
                        }


                        //(x.2)调用接口
                        if (button.ajax) {
                            //button   {text:'调用接口',  ajax:{ type:'GET',url:'/autoTemp/{template}/getConfig'    }     }

                            theme.confirm(button.text + '?', function () {

                                autoTemp.dataProvider.ajax({
                                    url: button.ajax.url,
                                    type: button.ajax.type || 'GET',
                                    //header: {},
                                    //data: {},
                                    onSuc: function (apiRet) {
                                        theme.progressStop();
                                        if (theme.alertApiReturn(apiRet, '操作成功')) {
                                            location.reload();
                                        }
                                    }
                                });

                                theme.progressStart(button.text);

                            });
                        }
                    }
                });
            }
        }


        //(x.4)row button
        if (controllerConfig.list && controllerConfig.list.rowButtons) {
            var rowButtons = controllerConfig.list.rowButtons;
            $.each(rowButtons, function (i, button) {

                controller.addRowButton({
                    text: button.text,
                    onclick: function (id) {
                        //(x.1)执行js
                        if (button.handler) {
                            //button   {text:'查看id',    handler:'function(callback,id){  callback();alert(id); }'    },

                            if (typeof (button.handler) == 'string') {
                                button.handler = eval('(' + button.handler + ')');
                            }

                            theme.progressStart(button.text);
                            button.handler(theme.progressStop, id);
                            return;
                        }


                        //(x.2)调用接口
                        if (button.ajax) {
                            //button   {text:'调用接口',  ajax:{ type:'GET',url:'/autoTemp/{template}/getConfig?name={id}'    }     }

                            theme.confirm(button.text + '?', function () {

                                autoTemp.dataProvider.ajax({
                                    url: button.ajax.url.replace('{id}', id),
                                    type: button.ajax.type || 'GET',
                                    //header: {},
                                    //data: {},
                                    onSuc: function (apiRet) {
                                        theme.progressStop();
                                        theme.alertApiReturn(apiRet, '操作成功');
                                    }
                                });

                                theme.progressStart(button.text);

                            });
                        }
 
                    }
                });
            });
        }


        //(x.5)columns
        if (controller.atFields) {
            var atFields = controller.atFields;
            var frozenColumns = [
                //{ field: 'ck', checkbox: true }
            ];
            var columns = [];
            //(x.x.1)column
            gridConfig.frozenColumns = [frozenColumns];
            gridConfig.columns = [columns];
            for (var t in atFields) {
                var atField = atFields[t];
                var item = atField.list_init();
                if (item) columns.push(item);
            }

            //(x.x.2)opt column
            frozenColumns.push({
                field: 'opt', title: '操作', width: 250, align: "center", formatter: function (value, row, index) {
                    return getOptHtml(row, controller);
                }
            });
        }


        return gridConfig;
    }



    scope.Controller = function (controllerConfig) {
        var self = this;

        //(x.1) fire event list.controller.beforeCreate
        autoTemp.eventListener.fireEvent('list.controller.beforeCreate', [self, controllerConfig]);

        self.controllerConfig = controllerConfig;

        self.rowButtons = [ ]


        self.addRowButton = function (btnConfig) {
            self.rowButtons.push(btnConfig);
        };     

        self.getPermit = function (opt) {
            if (!self.controllerConfig.permit) return true;
            var value = self.controllerConfig.permit[opt] ;
            return value != false && value != 'false';           
        };


        self.list_init = function () {           

            if (!controllerConfig.idField) controllerConfig.idField = 'id';
            if (!controllerConfig.pidField) controllerConfig.pidField = 'pid';


            //(x.2)添加row-opt 查看
            if (self.getPermit('show')) {
                self.addRowButton({
                    text: '查看',
                    onclick: function (id) {
                        var search = location.search;
                        search += search ? '&' : '?';
                        search += 'mode=show&id=' + id;
                        theme.popDialog('item.html' + search, '查看');
                    }
                });
            }

            //(x.3)添加row-opt 修改
            if (self.getPermit('update')) {
                self.addRowButton({
                    text: '修改',
                    onclick: function (id) {
                        var dialogParam = {
                            init: function () {
                                var iframe = this.iframe.contentWindow;
                                iframe.window.event_afterSave = function () { template.refreshAfterChange(id); };
                            }
                        };

                        var search = location.search;
                        search += search ? '&' : '?';
                        search += 'mode=update&id=' + id;
                        theme.popDialog('item.html' + search, '修改', null, null, dialogParam);
                    }
                });
            }

            //(x.4)添加row-opt 删除
            if (self.getPermit('delete')) {
                self.addRowButton({
                    text: '删除',
                    onclick: function (id) {

                        theme.confirm('删除之后不可还原，确认继续吗？', function () {
                            theme.progressStart('删除');
                            dataProvider.delete(id, function (apiRet) {
                                theme.progressStop();
                                if (theme.alertApiReturn(apiRet, '删除成功')) {
                                    template.refreshAfterChange(id);
                                }
                            });
                        });
                    }
                });
            }

            //(x.5)创建 atFields
            self.atFields = scope.Controller.createAtFields(self.controllerConfig.fields);

            //(x.6)buildGridConfig
            return buildGridConfig(self);
        };
    };




    scope.Controller.createAtFields = function createAtFields(fieldConfigs) {
        if (!fieldConfigs || fieldConfigs.length == 0) return [];

        var atFields = [];
        for (var t in fieldConfigs) {
            var config = fieldConfigs[t];
            try {
                var field = new autoTemp.Field(config);
                atFields.push(field);
            } catch (e) {
            }
        }
        return atFields;
    };


})(autoTemp);