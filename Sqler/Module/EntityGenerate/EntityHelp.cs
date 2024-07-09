#region << Version-v2 >>
/*
 * ========================================================================
 * Version： v2
 * Time   ： 2024-06-29
 * Author ： lith
 * Email  ： serset@yeah.net
 * Remarks： 
 * ========================================================================
*/
#endregion

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Vit.Db.Module.Schema;

namespace Vit.DynamicCompile.EntityGenerate
{
    public class EntityHelp
    {
        public static Type GenerateEntityBySchema(TableSchema schema, string Namespace)
        {
            var tableName = schema.table_name;

            // #1
            var typeDescriptor = new TypeDescriptor
            {
                assemblyName = "DynamicEntity",
                moduleName = "Main",
                typeName = Namespace + "." + tableName,
            };

            // #2 Type Attribute
            typeDescriptor.AddAttribute<TableAttribute>(constructorArgs: new object[] { tableName });

            // #3 properties
            {
                schema.columns.ForEach(column =>
                {
                    var property = new PropertyDescriptor(column.column_name, column.column_clr_type);

                    //property.AddAttribute<RequiredAttribute>();

                    if (column.primary_key == 1) property.AddAttribute<KeyAttribute>();

                    if (column.autoincrement == 1) property.AddAttribute<DatabaseGeneratedAttribute>(constructorArgs: new object[] { DatabaseGeneratedOption.Identity });

                    if (!string.IsNullOrEmpty(column.column_type))
                        property.AddAttribute<ColumnAttribute>(constructorArgs: new object[] { column.column_name }, propertyValues: new (string, object)[] { ("TypeName", column.column_type) });

                    typeDescriptor.AddProperty(property);
                });
            }

            return EntityGenerator.CreateType(typeDescriptor);
        }
    }
}
