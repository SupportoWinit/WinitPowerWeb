
using Common;
using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Business
{


    public static class ExpressionBuilder
    {
        #region EXTENSIONS


        //Extension del tipo IQueryable che permette la creazione dinamica di una lambda expression (WHERE) partendo dal vettore dei filtri 
        //provenienti da una dxDataGrid
        public static IQueryable<T> DxWhere<T>(this IQueryable<T> repo, JArray whereArray) where T : class
        {
            if (whereArray.Any())
            {
                ParameterExpression param = Expression.Parameter(typeof(T), typeof(T).Name);

                return repo.Where(ParseFilter<T>((whereArray[0].Type == JTokenType.String) ? new JArray() { whereArray } : whereArray, param));
            }



            return repo;
        }

        //Extension del tipo IQueryable che permette la generazione dinamica di una lambda expression (SELECT) partendo da un vettore di proprietà
        //che ritorna un'IQueryable di oggetti anonimi (tipo generato a runtime)
        public static IQueryable<dynamic> DxSelect<T>(this IQueryable<T> repo, JArray properties, bool distinct = false) where T : class
        {
            var parameterExpression = Expression.Parameter(typeof(T), typeof(T).Name);

            var stringProperties = properties.ToObject<string[]>();

            if (distinct)
            {
                return repo.Select(BuildDynamicObject<T>(parameterExpression, stringProperties)).Distinct();
            }

            if (!properties.Any())
            {
                return repo.Select(BuildDynamicObject<T>(parameterExpression, typeof(T).GetProperties().Where(p => !p.GetGetMethod().IsVirtual).Select(p => p.Name).ToArray()));
            }

            return repo.Select(BuildDynamicObject<T>(parameterExpression, stringProperties));
        }

        //Extension del tipo IQueyable che permette l'ordinamento secondo una proprietà (o più) e nel caso non vi siano proprietà
        //ordina per la chiave primaria della tabella corrente (è necessario per il metodo skip + take)
        public static IQueryable<T> DxOrderBy<T>(this IQueryable<T> repo, System.Data.Entity.DbContext context, JArray properties) where T : class
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(T), typeof(T).Name);

            IOrderedQueryable<T> orderedResult = null;

            if (!properties.Any())
            {
                //Estrazione della primary key dal context

                ObjectContext objectContext = ((IObjectContextAdapter)context).ObjectContext;
                ObjectSet<T> set = objectContext.CreateObjectSet<T>();
                string primaryKeyName = set.EntitySet.ElementType.KeyMembers.First().Name;

                //La lambda expression è sempre di tipo Func<T,int> perchè la chiave primaria è sempre un intero

                return repo.OrderBy(Expression.Lambda<Func<T, int>>(Expression.Property(parameterExpression, primaryKeyName), parameterExpression));
            }
            else
            {
                dynamic column;

                //Si estraggono i metodi per l'ordinamento e assegnati i relativi tipi generici in base alla proprietà da ordinare e il tipo di entità


                MethodInfo orderByMethod = typeof(Queryable).GetMethods().Single(m => m.Name == "OrderBy" && m.IsGenericMethodDefinition && m.GetParameters().Count() == 2);
                MethodInfo orderByDescMethod = typeof(Queryable).GetMethods().Single(m => m.Name == "OrderByDescending" && m.IsGenericMethodDefinition && m.GetParameters().Count() == 2);

                MethodInfo thenByMethod = typeof(Queryable).GetMethods().Single(m => m.Name == "ThenBy" && m.IsGenericMethodDefinition && m.GetParameters().Count() == 2);
                MethodInfo thenByDescMethod = typeof(Queryable).GetMethods().Single(m => m.Name == "ThenByDescending" && m.IsGenericMethodDefinition && m.GetParameters().Count() == 2);

                MethodInfo orderByMethodGen = null;
                MethodInfo orderByDescMethodGen = null;


                MethodInfo thenByMethodGen = null;
                MethodInfo thenByDescMethodGen = null;

                for (int i = 0; i < properties.Count(); i++)
                {
                    int index = i;

                    column = properties[i] as dynamic;

                    var property = (Expression.Property(parameterExpression, (string)column["selector"]));

                    var expression = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(T), typeof(T).GetProperty((string)column["selector"]).PropertyType), property, parameterExpression);

                    orderByMethodGen = orderByMethod.MakeGenericMethod(typeof(T), typeof(T).GetProperty((string)column["selector"]).PropertyType);

                    orderByDescMethodGen = orderByDescMethod.MakeGenericMethod(typeof(T), typeof(T).GetProperty((string)column["selector"]).PropertyType);

                    thenByMethodGen = thenByMethod.MakeGenericMethod(typeof(T), typeof(T).GetProperty((string)column["selector"]).PropertyType);

                    thenByDescMethodGen = thenByDescMethod.MakeGenericMethod(typeof(T), typeof(T).GetProperty((string)column["selector"]).PropertyType);


                    if (Convert.ToBoolean(column["desc"]))
                    {
                        orderedResult = (index == 0) ? (IOrderedQueryable<T>)orderByDescMethodGen.Invoke(null, new object[] { repo, expression }) : (IOrderedQueryable<T>)thenByDescMethodGen.Invoke(null, new object[] { orderedResult, expression });
                    }
                    else
                    {
                        orderedResult = (index == 0) ? (IOrderedQueryable<T>)orderByMethodGen.Invoke(null, new object[] { repo, expression }) : (IOrderedQueryable<T>)thenByMethodGen.Invoke(orderedResult, new object[] { orderedResult, expression });
                    }

                }
            }
            return orderedResult;
        }


        #endregion



        private static Expression<Func<T, dynamic>> BuildDynamicObject<T>(ParameterExpression parameter, string[] properties)
        {
            Dictionary<string, PropertyInfo> sourceProperties = properties.ToDictionary(name => name, name => typeof(T).GetProperty(name));
            Type dynamicType = LinqRuntimeTypeBuilder.GetDynamicType(sourceProperties.Values);

            ParameterExpression sourceItem = Expression.Parameter(typeof(T), "t");
            IEnumerable<MemberBinding> bindings = dynamicType.GetFields().Select(p => Expression.Bind(p, Expression.Property(sourceItem, sourceProperties[p.Name]))).OfType<MemberBinding>();

            Expression<Func<T, dynamic>> selector = Expression.Lambda<Func<T, dynamic>>(Expression.MemberInit(
            Expression.New(dynamicType.GetConstructor(Type.EmptyTypes)), bindings), sourceItem);


            return selector;


        }

        private static Expression<Func<T, bool>> BuildNavigationExpression<T>(Expression parameter, string property, OperatorComparer op, string value)
        {
            return BuildCondition<T>(parameter, property, op, value);
        }

        private static Expression BuildSubQuery<T>(Expression parameter, Type childType, Expression predicate)
        {
            var anyMethod = typeof(Enumerable).GetMethods().Single(m => m.Name == "Any" && m.GetParameters().Length == 2);
            anyMethod = anyMethod.MakeGenericMethod(childType);
            predicate = Expression.Call(anyMethod, parameter, predicate);
            return MakeLambda<T>(parameter, predicate);
        }

        private static Expression<Func<T, bool>> BuildCondition<T>(Expression parameter, string property, OperatorComparer comparer, string value)
        {
            var childProperty = parameter.Type.GetProperty(property);
            var left = Expression.Property(parameter, childProperty);
            //var right = Expression.Constant(value);
            var predicate = BuildComparsion(left, comparer, value);
            return MakeLambda<T>(parameter, predicate);
        }

        private static Expression BuildComparsion(Expression left, OperatorComparer comparer, object right)
        {
            var mask = new List<OperatorComparer>{
            OperatorComparer.Contains,
            OperatorComparer.NotContains,
            OperatorComparer.StartsWith,
            OperatorComparer.EndsWith,
             OperatorComparer.Equals
             };

            if (Nullable.GetUnderlyingType(left.Type) == typeof(DateTime) || left.Type == typeof(DateTime))  //Tipo DateTime o DateTime?
            {
                if (Nullable.GetUnderlyingType(left.Type) == typeof(DateTime))
                {
                    if ((string)right == "")
                    {
                        return BuildDateTimeCondition(left, comparer, Expression.Constant(null));
                    }
                    return BuildDateTimeCondition(left, comparer, Expression.Constant(DateTime.Parse((string)right)));
                }

                return BuildDateTimeCondition(left, comparer, Expression.Constant(DateTime.Parse((string)right)));
            }
            else if (Nullable.GetUnderlyingType(left.Type) == typeof(TimeSpan) || left.Type == typeof(TimeSpan))  //Tipo DateTime o DateTime?
            {
                if (Nullable.GetUnderlyingType(left.Type) == typeof(TimeSpan))
                {
                    if (right != null)
                    {
                        return BuildTimeSpanCondition(left, comparer, Expression.Constant(null));
                    }
                    return BuildTimeSpanCondition(left, comparer, Expression.Constant(TimeSpan.Parse((string)right)));
                }

                return BuildTimeSpanCondition(left, comparer, Expression.Constant(TimeSpan.Parse((string)right)));
            }
            else if (left.Type != typeof(string))
            {
                return BuildCondition(left, comparer, Expression.Constant(TypeDescriptor.GetConverter(left.Type).ConvertFromString((string)right)));
            }
            if (!mask.Contains(comparer))
            {
                return Expression.MakeBinary((ExpressionType)comparer, left, Expression.Convert(Expression.Constant(TypeDescriptor.GetConverter(left).ConvertTo(right, left.Type)), left.Type));
            }
            return BuildStringCondition(left, comparer, Expression.Constant(TypeDescriptor.GetConverter(left.Type).ConvertFromString((string)right)));
        }

        private static Expression<Func<T, bool>> ParseFilter<T>(JArray filter, ParameterExpression parameter)
        {

            Expression<Func<T, bool>> expTmp = null;

            int i = 0;

            while (i < filter.Count)
            {
                //Termine della ricorsione (ho trovato un filtro parziale)
                if (filter[i].Type == JTokenType.String && i % 2 == 0) //FILTRO
                {
                    return BuildNavigationExpression<T>(parameter, filter[0].ToString(), FindMethod(filter[1].ToString()), filter[2] != null ? filter[2].ToString() : null);

                }
                else if (filter[i].Type == JTokenType.Array && i % 2 == 0) //FILTRO ANNIDATO
                {
                    expTmp = ParseFilter<T>((JArray)filter[i], parameter);
                }
                else if (filter[i].Type == JTokenType.String && i % 2 != 0) //OPERATORE BINARIO
                {
                    switch (filter[i].ToString())
                    {
                        case "and":
                            expTmp = MakeLambda<T>(parameter, Expression.AndAlso(expTmp.Body, ParseFilter<T>((JArray)filter[++i], parameter).Body));
                            break;
                        case "or":
                            expTmp = MakeLambda<T>(parameter, Expression.OrElse(expTmp.Body, ParseFilter<T>((JArray)filter[++i], parameter).Body));
                            break;
                    }
                }

                i++;

            }
            return expTmp;


        }

        private static Expression BuildCondition(Expression left, OperatorComparer comparer, Expression right)
        {
            var compareMethod = left.Type.GetMethods().SingleOrDefault(m => m.Name.Equals(Enum.GetName(typeof(OperatorComparer), comparer)) && m.GetParameters().Count() == 1 && m.GetParameters().First().ParameterType == right.Type);
            if (compareMethod == null)
            {
                return Expression.NotEqual(left, right);
            }

            return Expression.Call(left, compareMethod, right);
        }

        #region CONDIZIONI TIPI COMPLESSI

        private static Expression BuildStringCondition(Expression left, OperatorComparer comparer, Expression right)
        {
            MethodInfo compareMethod = null;
            switch (comparer.ToString())
            {
                case "Equals":
                    compareMethod = typeof(string).GetMethod(comparer.ToString(), new[] { typeof(string) });
                    break;
                case "Contains":
                    compareMethod = typeof(string).GetMethod(comparer.ToString(), new[] { typeof(string) });
                    break;
                case "NotContains":
                    compareMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    return Expression.Not(Expression.Call(left, compareMethod, right));
                case "NotEquals":
                    compareMethod = typeof(string).GetMethod("Equals", new[] { typeof(string) });
                    return Expression.Not(Expression.Call(left, compareMethod, right));
                case "StartsWith":
                    compareMethod = typeof(string).GetMethod(comparer.ToString(), new[] { typeof(string) });
                    break;
                case "EndsWith":
                    compareMethod = typeof(string).GetMethod(comparer.ToString(), new[] { typeof(string) });
                    break;

            }
            return Expression.Call(left, compareMethod, right);
        }
        private static Expression BuildTimeSpanCondition(Expression left, OperatorComparer comparer, Expression right)
        {
            MethodInfo compareMethod = typeof(Expression).GetMethods().Single(m => m.Name == comparer.ToString() && m.GetParameters().Count() == 2);

            try
            {
                if (IsNullableType(left.Type))
                {
                    if (IsNullableType(left.Type) && !IsNullableType(right.Type))
                        right = Expression.Convert(right, left.Type);
                    else if (!IsNullableType(left.Type) && IsNullableType(right.Type))
                        left = Expression.Convert(left, right.Type);
                }

                return (Expression)compareMethod.Invoke(null, new object[] { left, right });
            }
            catch (Exception ex)
            {
            }

            return null;
        }
        private static Expression BuildDateTimeCondition(Expression left, OperatorComparer comparer, Expression right)
        {
            ;

            string method = comparer.ToString();

            if (method == "Equals")
            {
                method = "Equal";
            }
            if (method == "NotEquals")
            {
                method = "NotEqual";
            }

            try
            {
                if (IsNullableType(left.Type))
                {


                    if (IsNullableType(left.Type) && !IsNullableType(right.Type))
                    {
                        right = Expression.Convert(right, left.Type);
                    }

                    else if (!IsNullableType(left.Type) && IsNullableType(right.Type))
                        left = Expression.Convert(left, right.Type);
                }

                return (Expression)typeof(Expression).GetMethod(method, new[] { typeof(Expression), typeof(Expression) }).Invoke(null, new object[] { left, right });
            }
            catch (Exception ex)
            {
            }

            return null;
        }

        #endregion

        private static Expression<Func<T, bool>> MakeLambda<T>(Expression parameter, Expression predicate)
        {
            var resultParameterVisitor = new ParameterVisitor();
            resultParameterVisitor.Visit(parameter);
            var resultParameter = resultParameterVisitor.Parameter;
            return Expression.Lambda<Func<T, bool>>(predicate, (ParameterExpression)resultParameter);
        }

        private class ParameterVisitor : ExpressionVisitor
        {
            public Expression Parameter
            {
                get;
                private set;
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                Parameter = node;
                return node;
            }
        }

        static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }


        #region METODI ESTRAZIONE PROPRIETA'

        private static string[] ExtracProperties(object[] whereArray)
        {
            List<string> properties = new List<string>();

            IEnumerable<object[]> normalizedQuery = whereArray.OfType<object[]>().ToArray();

            foreach (object[] query in whereArray)
            {
                properties.Add(query[0].ToString());
            }

            return properties.ToArray();

        }

        #endregion

        #region METODI ESTRAZIONE VALORI

        private static string[] ExtractValues(object[] whereArray)
        {
            List<string> values = new List<string>();

            IEnumerable<object[]> normalizedQuery = whereArray.OfType<object[]>().ToArray();

            foreach (object[] query in whereArray)
            {
                values.Add(query[2].ToString());
            }

            return values.ToArray();

        }

        #endregion

        #region METODI ESTRAZIONE OPERATORI

        private static OperatorComparer[] ExtractOperators(object[] whereArray)
        {
            List<OperatorComparer> operators = new List<OperatorComparer>();

            IEnumerable<object[]> normalizedQuery = whereArray.OfType<object[]>().ToArray();

            foreach (object[] query in whereArray)
            {
                operators.Add(FindMethod(query[1].ToString()));
            }

            return operators.ToArray();
        }

        #endregion

        private static OperatorComparer FindMethod(string method)
        {
            switch (method)
            {
                case "contains":
                    return OperatorComparer.Contains;
                case "notcontains":
                    return OperatorComparer.NotContains;
                case "startswith":
                    return OperatorComparer.StartsWith;
                case "endswith":
                    return OperatorComparer.EndsWith;
                case "=":
                    return OperatorComparer.Equals;
                case ">=":
                    return OperatorComparer.GreaterThanOrEqual;
                case "<":
                    return OperatorComparer.LessThan;
                case "<=":
                    return OperatorComparer.LessThanOrEqual;
                case "<>":
                    return OperatorComparer.NotEqual;


            }
            return OperatorComparer.Contains;
        }


        public static class LinqRuntimeTypeBuilder
        {
            private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            private static AssemblyName assemblyName = new AssemblyName() { Name = "DynamicLinqTypes" };
            private static ModuleBuilder moduleBuilder = null;
            private static Dictionary<string, Type> builtTypes = new Dictionary<string, Type>();

            static LinqRuntimeTypeBuilder()
            {
                moduleBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(assemblyName.Name);
            }

            //Funzione che si occupa di dare un nome generico al nuovo tipo che stiamo generando partendo dalle proprietà richieste
            private static string GetTypeKey(Dictionary<string, Type> fields)
            {
                //TODO: optimize the type caching -- if fields are simply reordered, that doesn't mean that they're actually different types, so this needs to be smarter

                string key = string.Empty;

                var orderedFields = fields.ToList().OrderBy(c => c.Key).ToList();


                foreach (var field in orderedFields)
                    key += field.Key;

                return key;
            }

            public static Type GetDynamicType(Dictionary<string, Type> fields)
            {
                if (null == fields)
                    throw new ArgumentNullException("fields");
                if (0 == fields.Count)
                    throw new ArgumentOutOfRangeException("fields", "fields must have at least 1 field definition");

                try
                {
                    Monitor.Enter(builtTypes);
                    string className = GetTypeKey(fields);

                    if (builtTypes.ContainsKey(className))
                        return builtTypes[className];

                    TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

                    foreach (var field in fields)
                        typeBuilder.DefineField(field.Key, field.Value, FieldAttributes.Public);

                    builtTypes[className] = typeBuilder.CreateType();

                    return builtTypes[className];
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
                finally
                {
                    Monitor.Exit(builtTypes);
                }

                return null;
            }


            private static string GetTypeKey(IEnumerable<PropertyInfo> fields)
            {
                return GetTypeKey(fields.ToDictionary(f => f.Name, f => f.PropertyType));
            }

            public static Type GetDynamicType(IEnumerable<PropertyInfo> fields)
            {
                return GetDynamicType(fields.ToDictionary(f => f.Name, f => f.PropertyType));
            }

        }

    }
}
