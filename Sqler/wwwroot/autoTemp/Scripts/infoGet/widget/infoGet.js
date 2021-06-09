
/*
* infoGet 扩展 
* Date  : 2020-02-20
* author:lith

    
 */

/* 
    IgAttr数据格式DEMO为:
    var demo =  {      'ig-class': 'Text', 
                       'ig-id': '名称',   'ig-param': {xx:'xx'}    };

 
*/


; (function (infoGet) {
  

    /// <param name="errorList" type="Array">存放错误列表</param>
    infoGet.errorList = [];
     

    

    function WidgetCache() {
        // dom  -> widget
        var widgetMap = new Map();
   
        this.clear = function () {
            widgetMap.clear();
        };

        this.getWidgetByElem = function (elem) {
            return widgetMap.get(elem)
        };
        this.removeWidget = function (elem) {
            widgetMap.delete(elem);
        }
        this.addWidget = function (elem, widget) {
            widgetMap.set(elem, widget);
        }
        //callback:  function(widget,elem){}
        this.each = function (callback) {
            widgetMap.forEach(callback);
        };
    }

    var widgetCache =  new WidgetCache();
     

    //infoGet.getElems = function () {
    //    return $('*[ig-class]');
    //};


    infoGet.setMode = function (mode) {
        widgetCache.each(function (widget, elem) {
            widget.setMode(mode);
        });
    }

    infoGet.disable = function () {
        widgetCache.each(function (widget, elem) {
            widget.disable();
        });
    }
 

    //获取Widget控件
    infoGet.getWidget = function (elem) {
 
        if (!elem) return null;      
    
        var widget = widgetCache.getWidgetByElem(elem);
        if (!widget) {
            var je = $(elem);
            var igAttr = getIgAttrFromJe(je);
            var className = igAttr['ig-class'];
            with (infoGet.widget) {
                var widgetClass = eval(className);
            }
            if ('function' != typeof (widgetClass)) return null;

            widget = new widgetClass(je, igAttr);
            widgetCache.addWidget(elem, widget);
        }
        return widget;
    }
 

    //通过igId获取控件
    infoGet.getWidgetByIgId = function (igId) { 
        return infoGet.getWidget ($('*[ig-id="' + (""+igId).toJsStr() + '"]')[0]);
    };



    infoGet.parse = function (jqParent) {
        var jqElems = (jqParent || $(document)).find('*[ig-class]');

        jqElems.each(function (i, elem) {
            infoGet.getWidget(elem);
        });
    };


    infoGet.buildHtml = function (igAttr) {

        var className = igAttr['ig-class'];
        with (infoGet.widget) {
            var widgetClass = eval(className);
        }
        if ('function' != typeof (widgetClass)) return null;
        return widgetClass.buildHtml(igAttr);
    }


    //获取dom控件ig参数
    function getIgAttrFromJe(je) {        
        function getAttrEval(je, attrName) {
            try {
                var value = je.attr(attrName);
                return eval('(' + value + ')');
            } catch (e) {
                infoGet.errorList.push(e);
            }
            return null;
        }

        var igAttr = {
            'ig-class': je.attr('ig-class'),
            'ig-id': je.attr('ig-id'),
            'ig-param': getAttrEval(je, 'ig-param')            
        };    
         
        return igAttr;
    };


    /// <field name='toXmlStr' type='fucntion'>向xml转换</field>
    function toXmlMinStr(str) {
        /// <summary> 向xml转换。
        /// 例如 转换  <a title=''>ok</a>  中title的值。    
        /// 转换     &  < > 单引号     为      &amp;  &lt; &gt; &#39;
        /// </summary>          
        /// <returns type="String">转换后的字符串</returns>
        return str.replace(/\&/g, "&amp;").replace(/\</g, "&lt;").replace(/\>/g, "&gt;").replace(/\'/g, "&#39;");
    };

    infoGet.getHtmlCtrl_attr = function (igAttr) {
         
        var html = '';
        for (var name in igAttr) {
            var item = igAttr[name];
            if (!item) continue;
            html += ' ' + name + '=\'';
            html += toXmlMinStr('object' == typeof (item) ? JSON.stringify(item) : ('' + item)); 
            html += '\' ';
        }
        return html;
    };   
     
   
 
   

 

})(window.infoGet || (window.infoGet = {}));