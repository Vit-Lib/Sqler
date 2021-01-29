/*
 * autoTemp.Field 扩展
 * Date   : 2020-02-19
 * Version: 2.0
 * author : Lith
 * email  : serset@yeah.net
 */
; (function (scope) {   



    scope.Field = function (fieldConfig, controller) {
        //   field 对应 ig-id
        //  { field: 'name', title: '装修商', list_width: 200 ,visiable:false,editable:false ,'ig-class':'','ig-param':{}  }

        var self = this;

        self.config = fieldConfig;



        self.getVisiable = function () {
            return self.config.visiable != false && self.config.visiable != 'false';
        }



        self.getEditable = function () {
            var value = self.config.editable;
            return value != false && value != 'false';
        }




        self.setValue = function (value) {     
            var igWidget = getIgWidget();
            if (igWidget) igWidget.setValue(value);
        };



        self.getValue = function () {      
            var igWidget = getIgWidget();
            if (igWidget) return igWidget.getValue();
            return null;
        };



        self.list_init = function () {
            if (!self.getVisiable()) {
                return null;
            }
            //align: "center",
            return { field: self.config.field, title: self.config.title, width: self.config.list_width || 80, formatter: outSpan, sortable: true };
        }



        function getIgWidget() {
            if (!arguments.callee.igWidget) {
                arguments.callee.igWidget = infoGet.getWidgetByIgId(self.config.field);
            }
            return arguments.callee.igWidget;
        }

        self.build_infoGet_Html = function () {
            if (!self.getVisiable()) {
                return '';
            }        

            var html = "<li><table><tr><td class='mtbTitle'>";
            html += self.config.title;
            html += "</td><td class='mtbValue' >";

            //build ctrl
            var igAttr = { 'ig-id': self.config.field, 'ig-class': (self.config['ig-class'] || 'Text') };

            //ig-param
            var igParam = null;
            if (self.config['ig-param']) {
                igParam = self.config['ig-param'];
                if (typeof (igParam) == 'string') {
                    try {
                        igParam = eval('(' + igParam+')')
                    } catch (e) {
                    }
                }
            }

            if (!self.getEditable()) {
                if (!igParam) igParam = {};
                igParam.disabled=true;                
            }

            if (igParam) {
                igAttr['ig-param'] = igParam;
            }

            html += infoGet.buildHtml(igAttr);
            //html += "<input type='text'  id='" + domId+ "' class='ctl' />";

            html += "</td></tr></table></li>";
            return html;
        }

    };





    scope.Field.getFilter = function (filter, filterAtFields) {
        if (!filterAtFields || filterAtFields.length == 0) return;

        for (var t in filterAtFields) {
            var self = filterAtFields[t];
            var value = self.getValue();
            if (value) {
                filter.push({ field: self.config.field, opt: self.config.filterOpt || "=", value: value });
            }
        }
    };

    scope.Field.getValue = function (atFields, model) {
        if (!atFields || atFields.length == 0) return true;

        for (var t in atFields) {
            var self = atFields[t];

            if (!self.getVisiable() || !self.getEditable()) {
                delete model[self.config.field];
                continue;
            }

            var value = self.getValue();
            model[self.config.field] = value;
        }

        return true;
    };


    scope.Field.setValue = function (atFields, model) {
        if (!atFields || atFields.length == 0) return;

        for (var t in atFields) {
            var self = atFields[t];

            var value = model[self.config.field];
            self.setValue(value);
        }
    };


})(autoTemp);