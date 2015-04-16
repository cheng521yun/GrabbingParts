using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace GrabbingParts.DAL.DataAccessCenter
{
    public static class DataCenter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("WXH");
        private static string connectionString = ConfigurationManager.ConnectionStrings["WXH"].ToString();
        private static Object obj = new Object();

        public static void ExecuteTransaction(DataTable productSpecDataTable,
            DataTable manufacturerDataTable, DataTable productInfoDataTable)
        {
            lock(obj)
            {
                SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                SqlCommand command = conn.CreateCommand();

                SqlTransaction transaction = null;
                transaction = conn.BeginTransaction();
                command.Connection = conn;
                command.Transaction = transaction;

                try
                {
                    log.InfoFormat("插入[产品规格]记录数: {0}", productSpecDataTable.Rows.Count);
                    InsertDataToProductSpecTable(conn, transaction, productSpecDataTable);

                    log.InfoFormat("插入[厂家资料]记录数: {0}", manufacturerDataTable.Rows.Count);
                    InsertDataToManufacturer(conn, transaction, manufacturerDataTable);

                    log.InfoFormat("插入[产品资料]记录数: {0}", productInfoDataTable.Rows.Count);
                    InsertDataToProductInfo(conn, transaction, productInfoDataTable);

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    transaction.Rollback();
                }
            }
        }

        public static void InsertDataToCategory(List<DataTable> categoryDataTables)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand command = conn.CreateCommand();

            SqlTransaction transaction = null;
            transaction = conn.BeginTransaction();
            command.Connection = conn;
            command.Transaction = transaction;

            try
            {
                for (int i = 0; i < 4; i++)
                {
                    log.InfoFormat("插入第{0}层[产品分类]记录数: {1}", i, categoryDataTables[i].Rows.Count);
                    InsertDataForEachCategory(conn, transaction, categoryDataTables[i]);
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                log.Error(ex);
                transaction.Rollback();
            }
        }

        private static void InsertDataForEachCategory(SqlConnection conn, SqlTransaction transaction, DataTable dt)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = "dbo.[产品分类]";//目标表，就是说您将要将数据插入到哪个表中去
                bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                bulkCopy.ColumnMappings.Add("Name", "名称");
                bulkCopy.ColumnMappings.Add("ParentID", "父ID");
                bulkCopy.ColumnMappings.Add("Comment", "备注");

                //bulkCopy.BatchSize = 3;
                Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                stopwatch.Start();//跑表开始

                bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                log.InfoFormat("插入数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        public static void InsertDataToProductSpecTable(SqlConnection conn, SqlTransaction transaction, DataTable dt)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = "dbo.[产品规格]";//目标表，就是说您将要将数据插入到哪个表中去
                bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                bulkCopy.ColumnMappings.Add("PN", "产品编号");
                bulkCopy.ColumnMappings.Add("Name", "规格名称");
                bulkCopy.ColumnMappings.Add("Content", "规格内容");

                Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                stopwatch.Start();//跑表开始

                bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                log.InfoFormat("插入[产品规格]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        public static void InsertDataToSupplier(DataTable dt)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlCommand command = conn.CreateCommand();

            SqlTransaction transaction = null;
            transaction = conn.BeginTransaction();
            command.Connection = conn;
            command.Transaction = transaction;

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.[供应商资料]";//目标表，就是说您将要将数据插入到哪个表中去
                    bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                    bulkCopy.ColumnMappings.Add("Supplier", "Supplier");
                    bulkCopy.ColumnMappings.Add("WebUrl", "WebUrl");

                    Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                    stopwatch.Start();//跑表开始

                    bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                    log.InfoFormat("插入[供应商资料]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
                }

                transaction.Commit();
            }
            catch(Exception ex)
            {
                log.Error(ex);
                transaction.Rollback();
            }
        }

        public static void InsertDataToManufacturer(SqlConnection conn, SqlTransaction transaction, DataTable dt)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = "dbo.[厂家资料]";//目标表，就是说您将要将数据插入到哪个表中去
                bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                bulkCopy.ColumnMappings.Add("Manufacturer", "Manufacturer");

                Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                stopwatch.Start();//跑表开始

                bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                log.InfoFormat("插入[厂家资料]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        public static void InsertDataToProductInfo(SqlConnection conn, SqlTransaction transaction, DataTable dt)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = "dbo.[产品资料]";//目标表，就是说您将要将数据插入到哪个表中去
                bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                bulkCopy.ColumnMappings.Add("PN", "产品编号");
                bulkCopy.ColumnMappings.Add("Manufacturer", "制造商");
                bulkCopy.ColumnMappings.Add("ManufacturerID", "制造商ID");
                bulkCopy.ColumnMappings.Add("Description", "描述");
                bulkCopy.ColumnMappings.Add("Packing", "包装");
                bulkCopy.ColumnMappings.Add("Type1", "类别1GUID");
                bulkCopy.ColumnMappings.Add("Type2", "类别2GUID");
                bulkCopy.ColumnMappings.Add("Type3", "类别3GUID");
                bulkCopy.ColumnMappings.Add("Type4", "类别4GUID");
                bulkCopy.ColumnMappings.Add("Datasheets", "Datasheets");
                bulkCopy.ColumnMappings.Add("Image", "相片");

                Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                stopwatch.Start();//跑表开始

                bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中
                
                log.InfoFormat("插入[产品资料]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
            }
        }
    }
}