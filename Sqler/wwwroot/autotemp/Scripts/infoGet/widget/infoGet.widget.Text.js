
/*
* infoGet 扩展 
* Date  : 2020-02-19
* author:lith
 

 
*/


/*
    (1).初始化属性
    ig-param 数据格式:
        width(int): 控件的宽度(默认150)。
        height(int): 控件的高度(默认22)。
      

        value(string or other): 默认值。如果为时间框,并且默认值为"getdate()",则初始时会转换成当前时间
        mode(string):  模式，为edit或show,默认为edit
        disabled(bool): 控件是否禁用,禁用为"true",否则为"false"(默认false)。



     (2).对象属性 
     [属性]
     (x.0)  igId,igParam,disabled

     [方法]
     (x.1)  event_Set(eventName, funcEvent) 
     (x.2)  event_Add(eventName, funcEvent)

     (x.3)  init() 
     (x.4)  getValue(),setValue(value)  
     (x.5)  enable(),disable() 
    
     (x.8)  resize(width, height)
     (x.9)  setMode(mode) 
 

   (3).事件
     (x.1)  onSetValue :  function (value) { };
            在值变更时触发


   (4).Demo

 */

; if (typeof (infoGet) == 'undefined') infoGet = {};
if (!infoGet.widget) infoGet.widget = {};

; (function (scope) {

    var className = 'Text';

    

    var Widget = function (jeElem, igAttr) {
        var self = this;
 

        self.je = jeElem;
        var jeEdit = jeElem;
        var jeShow;


        self.mode = 'edit';
        self.disabled = false;


        self.init = function () {
            jeEdit.addClass('ig_mode_edit').addClass('ig_Text');
            var igParam = igAttr['ig-param'];
            if (!igParam) return;
            if (igParam.disabled === true) {
                self.disable();
            }
        };
    

        //修改模式
        //mode 显示模式，值可为 'show'  'edit'(默认) 
        self.setMode = function (mode) {
            if (self.mode == mode) return;

            var value = self.getValue();

            self.mode = mode;
            if (self.mode == 'edit') {
                jeEdit.show();
                jeShow.hide();

                self.je = jeEdit;
            } else {
                if (!jeShow) {
                    jeShow = $('<span class="ig_mode_show ig_Text"  /> ');
                    jeEdit.after(jeShow);
                }

                jeShow.show();
                jeEdit.hide();

                self.je = jeShow;
            }

            self.setValue(value);
        };


      
     

        //禁用控件
        self.disable = function () {          
            if (self.mode == 'edit') {
                jeEdit[0].disabled = 'disabled';
                self.disabled = true;
            }
           
        };

        //启用控件
        self.enable = function () {
            if (self.mode == 'edit') {
                jeEdit[0].disabled = '';
                self.disabled = false;
            }            
        };




        self.getValue = function () {
            if (self.mode == 'edit') {
                return jeEdit.val();
            } else {
                return jeShow.text();
            }            
        };


        self.setValue = function (value) {
            if (self.mode == 'edit') {
                return jeEdit.val(value);
            } else {
                return jeShow.text(value);
            }            
        };


        //重置大小
        self.resize = function (width, height) {            
            if (width) jeEdit.width(width);
            if (height) jeEdit.height(height);
        }      
 


        self.init();


    }
     

    //构建html控件字符串
    Widget.buildHtml = function (igAttr) {        

        var html = '<input type="text" ';      


        //html += ' style="width:' + width + 'px;height:' + height + 'px;" ';
        var igParam = igAttr['ig-param'];
        if (igParam) {
            var style = '';
            if (igParam.width) style += 'width:' + igParam.width + 'px;';

            if (igParam.height) style += 'height:' + igParam.height + 'px;';

            if (style) html += ' style="' + style + '"';
        }
       
        

        html += infoGet.getHtmlCtrl_attr(igAttr);
        html += ' />';
        return html;

    };


 
    Widget.className = className;
    scope[className] = Widget;

 

})(infoGet.widget);