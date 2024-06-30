
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
        required(string): 控件是否必填,  'true': 必填; 'valueSetToDefaultIfInvalid':若为无效值则设为默认值；  其他(例如'false'):非必填（默认值）
        invalidMessage(string): 必填提示内容,为空则启用自定义提示消息 ，例如：'名字不允许为空!' 。
        disabled(bool): 控件是否禁用,禁用为"true",否则为"false"(默认false)。 
        desc(string): 控件描述。例如：'申请的金额总额，单位：万元。'。
        value(string or other): 默认值。如果为时间框,并且默认值为"getdate()",则初始时会转换成当前时间

        builderId: 构建器的id,用于再次构建时定位构建器    

        otherParam: 其他自定义属性存放。
        {
            type(string):文本框类型,可选值:"textbox","textarea","numberbox","password","datebox"(默认"textbox")。
            dateFmt(string):时间格式,例如:"yyyy-MM-dd","yyyy-MM-dd HH:mm:ss","yyyy-MM-dd HH:mm","HH:mm:ss"
            numberFmt(string):保留小数点位数.默认0
        }


     (2).对象属性 
     [属性]
     (x.0)  igId,igParam,disabled

     [方法]
     (x.1)  event_Set(eventName, funcEvent) 
     (x.2)  event_Add(eventName, funcEvent)

     (x.3)  init() 
     (x.4)  getValue(),setValue(value)  
     (x.5)  enable(),disable() 
     (x.6)  validate(howToReport) 
     (x.7)  getDefaultValue() 
     (x.8)  resize(width, height)
     (x.9)  setMode(mode) 
     (x.10) convertTextByValue(value, funcOnConvert) 

   (3).事件
     (x.1)  onSetValue :  function (value) { };
            在值变更时触发


   (4).Demo

 */

; if (typeof (infoGet) == 'undefined') infoGet = {};
if (!infoGet.widget) infoGet.widget = {};

; (function (scope) {

    var className = 'TextArea';

    

    var Widget = function (jeElem, igAttr) {
        var self = this;
 

        self.je = jeElem;
        var jeEdit = jeElem;
        var jeShow;


        self.mode = 'edit';
        self.disabled = false;


        self.init = function () {
            jeEdit.addClass('ig_mode_edit').addClass('ig_TextArea');
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
                    jeShow = $('<span class="ig_mode_show ig_TextArea"  /> ');
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

        var html = '<textarea  cols="40" rows="3" '; 
 

        //html += ' style="width:' + width + 'px;height:' + height + 'px;" ';
        var igParam = igAttr['ig-param'];
        if (igParam) {
            var style = '';
            if (igParam.width) style += 'width:' + igParam.width + 'px;';

            if (igParam.height) style += 'height:' + igParam.height + 'px;';

            if (style) html += ' style="' + style + '"';
        }


        html += infoGet.getHtmlCtrl_attr(igAttr);
        html += ' ></textarea>';
        return html;

    };
 
    Widget.className = className;
    scope[className] = Widget;

 

})(infoGet.widget);