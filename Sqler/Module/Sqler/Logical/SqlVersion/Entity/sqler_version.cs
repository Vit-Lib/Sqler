using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Module.Sqler.Logical.SqlVersion.Entity
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
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "nvarchar(1000)")]
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
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "ntext")]
        public string result { get; set; }


        /// <summary>
        /// sql语句
        /// [field:ig-class=TextArea]
        /// [field:ig-param={height:50}]
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "ntext")]
        public string code { get; set; }



        public DateTime? exec_time { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "nvarchar(2000)")]
        public string remarks { get; set; }

    }
}
