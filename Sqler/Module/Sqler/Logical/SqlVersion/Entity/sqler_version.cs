using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sqler.Module.Sqler.Logical.SqlVersion.Entity
{

    [Table("sqler_version")]
    public class sqler_version
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        /// <summary>
        /// 模块
        /// [controller:permit.delete=false]
        /// [filter:,=]
        /// </summary>
        public string module { get; set; }

        /// <summary>
        /// 版本
        /// [filter:,=]
        /// </summary>
        public int? version { get; set; }

        /// <summary>
        /// 是否成功
        /// [filter:,=]
        /// </summary>
        public int success { get; set; }

        /// <summary>
        /// 语句执行结果
        /// [field:ig-class=TextArea]       
        /// [field:ig-param={height:250}]
        /// </summary>
        public string result { get; set; }


        /// <summary>
        /// sql语句
        /// [field:ig-class=TextArea]       
        /// [field:ig-param={height:50}]
        /// </summary>
        public string code { get; set; }



        public DateTime? exec_time { get; set; }

        public string remarks { get; set; }

    }
}
