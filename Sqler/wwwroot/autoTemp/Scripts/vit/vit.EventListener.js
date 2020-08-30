/*
 * vit.EventListener 事件监听器
 * Date   : 2020-08-19
 * Version: 1.1.3.7910
 * author : Lith
 * email  : sersms@163.com
 */
; (function (vit) {

 
    vit.EventListener = function () {
        var self = this;

        //item   {   eventType, handler, listenerKey:'listenFromBe'                  }
        var listenerList = [];

        //删除事件列表中相同listenerKey的事件
        self.removeEventByKey = function (listenerKey) {
            if (listenerKey) {
                listenerList = listenerList.filter(function (item, index) {
                    return item.listenerKey != listenerKey;
                });

                buildListenerMap();
            }
        }


        //eventListener.addListener({eventType:'mouse_onLeftClick' ,handler:function(){}, listenerKey:'listenFromBe'});
        self.addListener = function (listener) {
            //self.removeEventByKey(listener.listenerKey);
            listenerList.push(listener);

            buildListenerMap();
        }

        // funcName ->   [ listener1,listener2 ]
        var listenerMap = {};

        function buildListenerMap() {
            listenerMap = {};


            for (var t in listenerList) {
                var listener = listenerList[t];
                var eventType = listener.eventType;

                var listenerArray = (listenerMap[eventType] || (listenerMap[eventType] = []));
                listenerArray.push(listener);
            }

        }


        //eventListener.fireEvent('mouse_onLeftClick',[1,2]);
        self.fireEvent = function (eventType, args) {
            //var args = arguments.slice(1);
            args = args || [];

            var listeners = listenerMap[eventType];
            if (listeners) {
                for (var t in listeners) {
                    var listener = listeners[t];

                    try {
                        listener.handler.apply(listener, args);
                    }
                    catch (e) {
                        console.log(e);
                    }
                }
            }
        }
    };


})('undefined' === typeof (vit) ? vit = {} : vit);



//demo:
//var eventListener = new vit.EventListener();


//添加事件
//eventListener.addListener({ eventType: 'Mouse.LeftClick', handler: function (name,age) { } });

//eventListener.addListener({ eventType: 'Mouse.LeftClick', listenerKey: 'listenerFromBe', handler: function (name,age) { } });


//触发事件
//eventListener.fireEvent('Mouse.LeftClick', ['tom',16]);



//删除事件列表中相同listenerKey的事件
//eventListener.removeEventByKey('listenerFromBe');