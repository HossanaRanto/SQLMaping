using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLMaping
{
    /// <summary>
    /// Extension pour manipuler les base données
    /// </summary>
    public static class SqlMaping
    {
        public static void test(DbConnection dbConnection)
        {
            IEnumerable<dynamic> list = Enumerable.Empty<dynamic>();
        }
        /// <summary>
        /// Importer les données provenant de la base
        /// </summary>
        /// <typeparam name="T">Le type de données ou objet</typeparam>
        /// <param name="dbConnection">Connection au base de donnée (MySql ou Sqlite ou SqlServer, ...)</param>
        /// <param name="query">Commande à executer</param>
        /// <param name="parameter">Paramètre dans la commande executé</param>
        /// <returns></returns>
        public static IList<T> Query<T>(this DbConnection dbConnection, string query, object parameter = null)
        {
            var DbCommandcon = dbConnection.CreateCommand();
            DbCommandcon.CommandText = query;
            Open(dbConnection);
            if (parameter != null)
            {
                addparam(DbCommandcon, parameter);
            }
            DbDataReader reader = DbCommandcon.ExecuteReader();
            IList<T> list = new List<T>();
            var tp = list.GetType();
            while (reader.Read())
            {
                var instance = Activator.CreateInstance(typeof(T));
                if (typeof(T) == typeof(string))
                {
                    instance = reader.GetString(0);
                }
                if (typeof(T) == typeof(int))
                {
                    instance = reader.GetInt32(0);
                }
                foreach (var p in typeof(T).GetProperties().Where(p => !p.GetMethod.IsStatic && p.CanWrite))
                {
                    if (!p.GetMethod.IsStatic)
                    {
                        var ordinal = -1;
                        try
                        {
                            ordinal = reader.GetOrdinal(p.Name);
                        }
                        catch { ordinal = -1; }
                        if (ordinal >= 0)
                        {
                            var t = p.PropertyType;
                            if (!reader.IsDBNull(ordinal))
                            {
                                if (t == typeof(int))
                                {
                                    p.SetValue(instance, reader.GetInt32(ordinal));
                                }
                                if (t == typeof(short))
                                {
                                    p.SetValue(instance, reader.GetInt16(ordinal));
                                }
                                if (t == typeof(double))
                                {
                                    p.SetValue(instance, reader.GetDouble(ordinal));
                                }
                                if (t == typeof(float))
                                {
                                    p.SetValue(instance, (float)reader.GetInt32(ordinal));
                                }
                                if (t == typeof(bool))
                                {
                                    p.SetValue(instance, reader.GetBoolean(ordinal));
                                }
                                if (t == typeof(string))
                                {
                                    p.SetValue(instance, reader.GetString(ordinal));
                                }
                                if (t == typeof(DateTime))
                                {
                                    p.SetValue(instance, reader.GetDateTime(ordinal));
                                }

                            }

                        }
                    }
                }
                list.Add((T)instance);
            }
            Close(dbConnection);
            return list;
        }

        public static IList<T> QueryWithSimpleCondition<T>(this DbConnection dbConnection, string column, object value)
        {
            return dbConnection.Query<T>($"SELECT * FROM {typeof(T).Name} WHERE {column}=@value", new { value = value });
        }
        public static IList<T> GetAll<T>(this DbConnection dbConnection)
        {
            return dbConnection.Query<T>($"SELECT * FROM {typeof(T).Name}");
        }
        public static int Insert<T>(this DbConnection dbConnection, T value, params string[] properties)
        {
            var class_attribute = (ClassProperty)typeof(T).GetCustomAttribute(typeof(ClassProperty));
            string[] prts = class_attribute?.DefaultProperties;
            bool canbeinserted(string property)
            {
                if (prts.Count() == 0)
                {
                    return true;
                }
                else
                {
                    return !prts.Contains(property);
                }
            }
            var properties_default = typeof(T).GetProperties();
            //tous les propriétés à inserer dans la base
            List<PropertyInfo> prt = new List<PropertyInfo>();
            bool ispropertydefault = true;
            if (properties.Count() > 0)
            {
                ispropertydefault = false;
            }
            else
            {
                ispropertydefault = true;
            }
            if (ispropertydefault)
            {
                foreach (var p in properties_default)
                {
                    var attrib = p.GetCustomAttribute(typeof(FieldProperty));
                    bool caninsert = true;
                    caninsert = canbeinserted(p.Name);
                    if (attrib != null && ((FieldProperty)attrib).OnlyInSelect)
                    {
                        caninsert = false;
                    }
                    if (p.CanRead && !p.GetMethod.IsStatic
                        && caninsert)
                    {
                        prt.Add(p);
                    }
                }
            }
            else
            {
                foreach (var p in properties_default)
                {
                    if (p.CanRead && !p.GetMethod.IsStatic &&
                        p.PropertyType != typeof(IList<>) &&
                        properties.Contains(p.Name))
                    {
                        prt.Add(p);
                    }
                }
            }
            string sql = $"INSERT INTO {typeof(T).Name}(";
            for (int i = 0; i < prt.Count; i++)
            {
                if (i != prt.Count - 1)
                {
                    sql += $"{prt[i].Name},";
                }
                else
                {
                    sql += $"{prt[i].Name}) VALUES(";
                }
            }

            for (int i = 0; i < prt.Count; i++)
            {
                if (i != prt.Count - 1)
                {
                    sql += $"@{prt[i].Name},";
                }
                else
                {
                    sql += $"@{prt[i].Name})";
                }
            }
            return dbConnection.Execute(sql, value);
        }
        public static int Insert<T>(this DbConnection dbConnection, T value, string propretynull)
        {
            string[] property = new string[value.GetType().GetProperties().Count() - 1];
            int index = 0;
            foreach (var p in value.GetType().GetProperties())
            {
                if (p.Name != propretynull)
                {
                    property[index] = p.Name;
                    index++;
                }
            }
            return dbConnection.Insert(value, property);
        }
        public static int Update<T>(this DbConnection dbConnection, T value, string updateonproperty, params string[] properties)
        {
            var class_attribute = (ClassProperty)typeof(T).GetCustomAttribute(typeof(ClassProperty));
            string[] prts = class_attribute.DefaultProperties;
            bool canbeupdated(string property)
            {
                if (prts.Count() == 0)
                {
                    return true;
                }
                else
                {
                    return !prts.Contains(property);
                }
            }
            var properties_default = typeof(T).GetProperties();
            //tous les propriétés à inserer dans la base
            List<PropertyInfo> prt = new List<PropertyInfo>();
            bool ispropertydefault = true;
            if (properties.Count() > 0)
            {
                ispropertydefault = false;
            }
            else
            {
                ispropertydefault = true;
            }
            if (ispropertydefault)
            {
                foreach (var p in properties_default)
                {
                    var attrib = p.GetCustomAttribute(typeof(FieldProperty));
                    bool caninsert = true;
                    caninsert = canbeupdated(p.Name);
                    if (attrib != null && ((FieldProperty)attrib).OnlyInSelect)
                    {
                        caninsert = false;
                    }
                    if (p.CanRead && !p.GetMethod.IsStatic
                        && caninsert)
                    {
                        prt.Add(p);
                    }
                }
            }
            else
            {
                foreach (var p in properties_default)
                {
                    if (p.CanRead && !p.GetMethod.IsStatic)
                    {
                        prt.Add(p);
                    }
                }
            }
            string sql = $"UPDATE {typeof(T).Name} SET ";
            for (int i = 0; i < prt.Count; i++)
            {
                if (i != prt.Count - 1)
                {
                    sql += $"{prt[i].Name}=@{prt[i].Name},";
                }
                else
                {
                    sql += $"{prt[i].Name}=@{prt[i].Name} ";
                }
            }
            sql += $"WHERE {updateonproperty}=@{updateonproperty}";

            return dbConnection.Execute(sql, value);
        }
        public static IList<T> Max<T>(this DbConnection dbConnection, string fromproperty)
        {
            string query = $"SELECT * FROM {typeof(T).Name} WHERE {fromproperty}=(SELECT Max({fromproperty}) FROM {typeof(T).Name})";
            return dbConnection.Query<T>(query);
        }
        public static bool VerifyIfExist<T>(this DbConnection dbConnection, T value, string frompropery)
        {
            string query = $"SELECT * FROM {typeof(T).Name} WHERE {frompropery}=@{frompropery}";
            var result = dbConnection.Query<T>(query, value);
            if (result.Count() > 0)
            {
                return true;
            }
            return false;
        }
        public static int Execute(this DbConnection dbConnection, string query, object parameter = null, string list_parameter = null)
        {
            int result = 0;
            var DbCommandcon = dbConnection.CreateCommand();
            DbCommandcon.CommandText = query;
            if (parameter != null)
            {
                if (parameter.GetType() == typeof(IList<>))
                {
                    foreach (var item in (IList<object>)parameter)
                    {
                        DbCommandcon.Parameters.Add(item);
                        DbCommandcon.Parameters[0].ParameterName = list_parameter;
                        Open(dbConnection);
                        int rst = DbCommandcon.ExecuteNonQuery();
                        result += rst;
                    }
                }
                else
                {
                    addparam(DbCommandcon, parameter);
                    Open(dbConnection);
                    DbCommandcon.ExecuteNonQuery();
                }
            }
            else result = DbCommandcon.ExecuteNonQuery();
            Close(dbConnection);
            return result;
        }
        static void Open(DbConnection dbConnection)
        {
            if (dbConnection.State == ConnectionState.Closed)
            {
                dbConnection.Open();
            }
        }
        static void Close(DbConnection dbConnection)
        {
            if (dbConnection.State == ConnectionState.Open)
            {
                dbConnection.Close();
            }
        }
        static void addparam(DbCommand dbCommand, object parameter)
        {
            var properties = parameter.GetType().GetProperties();
            foreach (var p in properties)
            {
                var param = dbCommand.CreateParameter();
                param.ParameterName = p.Name;
                param.Value = p.GetValue(parameter);
                dbCommand.Parameters.Add(param);
            }
        }
    }
}

