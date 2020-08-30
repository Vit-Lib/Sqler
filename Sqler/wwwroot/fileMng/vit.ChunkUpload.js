/*
 * vit.ChunkUpload 分块文件上传器
 * Date   : 2020-08-30
 * Version: 1.0.1
 * author : Lith
 * email  : sersms@163.com
 */
; (function (vit) {


    function Deferred() {
        var self = this;


        function createCallback(eventName, fireName) {

            var eventArray = [];
            self[eventName] = function (event) {
                eventArray.push(event);
                return self;
            };

            self[fireName] = function () {
                for (var event of eventArray) {
                    event.apply(self, arguments);
                }
                return self;
            };
        }

        self.reset = function () {
            createCallback('onStart', 'fireStart');
            createCallback('beforeUploadChunk', 'fireBeforeUploadChunk');

            createCallback('progress', 'notify');
            createCallback('done', 'resolve');
            createCallback('fail', 'reject');

            return self;
        };

        self.reset();

    }



    vit.ChunkUpload = function () {
        var self = this;

        //var deferred = self.deferred = $.Deferred();
        var deferred = self.deferred = new Deferred();


        //文件块大小,默认 102400
        self.chunkSize = 102400;

        //上传文件的地址
        self.url;

        var file;
        var fileGuid;
        var uploadedSize;


        function onchange() {

            if (this.files.length != 1) return;


            //开始上传
            file = this.files[0];

            fileGuid = '' + file.size + '_' + file.name + '_' + Math.random();

            uploadedSize = 0;

            $(this).remove();

            deferred.fireStart(file, fileGuid);


            uploadChunk_start();
        }


        self.selectFile = function () {
            $('<input type="file"  >').change(onchange).click();
        };




        function uploadChunk_start() {

            //获取文件块的位置
            let end = (uploadedSize + self.chunkSize > file.size) ? file.size : (uploadedSize + self.chunkSize);

            //将文件切块上传
            let fd = new FormData();
            fd.append('files', file.slice(uploadedSize, end), file.name);

            var chunkInfo = {
                fileGuid: fileGuid,
                startIndex: uploadedSize,
                fileSize: file.size
            };

            fd.append("fileGuid", chunkInfo.fileGuid);
            fd.append("startIndex", chunkInfo.startIndex);
            fd.append("fileSize", chunkInfo.fileSize);

            uploadedSize = end;

            var ajaxConfig = {
                url: self.url,
                type: 'post',
                data: fd,
                processData: false,
                contentType: false,
                success: uploadChunk_onSuccess
            };

            deferred.fireBeforeUploadChunk(ajaxConfig);

            //POST表单数据
            $.ajax(ajaxConfig);
        }

        function uploadChunk_onSuccess(apiRet) {

            //上传失败
            if (!apiRet || apiRet.success != true) {
                deferred.reject(apiRet);
                return;
            }

            //上传完成
            if (uploadedSize >= file.size) {
                deferred.resolve(apiRet);
                return;
            }

            //更新进度
            deferred.notify(uploadedSize, file.size);

            //继续上传下一文件块
            uploadChunk_start();
        }


    };


})('undefined' === typeof (vit) ? vit = {} : vit);


/*
//demo:
var chunkUpload = new vit.ChunkUpload();

//文件块大小,默认 102400
//chunkUpload.chunkSize = 102400;

//上传文件的地址
chunkUpload.url = '/upload/uploadchunk';

chunkUpload.deferred
    //.reset()
    .beforeUploadChunk(function (ajaxConfig) {
        var formData = ajaxConfig.data;
        formData.append("type", 'test');
    })
    .onStart(function (file, fileGuid) {
        console.log('开始上传，文件名：' + file.name + '  文件大小：' + file.size + ' B');
    })
    .progress(function (uploadedSize, fileSize) {
        console.log('已经上传：' + uploadedSize + '  百分比：' + (uploadedSize / fileSize * 100).toFixed(2) + '%');
    })
    .done(function (apiRet) {
        console.log("上传成功！");
        console.log(apiRet);
    })
    .fail(function (apiRet) {
        console.log("上传出错！");
        console.log(apiRet);
    });

chunkUpload.selectFile();

//*/