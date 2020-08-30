using System.Collections.Generic;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using Sqler.Module.AutoTemp.Logical.Repository;
using System.Linq;
using Vit.Core.Util.ComponentModel.SsError;
using System.IO;

namespace Sqler.Module.FileMng.Controllers
{

    #region Repository
    public class FileMngRepository : IRepository<FileMngRepository.Model>
    {

        #region Model
        public class Model
        {

            /// <summary>
            /// 文件路径，如 /a/b
            /// [idField]
            /// [field:visiable=false]
            /// [controller:permit.insert=false] 
            /// [controller:permit.update=true] 
            /// [controller:permit.show=false]
            /// [controller:permit.delete=true] 
            /// 
            /// [controller:dependency.js=\x5B '/fileMng/vit.ChunkUpload.js','/fileMng/autoTemp.fileMng.js' \x5D] 
            /// </summary>      
            public string id { get; set; }


            /// <summary>
            /// [pidField]
            /// [rootPidValue:]
            /// [fieldIgnore] 
            /// </summary>
            public string pid { get; set; }

            /// <summary>
            /// &lt;span title="文件名"&gt;文件名&lt;/span&gt;
            /// [treeField]      
            /// [field:list_width=480] 
            /// [filter:,Contains]
            /// </summary>
            public string name { get; set; }


            // 文件夹  文件
            /// <summary>
            /// 类型
            /// [field:list_width=60] 
            /// [field:editable=false]
            /// </summary>
            public string type { get; set; }



            /// <summary>
            /// 大小(MB)
            /// [field:list_width=80] 
            /// [field:editable=false]
            /// </summary>
            public string size_MB => (float.Parse(size_KB??"0")/1024).ToString("f2");


            /// <summary>
            /// 大小(KB)
            /// [field:list_width=80] 
            /// [field:editable=false]
            /// </summary>
            public string size_KB { get; set; }

            /// <summary>
            /// [fieldIgnore] 
            /// </summary>
            public int? _childrenCount => (type == "文件夹" ? 1 : 0);

        }

        #endregion




        #region DataSource
 

        static string BasePath = "..\\..\\";


        public static string GetFilePathById(string id) 
        {
            if (string.IsNullOrEmpty(id)) return BasePath;

            return Path.Combine(BasePath, decode(id));
            //return Path.Combine(BasePath, id??"");
        }


        static string encode(string v) 
        {
            return v.StringToBytes().BytesToHexString();
        }

        static string decode(string v)
        {
            return v.HexStringToBytes().BytesToString();
        }

        static List<Model> GetDataSource(string pid)
        {
       
            DirectoryInfo dir = new DirectoryInfo(GetFilePathById(pid));
            var parentDirPath = dir.FullName;

            var list = new List<Model>();

            if (dir.Exists)
            {

                //文件夹
                list.AddRange( dir.GetDirectories().Select(m=> {
                    return new Model { id= encode( m.FullName.Replace(BasePath,"")),pid= encode(pid), name=m.Name, type="文件夹" };                
                }));


                //文件 
                list.AddRange(dir.GetFiles().Select(m => {
                    return new Model { id = encode(m.FullName.Replace(BasePath, "")), pid = encode(pid), name = m.Name, type = "文件", size_KB = (""+(m.Length/ 1024.0).ToString("f3")) };
                }));                
            }

            return list;
        }


        #endregion

        public ApiReturn<Model> GetModel(string id)
        {
            return GetFileModel(id);
        }
        public static ApiReturn<Model> GetFileModel(string id)
        {
            var filePath = GetFilePathById(id);
            if (File.Exists(filePath))
            {
                var m = new FileInfo(filePath);
                return new Model { id = encode(m.FullName.Replace(BasePath, "")), name = m.Name, type = "文件", size_KB = ("" + (m.Length / 1024.0).ToString("f3")) };
            }
            else if (Directory.Exists(filePath))
            {
                var m = new DirectoryInfo(filePath);
                return new Model { id = encode(m.FullName.Replace(BasePath, "")), name = m.Name, type = "文件夹" };
            }

            return false;
        }


        public ApiReturn<Model> Update(Model model)
        {
            var filePath = GetFilePathById(model.id);

            var newPath = Path.Combine(Path.GetDirectoryName(filePath), model.name);

            if (model.type == "文件")
            {
                File.Move(filePath, newPath);

            }
            else
            {           
                Directory.Move(filePath, newPath);
            }
            return true;

        }

        public ApiReturn Delete(Model model)
        {
            var filePath = GetFilePathById(model.id);
            if (model.type == "文件")
            {
                File.Delete(filePath);

            }
            else
            {
                Directory.Delete(filePath, true);
            }
            return true;
        }

        public ApiReturn<PageData<Model>> GetList(List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page)
        {
            var pid = filter.AsQueryable().FirstOrDefault(m => m.field == "pid" && m.opt=="=")?.value as string;

            filter = filter.Where(m => m.field != "pid").ToList();


            var query = GetDataSource(pid).AsQueryable();

            var pageData = query.ToPageData(filter, sort, page);


             

            return new ApiReturn<PageData<Model>> { data = pageData };
        }

        public ApiReturn<Model> Insert(Model model)
        {
            return new SsError { errorMessage = "功能未开放" };
        }

    }
    #endregion

}
