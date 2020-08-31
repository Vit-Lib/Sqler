/*
 * Date   : 2020-08-31
 * Version: 2.0.3
 * author : Lith
 * email  : sersms@163.com

 */
; (function (theme) {
     

   

    theme.show = function (options) {
        /// <summary>在屏幕右下角显示一条消息窗口。该选项参数是一个可配置的对象</summary>      
        return $.messager.show(options);
    };


    function alert(title, msg, icon, fn) {
         
        return getTopMessager().alert(title, msg, icon, fn);
 
    }


    

 
    theme.alert = function (title, msg, icon, fn) {
        /// <signature>
        /// <summary>显示警告窗口。
        /// demo: theme.alert('请输入数据'); 
        /// </summary>    
        /// <param name="msg" type="string">显示的消息</param>
        /// </signature>

        /// <signature>
        /// <summary>显示警告窗口。
        /// demo: theme.alert('请输入数据',function(){alert('点击了Ok');}); 
        /// </summary>    
        /// <param name="msg" type="string">显示的消息</param>
        /// <param name="fn" type="function">在窗口关闭的时候触发该回调函数。  </param>
        /// </signature>

        /// <signature>
        /// <summary>显示警告窗口。
        /// demo: theme.alert('提示', '请输入数据', 'info'，function(){alert('点击了Ok');});
        /// </summary>       
        /// <param name="title" type="string">在头部面板显示的标题</param>
        /// <param name="msg" type="string">显示的消息</param>
        /// <param name="icon" type="string">显示的图标图像。可用值有：error,question,info,warning。</param>
        /// <param name="fn" type="function">在窗口关闭的时候触发该回调函数 </param>
        /// </signature>

        if (arguments.length < 2) {
            // theme.alert('msg');
            return alert('提示', title?title:'', 'info');
        }
        else if (2 == arguments.length && 'function' == typeof (arguments[1])) {
            // theme.alert('msg',function(){});
            return alert('提示', title ? title : '', 'info', arguments[1]);
        }
        return alert(title, msg, icon, fn);
    };

    /**
     * 返回api是否成功
     * @param {any} apiRet
     * @param {any} sucMsg
     * @param {any} failMsg
     */
    theme.alertApiReturn = function (apiRet,sucMsg,failMsg) {
        

        if (apiRet && apiRet.success) {
            if (sucMsg) theme.tips(sucMsg);
            return true;
        }

        if (!failMsg) failMsg = '操作失败。';

        if ('string' == typeof (apiRet)) {
            failMsg += '<br/>原因：' + apiRet;
        } else if (apiRet && apiRet.error && apiRet.error.errorMessage) {
            failMsg += '<br/>原因：' + apiRet.error.errorMessage;
        }
        theme.alert(failMsg);
        return false;
    }

  

    function getTopMessager() {
        /// <summary>获取最顶层的jquery easy ui messager </summary>  
        var top = window.top;
        if (top.$ && top.$.messager) {
            return top.$.messager;
        }
        return $.messager;
    }

    theme.getTopMessager = getTopMessager;


    theme.confirmExt = function (title, msg, fn) {
        /// <signature>
        /// <summary>显示一个包含“确定”和“取消”按钮的确认消息窗口。theme.confirmExt('msg',fn);</summary>       
        /// <param name="msg" type="String">显示的消息文本。</param>
        /// <param name="fn" type="Function">fn(b): 当用户点击“确定”按钮的时侯将传递一个true值给回调函数，否则传递一个false值。 </param>
        /// </signature>

        /// <signature>
        /// <summary>显示一个包含“确定”和“取消”按钮的确认消息窗口。theme.confirmExt('title','msg',fn);</summary>       
        /// <param name="title" type="String">在头部面板显示的标题文本。</param>
        /// <param name="msg" type="String">显示的消息文本。</param>
        /// <param name="fn" type="Function">fn(b): 当用户点击“确定”按钮的时侯将传递一个true值给回调函数，否则传递一个false值。 </param>
        /// </signature>
 
        var messager = window.top.$.messager;
        //theme.confirmExt('msg',fn);
        if ('function' == typeof (arguments[1])) {
            return getTopMessager().confirm('', arguments[0], arguments[1]);
        }
        //theme.confirmExt('title','msg',fn);
        return getTopMessager().confirm(title, msg, fn);
    };


   function confirm(title, msg, fnYes,fnNo) {
        /// <summary>显示一个包含“确定”和“取消”按钮的确认消息窗口。</summary>       
        /// <param name="title" type="String">在头部面板显示的标题文本。</param>
        /// <param name="msg" type="String">显示的消息文本。</param>
        /// <param name="fnYes" type="Function">可不指定。当用户点击“确定”按钮的时侯回调。 </param>
       /// <param name="fnNo" type="Function">可不指定。当用户点击“取消”按钮的时侯回调。 </param>

        return theme.confirmExt(title, msg, function (b) {
            var fun = (b ? fnYes : fnNo);
            if (fun) fun();
       });

        
   }


   theme.confirm = function (title, msg, fnYes, fnNo) {
       /// <signature>
       /// <summary>显示一个包含“确定”和“取消”按钮的确认消息窗口。theme.confirm('msg',fnYes,fnNo); </summary>    
       /// <param name="msg" type="String">显示的消息文本。</param>
       /// <param name="fnYes" type="Function">可不指定。当用户点击“确定”按钮的时侯回调。 </param>
       /// <param name="fnNo" type="Function">可不指定。当用户点击“取消”按钮的时侯回调。 </param>
       /// </signature>

       /// <signature>
       /// <summary>显示一个包含“确定”和“取消”按钮的确认消息窗口。 theme.confirm('title','msg',fnYes,fnNo);</summary>       
       /// <param name="title" type="String">在头部面板显示的标题文本。</param>
       /// <param name="msg" type="String">显示的消息文本。</param>
       /// <param name="fnYes" type="Function">可不指定。当用户点击“确定”按钮的时侯回调。 </param>
       /// <param name="fnNo" type="Function">可不指定。当用户点击“取消”按钮的时侯回调。 </param>
       /// </signature>


        //theme.confirm('msg',fnYes,fnNo);
        if ('function' == typeof (arguments[1])) {
           return confirm('提示', arguments[0], arguments[1], arguments[2]);
        }

        //theme.confirm('title','msg',fnYes,fnNo);
        return confirm.apply(null, arguments);
    };




    theme.prompt = function (title, msg, fn) {
        /// <summary>显示一个包含“确定”和“取消”按钮的确认消息窗口。</summary>       
        /// <param name="title" type="String">在头部面板显示的标题文本。</param>
        /// <param name="msg" type="String">显示的消息文本。</param>
        /// <param name="fn" type="Function">fn(val): 在用户输入一个值参数的时候执行的回调函数。  </param>
        return getTopMessager().prompt(title, msg, fn);
    };

    /// 将窗口滚动条置顶
    /// par window对象
    theme.SetScrollToTop = function (par) {
        if (par != null && par != undefined)
        {
            par.scrollTo(0, 0);
            if (par.parent != null && par.parent != undefined && par.window != window.top)
                theme.SetScrollToTop(par.parent);
        }
    }

    theme.popDialog = function (url, title, width, height, dialogParam,popInTop) {
        /// <summary>弹出对话框</summary>
        /// <param name="url" type="string"></param>
        /// <param name="title" type="string"></param>
        /// <param name="width" type="string">例如： '550px'</param>
        /// <param name="height" type="string">例如： '550px'</param>
        /// <param name="dialogParam" type="object">其他参数，例如 {content:'loading..'},可不指定</param>
        /// <param name="popInTop" type="bool">是否在最顶层弹出对话框</param>


        window.setTimeout(function () {
    
            if (!dialogParam) dialogParam = {};
            if (title) {
                dialogParam.title = title;
            }

            dialogParam.width = (width || dialogParam.width || '800px');

            if (height) {
                dialogParam.height = height;
            }
            //if (!dialogParam.top) {
            //    dialogParam.top = '160px';
            //}

            var dialog;

            //popInTop
            if (popInTop && window.top && window.top.art) {
                dialog = window.top.art.dialog;
            } else {
                dialog = art.dialog;
                //将滚动条置顶
                theme.SetScrollToTop(window);
            }			
 
            var myWindows = dialog.open(url, dialogParam);
            //打开窗口后强制移动到指定位置 
            //myWindows.config.top = '160px';

        }, 0);
    };


    theme.tips = function (msg,title) {
        /// <summary> tips </summary>       
        /// <param name="msg" type="String">显示的消息文本。</param>
        /// <param name="title" type="String">在头部面板显示的标题文本。</param>

        var param={
            title:title?title:'提示',
            msg: msg,
            timeout:5000,
            showType:'slide'
        };
        return getTopMessager().show(param);
    };

    theme.progressStart = function (msg, isTop,hasProgress) {
        var msger = theme.progressStop(isTop);
        if (hasProgress) {
            return msger.progress({ title: '请稍侯...', msg: msg, interval: 0});
        }
        return msger.progress({ title: '请稍侯...', msg: msg, text: '请稍侯...' });   
    };

    theme.progressStop = function (isTop) {
        var msger = (isTop ? getTopMessager() : $.messager);
        msger.progress('close');
        return msger;
    };

    theme.progressValue = function (progressValue, isTop) {
        var msger = (isTop ? getTopMessager() : $.messager);
        var bar = msger.progress('bar');
        bar.progressbar('setValue', progressValue);
    };

    theme.progressText = function (progressText, isTop) {
        var msger = (isTop ? getTopMessager() : $.messager);
        var bar = msger.progress('bar');
        bar.progressbar('options').text = progressText;
    };

   



    theme.try = function (func, msg) {
        /// <summary>在try 语句块中执行函数func 若出错，则alert 错误原因。
        /// demo: theme.try(function(){},'升级出错。'); 
        /// </summary>    
        /// <param name="func" type="function">要执行的函数</param>
        /// <param name="msg" type="string">出错时提示的消息前缀</param>
        /// <returns type="bool">是否没出错</returns>
        try {
            func();
            return true;
        } catch (e) {
            theme.alert((msg || '') + (e.message || ''));
            return false;
        }
    };


    theme.addTab = function (url, title, funcOnClose) {
        /// <summary>添加tab页面(若没有tab页面，则 window.open)</summary>
        /// <param name="url" type="string">要打开的页面的url</param>
        /// <param name="title" type="string">添加的tab页的title的前缀</param>
        /// <param name="funcOnClose" type="function">当tab页关闭时调用的回调函数</param>
        /// <returns type="bool">true:调用 window.open新建窗口，false:调用parent.addTab添加tab页面</returns>
        if (parent && parent.addTab) {
            parent.addTab(title, url, funcOnClose);
            return;
        }
        window.open(url);
    };

 
    theme.closeWin = function () {
        /// <summary>关闭当前页面（tab页 或 artDialog 或 window.close()）</summary>

        // 当前页面为 art.dialog 弹窗
        if (window.parent && window.parent.art && window.parent.art.dialog) {
            var list = window.parent.art.dialog.list;
            for (var t in list) {
                var win = list[t];              
                if (win.iframe && win.iframe.contentWindow == window.self) {
                    win.close();
                    return;
                }
            }
        }

        //if ('undefined' != typeof (art) && art.dialog && art.dialog.opener && art.dialog.opener != window) {
        //    // 当前页面为 art.dialog 弹窗
        //    art.dialog.close();
        //    return;
        //}


         // 当前页面为 AutoTabs控件的 tab页
        if (window.top != window.self && window.top.closeTab) {           
            window.top.closeTab();
            return;
        }


        //直接关闭当前页面

        //window.location.href = "about:blank";
        window.opener = null;
        window.open("", "_self");
        window.close();

    };


 

  

})(window.theme || (window.theme = {}));