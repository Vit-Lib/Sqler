
/*
 * 常用 扩展 
 * Date  : 2020-01-14
 * author:lith
 

 */

; (function (ui) {


    function toXmlStr(str) {
        /// <summary> 向xml转换。
        /// <para>例如 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中a标签的内容体（innerHTML）或 转换  &lt;a title=&quot;&quot;&gt;ok&lt;/a&gt;  中title的值。</para>  
        /// <para>转换     &amp; 双引号 &lt; &gt;     为      &amp;amp; &amp;quot; &amp;lt; &amp;gt;(注： 单引号 对应 &amp;apos; (&amp;#39;) ，但有些浏览器不支持，故此函数不转换。)</para>  
        /// </summary>          
        /// <returns type="string">转换后的字符串</returns>
        return str.replace(/\&/g, "&amp;").replace(/\"/g, "&quot;").replace(/\</g, "&lt;").replace(/\>/g, "&gt;");
    };


    ui.EasyTab = function (jqTab,tabsParam) {

        var self = this;

        this.jqElem = jqTab;

        /// <field name='tabs' type='function'>调用jq easy 的api</field>
        this.tabs = function () {
            return jqTab.tabs.apply(jqTab, arguments);
        };
 

        /// <field name='tabFuncOnClose' type='function json'>存放当tab页面被关闭时调用的回调函数。以 title、function 的形式存储。</field>
        var tabFuncOnClose = {};

        //初始化tab控件
        (function (jqTab, tabsParam) {
            var p = {
                onClose: function (title, index) {                
                    var func = tabFuncOnClose[title];
                    if (func) {
                        func(title, index);
                        delete tabFuncOnClose[title];
                    }
                }
            };
            if (tabsParam) $.extend(p, tabsParam);                         
            jqTab.tabs(p);
            jqTab.addClass('luAutoTab');
        })(jqTab, tabsParam);
       
          

        /// <field name='addTab' type='function'>添加一个tab页面</field>
        this.addTab = function (title, url, tabParam) {
            /// <summary>添加一个tab页面</summary>
            /// <param name="title" type="string">为html</param>
            /// <param name="url" type="string">请求页面的url</param>
            /// <param name="tabParam" type="Object">tab参数。可不指定。若其存在onClose，则在tab关闭时会调用。</param>            

            var content = '<iframe src="' + toXmlStr(url.toString()) + '" style="width:100%;height:inherit;border:0px;"></iframe>';
            return this.addTabWithContent(title, content, tabParam);
        }

        /// <field name='addTabNoOverflow' type='function'>添加一个tab页面</field>
        this.addTabNoOverflow = function (title, url, tabParam) {
            /// <summary>添加一个tab页面</summary>
            /// <param name="title" type="string">为html</param>
            /// <param name="url" type="string">请求页面的url</param>
            /// <param name="tabParam" type="Object">tab参数。可不指定。若其存在onClose，则在tab关闭时会调用。</param>
            var content = '<iframe src="' + toXmlStr(url.toString()) + '" style="width:100%;height:100%;overflow: hidden;  padding-left: auto;padding-right: auto;"></iframe>';
            return this.addTabWithContent(title, content, tabParam);
        }

        /// <field name='addTab' type='function'>添加一个tab页面</field>
        this.addTabWithContent = function (title, content, tabParam) {
            /// <summary>添加一个tab页面</summary>
            /// <param name="title" type="string">为html</param>
            /// <param name="content" type="string">content</param>
            /// <param name="tabParam" type="Object">tab参数。可不指定。若其存在onClose，则在tab关闭时会调用。</param>

            if (jqTab.tabs('exists', title)) {
                jqTab.tabs('select', title);
            } else {
               
                var param={
                    title: title
                    , content: content
                    , closable: true
                };

                if (tabParam) {
                    $.extend(param, tabParam);
                    if ('function' == typeof (tabParam.onClose)) {
                        tabFuncOnClose[title] = tabParam.onClose;                      
                    }
                    delete param.onClose;
                }
                jqTab.tabs('add', param);
            }
        };


        this.getIframe = function (which) {
            /// <summary>获取iframe. 'which'参数可以是选项卡面板的标题或者索引。</summary>
            /// <returns type="jquery element"></returns> 
            return this.tabs('getTab', which).find('iframe:first');
        }

      

        this.switchTab = function (pos) {
            /// <summary>切换当前活动的tab页（pos为-2，选中在当前活动页面左侧第二个tab)</summary>
            var tab = jqTab.tabs('getSelected');
            var index = jqTab.tabs('getTabIndex', tab);
            jqTab.tabs('select', index + pos);
        };

      
        this.closeAllTabs = function () {
            /// <summary>关闭所有的(可关闭的)tab页面</summary>
            //var tabs = jqTab.tabs('tabs');
            //for (var t = tabs.length - 1; t >= 0; t--) {
            //    jqTab.tabs('close', t);
            //}
            jqTab.find('.tabs .tabs-close').click();
        };

      
        this.curTabClose = function () {
            /// <summary>关闭当前tab页面</summary>
            jqTab.find('.tabs .tabs-selected .tabs-close').click();
        };

      
        this.curTabRefresh = function () {
            /// <summary>重载当前tab页面</summary>
            self.curTabGo(0);
        };



 
        this.curTabGo = function (count) {
            /// <summary>前进后退或刷新当前tab页面</summary>
            /// <param name="count" type="int">window.history.go 的参数。0代表刷新，1代表前进，-1代表后退</param>

            var jqCurIframe = getCurJqIframe();

            if (jqCurIframe && jqCurIframe[0]) {
                //获取iframe的Dom对象
                var dom = jqCurIframe[0].contentWindow;
                dom.window.history.go(count);
            }
        }; 

        function getCurJqIframe() {
            /// <summary>获取当前tab页面的iframe对象</summary>
            /// <returns type="jq elem"></returns> 
            var tabs = jqTab.tabs('getSelected');
            var $iframe = $(tabs[0]).find('iframe:first');
            return $iframe;
        }

        this.getCurJqIframe = getCurJqIframe;

        /// <field name='getCurTabHeight' type='function'>获取当前tab页面的高度</field>
        this.getCurTabHeight = function () {
            /// <summary>获取当前tab页面的高度</summary>
            /// <returns type="number">当前tab页面的高度</returns> 

            var $iframe = getCurJqIframe();

            function getIframeHeight(iframe) {
                /// <summary>获取iframe的高度</summary>
                /// <param name="iframe" type="dom"></param>
                /// <returns type="number"></returns> 
                var win = iframe.contentWindow,
                doc = win.document,
                html = doc.documentElement,
                body = doc.body;
                // 获取高度 
                return Math.max(body.scrollHeight, body.offsetHeight, html.clientHeight, html.scrollHeight, html.offsetHeight);
            }
            var height = getIframeHeight($iframe[0]);
            return Math.max($(window).height(), height);
        };

         


 
    };


})((typeof ui) == 'undefined' ? ui = {}:ui);