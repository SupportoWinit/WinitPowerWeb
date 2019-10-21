
using Business.GridHelpers.Attributes;
using Business.GridHelpers.Classes;
using Business.GridHelpers.Classes.Decorations;
using Business.GridHelpers.Templates;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;

namespace Business.GridHelpers.ColumnsGenerator
{
    public static class ColumnsGenerator
    {
        private static IDictionary<Type, string> _propertyTypeCaptions;

        private static IDictionary<Type, Type> _propertyActions;

        private static IDictionary<Type, ICollection<Column>> _columnCache;

        static ColumnsGenerator()
        {
            _propertyTypeCaptions = new Dictionary<Type, string>();

            _propertyActions = new Dictionary<Type, Type>();

            _columnCache = new Dictionary<Type, ICollection<Column>>();

            _propertyTypeCaptions.Add(typeof(int), "number");
            _propertyTypeCaptions.Add(typeof(int?), "number");
            _propertyTypeCaptions.Add(typeof(string), "string");
            _propertyTypeCaptions.Add(typeof(DateTime), "date");
            _propertyTypeCaptions.Add(typeof(DateTime?), "date");
            _propertyTypeCaptions.Add(typeof(bool), "boolean");
            _propertyTypeCaptions.Add(typeof(bool?), "boolean");


            _propertyActions.Add(typeof(Col), typeof(ColClassTemplate));
            _propertyActions.Add(typeof(Cant), typeof(CantClassTemplate));

        }

        public static string GetTypeCaption(Type propertyType)
        {
            string propertyTypeCaption = "";

            _propertyTypeCaptions.TryGetValue(propertyType, out propertyTypeCaption);

            return propertyTypeCaption;

        }


        public static IEnumerable<Column> Generate<T>()
        {
            var baseType = typeof(T);

            ICollection<Column> columns;

            if (_columnCache.TryGetValue(baseType, out columns))
                return columns;

            columns = new List<Column>();

            var templateType = _propertyActions[baseType];

            var propertyColumns = templateType.GetProperties();

            foreach (var property in propertyColumns)
            {
                Column column = null;

                var attribute = property.GetCustomAttributes().Where(att => !(att is VisibleColumn)).FirstOrDefault();

                if (attribute == null)
                    continue;

                if (attribute is Attributes.KeyColumn)
                    column = new Classes.KeyColumn(property.PropertyType, property.Name);
                else if (attribute is Attributes.ForeignKeyColumn)
                    column = new Classes.ForeignKeyColumn(property.PropertyType, property.Name);
                else if (attribute is Attributes.StringColumn)
                    column = new Classes.StringColumn(property.PropertyType, property.Name);
                else if (attribute is Attributes.NumericColumn)
                    column = new Classes.NumericColumn(property.PropertyType, property.Name);
                else if (attribute is Attributes.DateTimeColumn)
                    column = new Classes.DateTimeColumn(property.PropertyType, property.Name);
                else if (attribute is Attributes.BooleanColumn)
                    column = new Classes.BooleanColumn(property.PropertyType, property.Name);
                else if (attribute is Attributes.DateColumn)
                    column = new Classes.DateColumn(property.PropertyType, property.Name);

                var visibleAttribute = property.GetCustomAttributes<VisibleColumn>().FirstOrDefault();

                if (visibleAttribute != null)
                    column.Visible = true;

                var customDisplayAttribute = property.GetCustomAttributes<CustomDisplayColumn>().FirstOrDefault();

                if (customDisplayAttribute != null && column is Classes.ForeignKeyColumn)
                    column.Select = customDisplayAttribute.Select;

                var lookupAttribute = property.GetCustomAttributes<Attributes.LookupColumn>().FirstOrDefault();

                if (lookupAttribute != null && column is Classes.ForeignKeyColumn)
                {
                    column.Lookup = new Lookup
                    {
                        DataSource = new DataSource
                        {
                            Paginate = true,
                            Select = lookupAttribute.Select
                        }
                    };
                }
                    
                columns.Add(column);
            }

            _columnCache.Add(baseType, columns);

            return _columnCache[baseType];
        }



    }
}
