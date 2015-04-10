﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace GrabbingParts.DAL.DataAccessCenter
{
    public static class DataCenter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string connectionString = ConfigurationManager.ConnectionStrings["WXH"].ToString();

        public static void InsertDataToDatabase(DataTable dt)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.UseInternalTransaction))
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

                    log.DebugFormat("插入数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void InsertDataToProductSpecTable(DataTable dt)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.UseInternalTransaction))
                {
                    bulkCopy.DestinationTableName = "dbo.[产品规格]";//目标表，就是说您将要将数据插入到哪个表中去
                    bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                    bulkCopy.ColumnMappings.Add("PN", "产品编号");
                    bulkCopy.ColumnMappings.Add("Name", "规格名称");
                    bulkCopy.ColumnMappings.Add("Content", "规格内容");

                    Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                    stopwatch.Start();//跑表开始

                    bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                    log.DebugFormat("插入[产品规格]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void InsertDataToSupplier(DataTable dt)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.UseInternalTransaction))
                {
                    bulkCopy.DestinationTableName = "dbo.[供应商资料]";//目标表，就是说您将要将数据插入到哪个表中去
                    bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                    bulkCopy.ColumnMappings.Add("Supplier", "Supplier");
                    bulkCopy.ColumnMappings.Add("WebUrl", "WebUrl");

                    Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                    stopwatch.Start();//跑表开始

                    bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                    log.DebugFormat("插入[供应商资料]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void InsertDataToManufacturer(DataTable dt)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.UseInternalTransaction))
                {
                    bulkCopy.DestinationTableName = "dbo.[厂家资料]";//目标表，就是说您将要将数据插入到哪个表中去
                    bulkCopy.ColumnMappings.Add("GUID", "GUID");//数据源中的列名与目标表的属性的映射关系
                    bulkCopy.ColumnMappings.Add("Manufacturer", "Manufacturer");                    

                    Stopwatch stopwatch = new Stopwatch();//跑表，该类可以进行时间的统计

                    stopwatch.Start();//跑表开始

                    bulkCopy.WriteToServer(dt);//将数据源数据写入到目标表中

                    log.DebugFormat("插入[厂家资料]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static void InsertDataToProductInfo(DataTable dt)
        {
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.UseInternalTransaction))
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

                    log.DebugFormat("插入[厂家资料]数据所用时间:{0}ms", stopwatch.ElapsedMilliseconds);
                }
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}