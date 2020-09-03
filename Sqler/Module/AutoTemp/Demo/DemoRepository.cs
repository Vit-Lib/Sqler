using System.Collections.Generic;
using Vit.Core.Util.ComponentModel.Data;
using Vit.Core.Util.ComponentModel.Query;
using Vit.Extensions;
using Vit.Linq.Query;
using System.ComponentModel.DataAnnotations;
using App.Module.AutoTemp.Logical.Repository;
using Vit.Core.Util.Common;
using System.Linq;
using Vit.Extensions.ObjectExt;
using Vit.Core.Util.ComponentModel.SsError;

namespace App.Module.AutoTemp.Demo
{

    #region Repository
    public class DemoRepository : IRepository<DemoRepository.Model>
    {

        #region Model
        public class Model
        {

            /// <summary>
            /// [field:visiable=false]
            /// [controller:permit.delete=false] 
            /// </summary>
            [Key]      
            public int id { get; set; }



            public int pid { get; set; }

            /// <summary>
            /// &lt;span title="装修商名称"&gt;装修商&lt;/span&gt;
            /// [field:editable=false]
            /// [field:list_width=200] 
            /// [filter:,Contains]
            /// </summary>
            public string name { get; set; }
            public int age { get; set; }
            public string sex { get; set; }

            /// <summary>
            /// [field:ig-class=TextArea]
            /// </summary>
            public string random { get; set; }
            public string random2 { get; set; }

            /// <summary>
            /// [fieldIgnore] 
            /// </summary>
            public int? _childrenCount { get; set; }

        }

        #endregion




        #region DataSource
        static List<Model> dataSource = getDataSource();


        static List<Model> getDataSource()
        {
            var list = new List<Model>(1000);
            for (int t = 1; t <= 1000; t++)
            {
                list.Add(new Model
                {
                    id = t,
                    pid = t / 10,
                    name = "name" + t,
                    age = 20,
                    sex = "1",
                    random = "" + CommonHelp.NewGuidLong()
                });
            }

            return list;
        }

        
        #endregion


        public ApiReturn<Model> GetModel(string id)
        {
            if (!int.TryParse(id, out int m_id))
            {
                return new SsError{  errorMessage = "数据不存在" };           
            }

            var query = dataSource.AsQueryable();
            var model = query.FirstOrDefault(m => m.id == m_id);

            return model;       
        }


        public ApiReturn<Model> Update(Model model_)
        {      

            var model_Data = dataSource.FirstOrDefault(m => m.id == model_.id);

            if (model_Data != null)
            {
                model_Data.CopyNotNullProrertyFrom(model_);

                return model_Data;
            }
            else
            {
                return new SsError { errorMessage = "待修改的数据不存在" };
            }          
        }

        public ApiReturn Delete(Model model)
        {             

            var model_Data = dataSource.FirstOrDefault(m => m.id == model.id);

            if (model_Data != null)
            {
                dataSource.Remove(model_Data);
                return true;
            }
            else
            {
                return new SsError { errorMessage = "待删除的数据不存在" };
            }
        }

        public ApiReturn<PageData<Model>> GetList(List<DataFilter> filter, IEnumerable<SortItem> sort, PageInfo page)
        {
            var query = dataSource.AsQueryable();

            var pageData = query.ToPageData(filter, sort, page);


            #region _childrenCount            
            pageData.rows.ForEach(m =>
            {
                m._childrenCount = query.Count(child => child.pid == m.id);
            });
            #endregion

            return new ApiReturn<PageData<Model>> { data = pageData };
        }

        public ApiReturn<Model> Insert(Model model)
        {
            var model_ = model.ConvertBySerialize<Model>();

            model_.id = dataSource[dataSource.Count - 1].id + 1;
            dataSource.Add(model_);

            return model_;
        }

    }
    #endregion

}
