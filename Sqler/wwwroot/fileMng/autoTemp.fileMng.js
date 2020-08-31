;

 
autoTemp.eventListener.addListener({
    eventType: 'list.controller.beforeCreate', 
    handler: function (controller, controllerConfig) {


        var chunkUpload = new vit.ChunkUpload();

        //文件块大小,默认 500KB
        chunkUpload.chunkSize = 102400 * 5;

        //上传文件的地址
        chunkUpload.url = '/fileMng/uploadChunkFile';

        function KpsCalc() {
            var lastTime;
            var uploadedKb = 0;
            var lastKps = 0;

            var chunkKb = chunkUpload.chunkSize / 1024.0;

            this.getKps = function () {
                var curTime = new Date();
                if (!lastTime) {
                    lastTime = curTime;
                    return lastKps;
                }

                uploadedKb += chunkKb;

                var ms = curTime - lastTime;
                if (ms >= 1000) {
                    lastKps = uploadedKb / ms * 1000;
                    uploadedKb = 0;
                    lastTime = curTime;
                }             
              
                return lastKps;
            };

        }

        var kpsCalc = new KpsCalc();

        function uploadFile(callback, id) {

            callback();

            chunkUpload.deferred
                .reset()
                .beforeUploadChunk(function (ajaxConfig) {
                    var formData = ajaxConfig.data;
                    formData.append("id", id);
                })
                .onStart(function (file, fileGuid) {
                    //console.log('开始上传，文件名：' + file.name + '  文件大小：' + file.size + ' B');
                    theme.progressStart('上传文件中', null, true);
                    kpsCalc.getKps();
                })
                .progress(function (uploadedSize, fileSize) {
                    var kbs = kpsCalc.getKps();

                    if (kbs != kpsCalc.kbs) {
                        kpsCalc.kbs = kbs;
                        theme.progressText('{value}% ' + parseFloat(kbs).toFixed(2) + ' KB/s');
                    }

                    var progress = (uploadedSize / fileSize * 100).toFixed(2);
                    theme.progressValue(progress);
                    //console.log('已经上传：' + uploadedSize + '  百分比：' + (uploadedSize / fileSize * 100).toFixed(2) + '%');
                })
                .done(function (apiRet) {
                    theme.progressStop();

                    console.log("上传成功！");
                    console.log(apiRet);          

                    template.jqGrid.treegrid('reload', id);
                })
                .fail(function (apiRet) {
                    theme.progressStop();

                    if (!theme.alertApiReturn(apiRet)) { return;}                 
                });

            chunkUpload.selectFile();
        }

        if (!controllerConfig.list) controllerConfig.list = {};
        if (!controllerConfig.list.rowButtons) controllerConfig.list.rowButtons = [];

        controllerConfig.list.rowButtons.push({ text: '上传文件', handler: uploadFile });
    }
});