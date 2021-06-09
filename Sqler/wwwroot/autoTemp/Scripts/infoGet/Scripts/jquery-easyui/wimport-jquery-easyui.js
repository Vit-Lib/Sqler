/*
* 导入jquery.easyui
*
*/
; lith.wimport({
    widgetName: 'jquery.easyui'
    , depends: [
        //导入jquery
        { type: 'js', src: '/jquery/wimport-jquery.js' }
    ]
    , files: [
        { type: 'css', src: 'themes/default/easyui.css' },
        { type: 'css', src: 'themes/icon.css' },
        { type: 'js', src: 'jquery.easyui.min.js' }
    ]
});

 


