using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using SqlSugar;

using ZeroOne.Entity;
using ZeroOne.Extension;

namespace ZeroOne.Repository
{
    public abstract class BaseRep<TSearchModel, TModel> : IBaseRep<TSearchModel, TModel> where TSearchModel : BaseSearch where TModel : BaseEntity
    {

        private ISqlSugarClient _client;
        public BaseRep(ISqlSugarClient client)
        {
            this._client = client;
        }

        public async Task<TModel> GetModel(Guid id)
        {
            var query = this._client.Queryable<TModel>();
            return await query.Where(t => t.Id == id).SingleAsync();
        }

        /// <summary>
        /// 根据不同的IEnumerable的泛型类型参数的类型调用不同的Contains方法
        /// </summary>
        /// <param name="type">泛型类型参数的类型</param>
        /// <returns></returns>
        private MethodInfo GetMethodByGenericArgType(Type type)
        {
            MethodInfo method = null;
            if (type != null)
            {
                if (type == typeof(int))
                {
                    method = (methodof<Func<IEnumerable<int>, int, bool>>)Enumerable.Contains;
                }
                else if (type == typeof(decimal))
                {
                    method = (methodof<Func<IEnumerable<decimal>, decimal, bool>>)Enumerable.Contains;
                }
                else if (type == typeof(float))
                {
                    method = (methodof<Func<IEnumerable<float>, float, bool>>)Enumerable.Contains;
                }
                else if (type == typeof(long))
                {
                    method = (methodof<Func<IEnumerable<long>, long, bool>>)Enumerable.Contains;
                }
                else if (type == typeof(double))
                {
                    method = (methodof<Func<IEnumerable<double>, double, bool>>)Enumerable.Contains;
                }
                else if (type == typeof(DateTime))
                {
                    method = (methodof<Func<IEnumerable<DateTime>, DateTime, bool>>)Enumerable.Contains;
                }
                else if (type == typeof(string))
                {
                    method = (methodof<Func<IEnumerable<string>, string, bool>>)Enumerable.Contains;
                }
            }
            return method;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="model"></param>
        /// <param name="client"></param>
        /// <typeparam name="TSearchModel"></typeparam>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public async Task<IList<TModel>> GetModelList(IList<BaseRepModel> items, TSearchModel model)
        {

            //返回model的类型
            var modelType = typeof(TModel);
            //获取model类型
            var searchModelType = model.GetType();

            //lambda类型参数别名
            ParameterExpression paramExpr = Expression.Parameter(modelType, "it");

            PropertyInfo property;
            if (items.Count > 0)
            {
                Expression left;
                Expression right;
                Expression childExpression = null;
                Expression totalExpression = null;

                string propTypeName = string.Empty;
                //ConditionalExpression 
                foreach (var item in items)
                {
                    property = searchModelType.GetProperty(item.Key);
                    //判断值是否存在
                    var value = property.GetValue(model);
                    if (value == null)
                    {
                        continue;
                    }
                    propTypeName = property.PropertyType.FullName;
                    //判断是泛型IEnumerable<>
                    if (property.PropertyType != typeof(string) && property.PropertyType.GetInterfaces().Any(x => typeof(IEnumerable<>) == (x.IsGenericType ? x.GetGenericTypeDefinition() : x)))
                    {
                        PropertyInfo compareProp = modelType.GetProperty(item.Key);

                        MethodInfo containsMethod = this.GetMethodByGenericArgType(property.PropertyType.GetGenericArguments()[0]);
                        //判断属性值是否为Nullable类型值
                        var parentTypes = compareProp.PropertyType.GetGenericTypeDefinition();
                        if (compareProp.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            var valProp = compareProp.PropertyType.GetProperty("Value");
                            var valExpression = Expression.Property(Expression.Property(paramExpr, compareProp), valProp);
                            var searchPropertyExpression = Expression.Property(Expression.Constant(model, searchModelType), property);
                            childExpression = Expression.Call(containsMethod, searchPropertyExpression, valExpression);
                        }
                        else
                        {
                            var searchPropertyExpression = Expression.Property(Expression.Constant(model, searchModelType), property);
                            childExpression = Expression.Call(containsMethod, searchPropertyExpression, Expression.Property(paramExpr, compareProp));
                        }
                    }
                    else
                    {
                        //等于运算各种基础类型一致
                        if (item.CompareOperator == ECompareOperator.Equal)
                        {
                            left = Expression.Property(paramExpr, modelType.GetProperty(item.Key));
                            right = Expression.Constant(value, property.PropertyType);
                            childExpression = Expression.Equal(left, right);
                        }
                        else
                        {
                            if (propTypeName.Contains("String"))
                            {
                                if (item.CompareOperator == ECompareOperator.Contains)
                                {
                                    string strVal = ((string)value).Trim();
                                    //常量表达式
                                    Expression valExpression = Expression.Constant(strVal, strVal.GetType());
                                    //调用的函数
                                    MethodInfo contains = (methodof<Func<string, bool>>)strVal.Contains;
                                    childExpression = Expression.Call(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), contains, valExpression);
                                }
                            }
                            else if (propTypeName.Contains("Int32") || propTypeName.Contains("Single")
                                    || propTypeName.Contains("Double") || propTypeName.Contains("Decimal")
                                    || propTypeName.Contains("DateTime"))
                            {
                                if (item.CompareOperator == ECompareOperator.GreaterThan)
                                {
                                    childExpression = Expression.GreaterThan(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
                                }
                                else if (item.CompareOperator == ECompareOperator.GreaterThanOrEqual)
                                {
                                    childExpression = Expression.GreaterThanOrEqual(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
                                }
                                else if (item.CompareOperator == ECompareOperator.LessThan)
                                {
                                    childExpression = Expression.LessThan(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
                                }
                                else if (item.CompareOperator == ECompareOperator.LessThanOrEqual)
                                {
                                    childExpression = Expression.LessThanOrEqual(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
                                }
                            }
                        }
                    }

                    //表达式是否为空
                    if (childExpression != null)
                    {
                        if (totalExpression == null)
                        {
                            totalExpression = childExpression;
                        }
                        else
                        {
                            if (item.LogicalOperatorType == ELogicalOperatorType.And)
                            {
                                totalExpression = Expression.AndAlso(totalExpression, childExpression);
                            }
                            else if (item.LogicalOperatorType == ELogicalOperatorType.Or)
                            {
                                totalExpression = Expression.OrElse(totalExpression, childExpression);
                            }
                        }
                    }
                }
                if (totalExpression != null)
                {
                    var lambdaExpression = Expression.Lambda<Func<TModel, bool>>(totalExpression, new ParameterExpression[] { paramExpr });
                    var query = this._client.Queryable<TModel>().Where(lambdaExpression);
                    var keyValues = query.ToSql();
                    return await query.ToListAsync();
                }
            }
            return new List<TModel>();
        }

        //         public async Task<IList<TModel>> GetModelList(List<Tuple<IList<BaseRepModel>, ELogicalOperatorType>> groups, TSearchModel model)
        //         {
        //             if (groups != null && groups.Count > 0)
        //             {
        //                 //返回model的类型
        //                 var modelType = typeof(TModel);
        //                 //获取model类型
        //                 var searchModelType = model.GetType();

        //                 //lambda类型参数别名
        //                 ParameterExpression paramExpr = Expression.Parameter(modelType, "it");
        //                 ParameterExpression compareParamExpr = Expression.Parameter(searchModelType, nameof(model));
        //                 //所有表达式合并
        //                 Expression totalExpression = null;

        //                 PropertyInfo property;
        //                 foreach (var group in groups)
        //                 {
        //                     //判断List的组成员是否为空并且判断组成员下面的List集合是否为空
        //                     if (group != null && group.Item1 != null && group.Item1.Count > 0)
        //                     {
        //                         Expression left;
        //                         Expression right;
        //                         Expression childExpression = null;
        //                         Expression tempExpression = null;

        //                         string propTypeName = string.Empty;
        //                         //ConditionalExpression 
        //                         foreach (var item in group.Item1)
        //                         {
        //                             property = searchModelType.GetProperty(item.Key);
        //                             //判断值是否存在
        //                             var value = property.GetValue(model);
        //                             if (value == null)
        //                             {
        //                                 continue;
        //                             }
        //                             propTypeName = property.PropertyType.FullName;
        //                             //判断是泛型IEnumerable<>
        //                             if (property.PropertyType != typeof(string) && property.PropertyType.GetInterfaces().Any(x => typeof(IEnumerable<>) == (x.IsGenericType ? x.GetGenericTypeDefinition() : x)))
        //                             {
        //                                 PropertyInfo compareProp = modelType.GetProperty(item.Key);

        //                                 MethodInfo containsMethod = this.GetMethodByGenericArgType(property.PropertyType.GetGenericArguments()[0]);
        //                                 //判断属性值是否为Nullable类型值
        //                                 var parentTypes = compareProp.PropertyType.GetGenericTypeDefinition();
        //                                 if (compareProp.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        //                                 {
        //                                     var valProp = compareProp.PropertyType.GetProperty("Value");
        //                                     var valExpression = Expression.Property(Expression.Property(paramExpr, compareProp), valProp);
        //                                     Expression.Call(containsMethod, Expression.Property(compareParamExpr, property), valExpression);
        //                                 }
        //                                 else
        //                                 {
        //                                     childExpression = Expression.Call(containsMethod, Expression.Property(compareParamExpr, property), Expression.Property(paramExpr, compareProp));
        //                                 }
        //                             }
        //                             else
        //                             {
        //                                 //等于运算各种基础类型一致
        //                                 if (item.CompareOperator == ECompareOperator.Equal)
        //                                 {
        //                                     left = Expression.Property(paramExpr, modelType.GetProperty(item.Key));
        //                                     right = Expression.Constant(value, property.PropertyType);
        //                                     childExpression = Expression.Equal(left, right);
        //                                 }
        //                                 else
        //                                 {
        //                                     if (propTypeName.Contains("String"))
        //                                     {
        //                                         if (item.CompareOperator == ECompareOperator.Contains)
        //                                         {
        //                                             string strVal = ((string)value).Trim();
        //                                             //常量表达式
        //                                             Expression valExpression = Expression.Constant(strVal, strVal.GetType());
        //                                             //调用的函数
        //                                             MethodInfo contains = (methodof<Func<string, bool>>)strVal.Contains;
        //                                             childExpression = Expression.Call(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), contains, valExpression);
        //                                         }
        //                                     }
        //                                     else if (propTypeName.Contains("Int32") || propTypeName.Contains("Single")
        //                                             || propTypeName.Contains("Double") || propTypeName.Contains("Decimal")
        //                                             || propTypeName.Contains("DateTime"))
        //                                     {
        //                                         if (item.CompareOperator == ECompareOperator.GreaterThan)
        //                                         {
        //                                             childExpression = Expression.GreaterThan(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
        //                                         }
        //                                         else if (item.CompareOperator == ECompareOperator.GreaterThanOrEqual)
        //                                         {
        //                                             childExpression = Expression.GreaterThanOrEqual(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
        //                                         }
        //                                         else if (item.CompareOperator == ECompareOperator.LessThan)
        //                                         {
        //                                             childExpression = Expression.LessThan(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
        //                                         }
        //                                         else if (item.CompareOperator == ECompareOperator.LessThanOrEqual)
        //                                         {
        //                                             childExpression = Expression.LessThanOrEqual(Expression.Property(paramExpr, modelType.GetProperty(item.Key)), Expression.Constant(value, value.GetType()));
        //                                         }
        //                                     }
        //                                 }
        //                             }

        //                             //表达式是否为空
        //                             if (childExpression != null)
        //                             {
        //                                 if (tempExpression == null)
        //                                 {
        //                                     tempExpression = childExpression;
        //                                 }
        //                                 else
        //                                 {
        //                                     if (item.LogicalOperatorType == ELogicalOperatorType.And)
        //                                     {
        //                                         tempExpression = Expression.And(tempExpression, childExpression);
        //                                     }
        //                                     else if (item.LogicalOperatorType == ELogicalOperatorType.Or)
        //                                     {
        //                                         tempExpression = Expression.Or(tempExpression, childExpression);
        //                                     }
        //                                 }
        //                             }
        //                         }

        //                         if (tempExpression != null)
        //                         {
        //                             if (totalExpression == null)
        //                             {
        //                                 totalExpression = tempExpression;

        //                             }
        //                             else
        //                             {

        //                             }
        //                         }



        //                         if (tempExpression != null)
        //                         {
        //                             var lambdaExpression = Expression.Lambda<Func<TModel, bool>>(tempExpression, new ParameterExpression[] { paramExpr });

        //                             return await this._client.Queryable<TModel>().Where(lambdaExpression).ToListAsync();
        //                         }
        //                     }
        //                 }
        //             }
        //         }

        //         /// <summary>
        //         /// 
        //         /// </summary>
        //         /// <param name="sbLambda"></param>
        //         /// <param name="key"></param>
        //         /// <param name="logicalOperatorType"></param>
        //         /// <param name="compareOperator"></param>
        //         /// <param name="property"></param>
        //         /// <param name="whereModelName"></param>
        //         /// <returns></returns>
        //         private StringBuilder GetStrLambda(StringBuilder sbLambda, string key, ELogicalOperatorType logicalOperatorType,
        //         ECompareOperator compareOperator, PropertyInfo property, string whereModelName)
        //         {
        //             if (property != null)
        //             {
        //                 string propTypeName;
        //                 propTypeName = property.PropertyType.FullName;
        //                 bool isAdd = true;
        //                 if (property.PropertyType.BaseType is IEnumerable)
        //                 {
        //                     sbLambda.Append($"{whereModelName}.{key}.Contains(x.{key})");
        //                 }
        //                 else
        //                 {
        //                     if (propTypeName.Contains("Boolean"))
        //                     {
        //                         sbLambda.Append($"x.{key}=={whereModelName}.{key}");
        //                     }
        //                     else if (propTypeName.Contains("String"))
        //                     {
        //                         //比较运算
        //                         if (compareOperator == ECompareOperator.Contains)
        //                         {
        //                             sbLambda.Append($"x.{key}.Contains({whereModelName}.{key})");
        //                         }
        //                         else if (compareOperator == ECompareOperator.Equal)
        //                         {
        //                             sbLambda.Append($"x.{key}=={whereModelName}.{key}");
        //                         }
        //                         else
        //                         {
        //                             isAdd = false;
        //                         }
        //                     }
        //                     else if (propTypeName.Contains("Int32") || propTypeName.Contains("Single") || propTypeName.Contains("Double") || propTypeName.Contains("Decimal"))
        //                     {
        //                         //比较运算
        //                         if (compareOperator == ECompareOperator.Equal)
        //                         {
        //                             sbLambda.Append($"x.{key}=={whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.GreaterThan)
        //                         {
        //                             sbLambda.Append($"x.{key}>{whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.GreaterThanOrEqual)
        //                         {
        //                             sbLambda.Append($"x.{key}>={whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.LessThan)
        //                         {
        //                             sbLambda.Append($"x.{key}<{whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.LessThanOrEqual)
        //                         {
        //                             sbLambda.Append($"x.{key}<={whereModelName}.{key}");
        //                         }
        //                         else
        //                         {
        //                             isAdd = false;
        //                         }
        //                     }
        //                     else if (propTypeName.Contains("DateTime"))
        //                     {
        //                         //比较运算
        //                         if (compareOperator == ECompareOperator.Equal)
        //                         {
        //                             sbLambda.Append($"x.{key}=={whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.GreaterThan)
        //                         {
        //                             sbLambda.Append($"x.{key}>{whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.GreaterThanOrEqual)
        //                         {
        //                             sbLambda.Append($"x.{key}>={whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.LessThan)
        //                         {
        //                             sbLambda.Append($"x.{key}<{whereModelName}.{key}");
        //                         }
        //                         else if (compareOperator == ECompareOperator.LessThanOrEqual)
        //                         {
        //                             sbLambda.Append($"x.{key}<={whereModelName}.{key}");
        //                         }
        //                         else
        //                         {
        //                             isAdd = false;
        //                         }
        //                     }
        //                     else
        //                     {
        //                         isAdd = false;
        //                     }
        //                 }
        //                 if (isAdd)
        //                 {
        //                     if (logicalOperatorType == ELogicalOperatorType.And)
        //                     {
        //                         sbLambda.Append("&&");
        //                     }
        //                     else if (logicalOperatorType == ELogicalOperatorType.Or)
        //                     {
        //                         sbLambda.Append("||");
        //                     }
        //                 }
        //             }
        //             return sbLambda;
        //         }
        //     
    }
}