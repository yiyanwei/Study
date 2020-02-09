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
        public static bool BulkAddOrUpdate<TModel>(this IBulkAddOrUpdate bulk, IList<TModel> models, string tableName, int bulkAddRecords = 1000, bool isSameUpdate = false, string uniqueFieldName = null) where TModel : new()
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

            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new Exception("δ�ṩ���ݿ����");
            }

            if (isSameUpdate)
            {
                if (string.IsNullOrWhiteSpace(uniqueFieldName))
                {
                    throw new Exception("��isSameUpdateΪtrue������£�δ�ṩΨһ�����ֶ���");
                }
            }

            var client = bulk.Client;
            //ʵ������
            Type modelType = typeof(TModel);
            var properties = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties != null && properties.Count() > 0)
            {

                try
                {
                    //��������
                    client.Ado.BeginTran();
                    string tempTableName = $"{tableName}_{DateTime.Now.ToString("yyyyMMdd")}";
                    StringBuilder sbTempTable = new StringBuilder();
                    //�������ݿ��sql�ַ���
                    sbTempTable.Append($" create temporary table if not exists {tempTableName}(");
                    IList<string> columnNames = new List<string>();
                    //List<DbColumnInfo> columns = new List<DbColumnInfo>();
                    foreach (var prop in properties)
                    {
                        //DbColumnInfo column  = new DbColumnInfo()
                        sbTempTable.Append($" {prop.Name} {GetMySqlDbType(prop.PropertyType)},");
                        columnNames.Add(prop.Name);
                    }
                    sbTempTable = sbTempTable.Remove(sbTempTable.Length - 1, 1);
                    sbTempTable.Append(" ) ");
                    //������ʱ��
                    client.Ado.ExecuteCommand(sbTempTable.ToString());

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
                            }
                            //ת������,�ָ��ֵ���ַ���
                            string strVal = string.Join(",", vals);
                            insertSql.Append($"({strVal}),");
                        }
                        insertSql = insertSql.Remove(insertSql.Length - 1, 1);
                        insertSql.Append(";");
                        affecedRows +=  client.Ado.ExecuteCommand(insertSql.ToString());

                    }
                    //�������ɹ��������봫���������һ��
                    if (affecedRows == length)
                    {
                        //�ж��Ƿ�ֱ��������ݵ�ʵ�����ݱ���
                        StringBuilder synchronousData = new StringBuilder();
                        if (!isSameUpdate)
                        {
                            synchronousData.Append($" insert into {tableName} ({string.Join(",", columnNames)}) select {string.Join(",", columnNames)} from {tempTableName}");
                        }
                        else
                        {
                            synchronousData.Append($" insert into {tableName} ({string.Join(",", columnNames)}) select {string.Join(",", columnNames)} from {tempTableName}");
                        }
                        //��������
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
                throw new Exception($"��{nameof(TModel)}��û�п�ʵ�����Ĺ�������");
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