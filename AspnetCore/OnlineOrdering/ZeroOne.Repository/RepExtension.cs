using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using SqlSugar;
using ZeroOne.Entity;
using ZeroOne.Extension;

namespace ZeroOne.Repository
{

    public interface IBulkAddOrUpdate
    {
        /// <summary>
        /// sqlsugar�ͻ��˶���
        /// </summary>
        ISqlSugarClient Client { get; set; }
    }

    /// <summary>
    /// �ֿ���չ��
    /// </summary>
    public static class RepExtension
    {
        public static void BeforeAction(this IBulkAddOrUpdate bulk, ISqlSugarClient client, Type type)
        {
            if (type == null)
            {
                throw new Exception("Model����Ϊ��");
            }
            //ʵ������
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var tableAttribute = type.GetCustomAttribute<SugarTable>();
            string tempTableName = tableAttribute != null ? tableAttribute.TableName : type.Name;

            StringBuilder sbTempTable = new StringBuilder();
            //�������ݿ��sql�ַ���
            sbTempTable.Append($" create table if not exists {tempTableName}(");
            foreach (var prop in properties)
            {
                sbTempTable.Append($" {prop.Name} {GetMySqlDbType(prop.PropertyType)},");
            }
            sbTempTable = sbTempTable.Remove(sbTempTable.Length - 1, 1);
            sbTempTable.Append(" ) ");
            //������ʱ��
            client.Ado.ExecuteCommand(sbTempTable.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="bulk"></param>
        /// <param name="models"></param>
        /// <param name="bulkAddRecords"></param>
        /// <param name="beforeAction"></param>
        /// <param name="afterAction"></param>
        /// <returns></returns>
        public static bool BulkAddOrUpdate<TSource, TTarget>(this IBulkAddOrUpdate bulk, IList<TSource> models, int bulkAddRecords = 1000, Action<ISqlSugarClient, Type> beforeAction = null, Action<ISqlSugarClient, Type, Type, string> afterAction = null) where TSource : IBulkModel, new() where TTarget : new()
        {
            int length = 0;
            if (models == null || models.Count() <= 0)
            {
                throw new Exception("���ݼ�Ϊ��");
            }
            else
            {
                length = models.Count();
            }

            var client = bulk.Client;
            //ʵ������
            Type modelType = typeof(TSource);
            var tableAttribute = modelType.GetCustomAttribute<SugarTable>();
            string tempTableName = tableAttribute != null ? tableAttribute.TableName : modelType.Name;

            var properties = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties != null && properties.Count() > 0)
            {

                try
                {
                    //��������
                    client.Ado.BeginTran();

                    //ִ����������֮ǰ�Ƿ��в���
                    beforeAction?.Invoke(client, modelType);

                    //��ȡ���й���������
                    var columnNames = properties.Select(t => t.Name);
                    int step = 0;
                    bool isExactDivision = (length % bulkAddRecords == 0);
                    //������������
                    if (isExactDivision)
                    {
                        step = length / bulkAddRecords;
                    }
                    else
                    {
                        step = (int)(length / bulkAddRecords) + 1;
                    }

                    //������ʶ����
                    string bulkIdentityVal = string.Empty;

                    int start = 0;
                    int affecedRows = 0;
                    for (int i = 0; i < step; i++)
                    {
                        start = bulkAddRecords * i;
                        int stepSize = 0;
                        //�ж��Ƿ�Ϊ���һ��
                        if (step - i - 1 == 0)
                        {
                            stepSize = length - start;
                        }
                        else
                        {
                            stepSize = bulkAddRecords;
                        }
                        StringBuilder insertSql = new StringBuilder();
                        insertSql.Append($" insert into {tempTableName} values ");
                        //������������С�ֶ�β������ݿ�
                        for (int j = start; j < start + stepSize; j++)
                        {
                            IList<string> vals = new List<string>();
                            foreach (var columnName in columnNames)
                            {
                                var prop = properties.First(t => t.Name == columnName);
                                var propVal = prop.GetValue(models[j]);
                                vals.Add(GetMySqlDataType(prop.PropertyType, propVal));
                                if (columnName == nameof(IBulkModel.BulkIdentity) && string.IsNullOrWhiteSpace(bulkIdentityVal))
                                {
                                    bulkIdentityVal = vals[vals.Count - 1];
                                }
                            }
                            //ת������,�ָ��ֵ���ַ���
                            string strVal = string.Join(",", vals);
                            insertSql.Append($"({strVal}),");
                        }
                        insertSql = insertSql.Remove(insertSql.Length - 1, 1);
                        insertSql.Append(";");
                        affecedRows += client.Ado.ExecuteCommand(insertSql.ToString());

                    }
                    //�������ɹ��������봫���������һ��
                    if (affecedRows == length)
                    {

                        //1.���ݲ�������2������Ŀ���������ͣ�3����ʱ��������ͣ�4���������������α�ʶ
                        afterAction?.Invoke(client, modelType, typeof(TTarget), bulkIdentityVal);
                        //�ύ����
                        client.Ado.CommitTran();
                    }
                    else
                    {
                        //�ع�����
                        client.Ado.RollbackTran();
                    }
                }
                catch (Exception ex)
                {
                    client.Ado.RollbackTran();
                    throw ex;
                }
                finally
                {
                    client.Close();
                }
                return true;
            }
            else
            {
                throw new Exception($"��{nameof(TSource)}��û�п�ʵ�����Ĺ�������");
            }
        }

        private static string GetMySqlDataType(Type propValType, object val)
        {
            if (val == null)
            {
                return "NULL";
            }

            else if (propValType == typeof(int) || propValType == typeof(int?))
            {
                return val.ToString();
            }
            else if (propValType == typeof(long) || propValType == typeof(long?))
            {
                return val.ToString();
            }
            else if (propValType == typeof(decimal) || propValType == typeof(decimal?))
            {
                return val.ToString();
            }
            else if (propValType == typeof(double) || propValType == typeof(double?))
            {
                return val.ToString();
            }
            else if (propValType == typeof(float) || propValType == typeof(float?))
            {
                return val.ToString();
            }
            else if (propValType == typeof(DateTime) || propValType == typeof(DateTime?))
            {
                return $"'{val.ToString()}'";
            }
            else if (propValType == typeof(Guid) || propValType == typeof(Guid?))
            {
                return $"'{val.ToString()}'";
            }
            else if (propValType == typeof(string))
            {
                return $"'{val.ToString()}'";
            }
            else if (propValType == typeof(bool) || propValType == typeof(bool?))
            {
                return val.ToString().ToLower();
            }
            return "NULL";
        }

        private static string GetMySqlDbType(Type propType)
        {
            if (propType == typeof(int) || propType == typeof(int?))
            {
                return "int";
            }
            else if (propType == typeof(long) || propType == typeof(long?))
            {
                return "bigint";
            }
            else if (propType == typeof(decimal) || propType == typeof(decimal?))
            {
                return "decimal";
            }
            else if (propType == typeof(double) || propType == typeof(double?))
            {
                return "double";
            }
            else if (propType == typeof(float) || propType == typeof(float?))
            {
                return "float";
            }
            else if (propType == typeof(DateTime) || propType == typeof(DateTime?))
            {
                return "datetime";
            }
            else if (propType == typeof(Guid) || propType == typeof(Guid?))
            {
                return "char(36)";
            }
            else if (propType == typeof(string))
            {
                return "varchar(255)";
            }
            else if (propType == typeof(bool) || propType == typeof(bool?))
            {
                return "bit";
            }
            return string.Empty;
        }
    }
}