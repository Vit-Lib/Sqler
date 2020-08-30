/*
 * Date   : 2020-08-30
 * Version: 1.0
 * author : Lith
 * email  : sersms@163.com
 */
;autoTemp.eventListener = new vit.EventListener();



/*
 
 'list.controller.beforeCreate'  :        回调  function (controller,controllerConfig) { }
 
 'list.template.beforeInit'  :        回调  function (template,templateConfig) { }

 




 

//添加事件
autoTemp.eventListener.addListener({
    eventType: 'list.template.beforeInit',
    listenerKey: 'listenerFromBe',
    handler: function (template,templateConfig) {
    }
});


//触发事件
autoTemp.eventListener.fireEvent('list.template.beforeInit', [template,templateConfig]);



//删除事件列表中相同listenerKey的事件
autoTemp.eventListener.removeEventByKey('listenerFromBe');



 */



