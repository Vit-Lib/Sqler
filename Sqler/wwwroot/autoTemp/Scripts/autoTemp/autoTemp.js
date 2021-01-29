/*
 * autoTemp 扩展  
 * Date   : 2020-01-09
 * Version: 2.0
 * author : Lith
 * email  : serset@yeah.net

 */ 
; (function (scope) {

    scope.createDataPrivider = function () {
        var dataProviderClass = document.url_GetCurArg('dataProvider') ||'ApiProvider';

        try {
            with (autoTemp.dataProvider) {
                var clazz = eval(dataProviderClass);
            }
            var dataProvider = new clazz();            
        } catch (e) {
        }

        //var dataProvider = new autoTemp.dataProvider.ApiProvider();
        return dataProvider;
    };    

})(autoTemp = {});



/**  扩展 document（动态加载 js 和 css，打开新窗口，获取url参数等）
 * Date  : 2018-08-02
 * author:Lith
 */
; (function (obj) {
 



    var toXmlStr = function toXmlStr(str) {
        /// <summary> 向xml转换。
        /// <para>例如 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）或 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中title的值。</para>  
        /// <para>转换     &amp; 双引号 &lt; &gt;     为      &amp;amp; &amp;quot; &amp;lt; &amp;gt;(注： 单引号 对应 &amp;apos; (&amp;#39;) ，但有些浏览器不支持，故此函数不转换。)</para>  
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return str.replace(/\&/g, "&amp;").replace(/\"/g, "&quot;").replace(/\</g, "&lt;").replace(/\>/g, "&gt;");
    };


    // var id=document.url_GetCurArg('id');
    obj.url_GetCurArg = function (key) {
        return obj.url_GetArg(location.search,key);
    }

    obj.url_GetArg = function (src, key) {
        /// <summary>获取当前src中的参数
        /// <para>demo： var jsName=document.url_GetArg("aaa.html?a=1&amp;b=2",'name');</para>
        /// </summary>
        /// <param name="src" type="string">例如:"?a=1&amp;b=2"</param>
        /// <param name="key" type="string">若不为字符串，则返回把所有参数做为键值对的对象。若为字符串，且获取不到，则返回 null</param>
        /// <returns type="string or object"></returns>

        if (arguments.length == 1) {
            key = src;
            src = location.search;
        }

        if ('string' == typeof key) {
            var v = (src.match(new RegExp("(?:\\?|&)" + key + "=(.*?)(?=&|$)")) || ['', null])[1];
            return v && decodeURIComponent(v);
        } else {
            var reg = /(?:\?|&)(.*?)=(.*?)(?=&|$)/g, temp, res = {};

            while ((temp = reg.exec(src)) != null)
                res[temp[1]] = decodeURIComponent(temp[2]);
            return res;
        }

        //var src = location.search;
        //if (src.length < 2 || src.charAt(0) != '?') {
        //    return null;
        //}
        //var params = src.substring(1).split('&');
        //var ps = null;
        //for (var i in params) {
        //    ps = params[i].split('=');
        //    if (decodeURIComponent(ps[0]) == name) {
        //        return decodeURIComponent(ps[1]);
        //    }
        //}


        //return null;
    };




    obj.script_getArg = function (key) {
        /// <summary>返回所在脚本src参数。
        /// <para>demo： var jsName=lith.document.script_getArg('name');</para>
        /// <para>不要在方法中调用此方法，否则可能始终获取的是最后一个js的文件的参数</para>
        /// </summary>
        /// <param name="key" type="string">若不为字符串，则返回把所有参数做为键值对的对象。若为字符串，且获取不到，则返回 null</param>



        ////假如上面的js是在这个js1.js的脚本中<script type="text/javascript" src="js1.js?a=abc&b=汉字&c=123"></script>
        var scripts = document.getElementsByTagName("script"),
            //因为当前dom加载时后面的script标签还未加载，所以最后一个就是当前的script
            script = scripts[scripts.length - 1],
            src = script.src;

        return obj.url_GetArg(src, key);
    };






    obj.openWin = function (html) {
        /// <summary>在新页面中显示html</summary>
        /// <param name="html" type="String">html 代码</param>
        /// <returns type="Window"></returns> 
        var oWin = window.open('');
        oWin.document.write(html);
        return oWin;
    };

    obj.openForm = function (param) {
        /// <summary>在新页面中新建Form，发送请求。
        /// <para> demo:lith.document.openForm({url:'http://www.a.com',reqParam:{a:3},type:'post'}); </para>
        /// </summary>
        /// <param name="param" type="object">
        /// <para> demo:{url:'http://www.a.com',reqParam:{a:3},type:'post'} </para>
        /// <para> url[string]:要打开的链接地址。</para>
        /// <para> reqParam[object]:请求参数。</para>
        /// <para> type[string]:请求方式。可为'get'、'post'、'put'等，不指定则为get。</para>           
        /// </param>
        /// <param name="url" type="string"></param>
        /// <param name="postParam" type="object">。</param>
        /// <param name="type" type="string"></param>
        /// <returns type="window"></returns>


        var html = '<!DOC' + 'TYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"><html xmlns="http://www.w3.org/1999/xhtml"><head ><meta http-equiv="Content-Type" content="text/html;charset=UTF-8" /><tit';
        html += 'le>请稍等</title> </head><body>';
        html += '<h3>请稍等 ...</h3>';
        html += '<form  accept-charset="UTF-8"  name="tempForm"  action="' + toXmlStr(param.url) + '" method="' + (param.type || "get") + '" style="display:none">';
        for (var name in param.reqParam) {
            html += '<input type="hidden" name="' + toXmlStr(name) + '" value="' + toXmlStr(param.reqParam[name]) + '"/>';
        }
        html += '</form>';
        html += '<script type="text/javascript">document.tempForm.submit();</sc' + 'ript>';
        html += '</body></html>';

        return obj.openWin(html);
    };


    
    obj.loadCss = function (cssSrc) {
        /// <summary>载入css文件。在文档加载过程中或已经加载完成后载入css文件。</summary>
        /// <param name="cssSrc" type="string">例如："/Scripts/jquery-easyui/themes/icon.css"</param>

        if (document.readyState == "loading") {
            loadCss_BeforeDocumentLoaded(cssSrc);
        } else {
            loadCss_AfterDocumentLoaded(cssSrc);
        }

        function loadCss_BeforeDocumentLoaded(cssSrc) {
            /// <summary>载入css文件。在文档加载过程中载入css文件。</summary>
            /// <param name="cssSrc" type="string">例如："/Scripts/jquery-easyui/themes/icon.css"</param>

            // <link rel="stylesheet" type="text/css" href="/Scripts/jquery-easyui/themes/icon.css" />
            document.write('<link rel="stylesheet" type="text/css" href="' + toXmlStr('' + cssSrc) + '" />');
        }


        function loadCss_BeforeDocumentLoaded(cssSrc) {
            /// <summary>载入css文件。在文档已经加载完成后载入css文件。</summary>
            /// <param name="cssSrc" type="string">例如："/Scripts/jquery-easyui/themes/icon.css"</param>

            var eCss = document.createElement('link');
            eCss.rel = 'Stylesheet';
            eCss.type = 'text/css';
            eCss.href = cssSrc;
            document.getElementsByTagName("head")[0].appendChild(eCss);
        }

    };





    obj.addCss = function (cssText) {
        /// <summary>添加新的CSS样式节点。demo: lith.document.addCss('.header{ background-color:#8f8;}');</summary>
        /// <param name="cssText" type="String"></param>

        var style = document.createElement('style');  //创建一个style元素              
        style.type = 'text/css'; //这里必须显示设置style元素的type属性为text/css，否则在ie中不起作用        

        var head = document.head || document.getElementsByTagName('head')[0]; //获取head元素
        head.appendChild(style); //把创建的style元素插入到head中  

        if (style.styleSheet) { //IE

            var func = function () {
                try {
                    //防止IE中stylesheet数量超过限制而发生错误
                    style.styleSheet.cssText = cssText;
                } catch (e) { }
            }
            //如果当前styleSheet还不能用，则放到异步中则行
            if (style.styleSheet.disabled) {
                setTimeout(func, 10);
            } else {
                func();
            }
        } else { //w3c
            //w3c浏览器中只要创建文本节点插入到style元素中就行了
            var textNode = document.createTextNode(cssText);
            style.appendChild(textNode);
        }
    };




})(document);





/**  扩展 localStorage 客户端存储
 * Date  : 2019-04-17
 * author:Lith
 */
; (function (scope) {



    scope.__proto__.setValue = function (key, value, expireSeconds) {
        /// <summary>存储值到对应的key中</summary>
        /// <param name="name" type="string">索引码或者名称,需要唯一.</param>
        /// <param name="value" type="string">具体的内容值</param>
        /// <param name="expireSeconds" type="int">过期秒数,不传则永不失效</param>

        var data = { value: value };

        if (expireSeconds && expireSeconds > 0) {
            data.expireTime = new Date().addSecond(expireSeconds).getTime();
        }

        try {
            localStorage.setItem(key, JSON.stringify(data));
        } catch (e) {

        }
    }

    scope.__proto__.getValue = function (key) {
        /// <summary>根据对应的key返回对应值,找不到返回null</summary>
        /// <param name="key" type="string">索引码或者名称,需要唯一.</param>
        try {

            var data = localStorage.getItem(key);
            if (!data) return null;

            data = JSON.parse(data);

            if (data.expireTime) {
                if (data.expireTime < new Date().getTime()) {
                    localStorage.removeItem(key);
                    return null;
                }
            }
            return data.value;
        } catch (e) {
            return null;
        }
    }

    scope.__proto__.deleteValue = function (key) {
        /// <summary>根据对应的key删除值</summary>
        /// <param name="name" type="string">索引码或者名称,需要唯一.</param>
        localStorage.removeItem(key);
    }

})(localStorage);



/**  扩展 String
 * 说明  : 对String类的prototype和类扩充函数： toJsonStr、toJsStr、toXmlStr、decodeXmlStr、html2Text、isNotStr、trim、lTrim、rTrim
 * Date  : 2017-09-22
 * author: Lith
 */
; (function (String) {

    //String的去除空格函数
    String.prototype.trim = function () { return this.replace(/(^\s*)|(\s*$)/g, ""); };
    String.prototype.lTrim = function () { return this.replace(/(^\s*)/g, ""); };
    String.prototype.rTrim = function () { return this.replace(/(\s*$)/g, ""); };




    String.prototype.toJsonStr = function () {
        /// <summary> 向json键值对数据的字符串型值 转换。 
        /// <para>例如 转换为javascript 代码  var oJson={"name":"ff"};  中json对象的name属性所对应的值（以双引号包围）。</para> 
        /// <para>转换   \b \t \n \f \r \" \\ 为 \\b \\t \\n \\f \\r \\" \\\\</para>
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return this.replace(/\\/g, "\\\\").replace(/\x08/g, "\\b").replace(/\t/g, "\\t").replace(/\n/g, "\\n").replace(/\f/g, "\\f").replace(/\r/g, "\\r").replace(/\"/g, "\\\"");
    };




    String.prototype.toJsStr = function () {
        /// <summary> 向javascript的字符串转换。
        /// <para>例如转换为javascript 代码  var str="";  中str对象所赋的值（以引号包围）。 </para>   
        /// <para>转换   \b \t \n \f \r \" \' \\ 为 \\b \\t \\n \\f \\r \\" \\' \\\\        </para>
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return this.replace(/\\/g, "\\\\").replace(/\x08/g, "\\b").replace(/\t/g, "\\t").replace(/\n/g, "\\n").replace(/\f/g, "\\f").replace(/\r/g, "\\r").replace(/\"/g, "\\\"").replace(/\'/g, "\\\'");
    };



    String.prototype.toXmlStr = function () {
        /// <summary> 向xml转换。
        /// <para>例如 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）或 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中title的值。</para>  
        /// <para>转换     &amp; 双引号 &lt; &gt;     为      &amp;amp; &amp;quot; &amp;lt; &amp;gt;(注： 单引号 对应 &amp;apos; (&amp;#39;) ，但有些浏览器不支持，故此函数不转换。)</para>  
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return this.replace(/\&/g, "&amp;").replace(/\"/g, "&quot;").replace(/\</g, "&lt;").replace(/\>/g, "&gt;");
    };

    String.prototype.decodeXmlStr = function () {
        /// <summary> xml属性字符串反向转换（与toXmlStr对应）。
        /// <para>例如 反向转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）或 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中title的值。</para>    
        /// <para>转换  &amp;amp;  &amp;quot;  &amp;lt;  &amp;gt; 为 &quot; &amp; &lt; &gt; (注： 单引号 对应 &amp;apos; (&amp;#39;) ，但有些浏览器不支持，故此函数不转换。)</para>
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return this.replace(/\&amp\;/g, "&").replace(/\&quot\;/g, "\"").replace(/\&lt\;/g, "<").replace(/\&gt\;/g, ">");
    };


    String.prototype.html2Text = function () {
        /// <summary> 清除Html格式。例如 ： 转换   "&lt;br/&gt;aa&lt;p&gt;ssfa&lt;/p&gt;" 为 "aassfa" <summary>          
        /// <returns type="string">转换后的字符串</returns>
        return this.replace(/<[^>].*?>/g, "");
    };



    function isNotStr(str) {
        return null == str || undefined == str;
    }

    String.isNotStr = isNotStr;

    String.trim = function (str) { return isNotStr(str) ? '' : ('' + str).trim(); };
    String.lTrim = function (str) { return isNotStr(str) ? '' : ('' + str).lTrim(); };
    String.rTrim = function (str) { return isNotStr(str) ? '' : ('' + str).rTrim(); };
    String.toJsonStr = function (str) {
        /// <summary> 向json键值对数据的字符串型值 转换。 
        /// <para>例如 转换为javascript 代码  var oJson={"name":"ff"};  中json对象的name属性所对应的值（以双引号包围）。</para> 
        /// <para>转换   \b \t \n \f \r \" \\ 为 \\b \\t \\n \\f \\r \\" \\\\</para>
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return isNotStr(str) ? '' : ('' + str).toJsonStr();
    };
    String.toJsStr = function (str) {
        /// <summary> 向javascript的字符串转换。
        /// <para>例如转换为javascript 代码  var str="";  中str对象所赋的值（以引号包围）。 </para>   
        /// <para>转换   \b \t \n \f \r \" \' \\ 为 \\b \\t \\n \\f \\r \\" \\' \\\\        </para>
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return isNotStr(str) ? '' : ('' + str).toJsStr();
    };
    String.toXmlStr = function (str) {
        /// <summary> 向xml转换。
        /// <para>例如 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）或 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中title的值。</para>  
        /// <para>转换     &amp; 双引号 &lt; &gt;     为      &amp;amp; &amp;quot; &amp;lt; &amp;gt;(注： 单引号 对应 &amp;apos; (&amp;#39;) ，但有些浏览器不支持，故此函数不转换。)</para>  
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return isNotStr(str) ? '' : ('' + str).toXmlStr();
    };

    String.decodeXmlStr = function (str) {
        /// <summary> xml属性字符串反向转换（与toXmlStr对应）。
        /// <para>例如 反向转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）或 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中title的值。</para>    
        /// <para>转换  &amp;amp;  &amp;quot;  &amp;lt;  &amp;gt; 为 &quot; &amp; &lt; &gt; (注： 单引号 对应 &amp;apos; (&amp;#39;) ，但有些浏览器不支持，故此函数不转换。)</para>
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return isNotStr(str) ? '' : ('' + str).decodeXmlStr();
    };
    String.html2Text = function (str) {
        /// <summary> 清除Html格式。例如 ： 转换   "&lt;br/&gt;aa&lt;p&gt;ssfa&lt;/p&gt;" 为 "aassfa" <summary>          
        /// <returns type="string">转换后的字符串</returns>
        return isNotStr(str) ? '' : ('' + str).html2Text();
    };



})(String);
 