using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Configuration;

//fix permissions for folder  for sql server to read new file
namespace CollationChange
{
    public partial class CollationChanges : Form
    {
        private SqlConnection sqlConn = new SqlConnection();
        private string sqlConnString = String.Format("Server={0};Database=master;User ID={1};Password={2};", ConfigurationManager.AppSettings["server"], ConfigurationManager.AppSettings["user"], ConfigurationManager.AppSettings["pass"]);
        private SqlCommand sqlComm;
        private string myDir = Environment.GetEnvironmentVariable("TEMP") + "\\RDBS\\Satage1\\";
        private string myDirSecondStage = Environment.GetEnvironmentVariable("TEMP") + "\\RDBS\\Satage2\\";
        private ErrorLogging myErrors = new ErrorLogging();
        public CollationChanges()
        {
            InitializeComponent();
            if (Directory.Exists(myDir) == false)
            {
                Directory.CreateDirectory(myDir);                
                DirectoryInfo dInfo = new DirectoryInfo(myDir);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
            }
            if (Directory.Exists(myDirSecondStage) == false)
            {
                Directory.CreateDirectory(myDirSecondStage);
                DirectoryInfo dInfo = new DirectoryInfo(myDirSecondStage);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            sqlConn.ConnectionString = sqlConnString;
            loadData(lvDBCollation, cbCollation, sqlConn);
        }
        private class CollationInfo
        {
            public string CollationName { get; set; }
            public int CollationCodePage { get; set; }
            public string CollationInfoData { get; set; }

            public CollationInfo(string colName, int CodePage, string info )
            {
                this.CollationName = colName;
                this.CollationCodePage = CodePage;  
                this.CollationInfoData = info;
            }
            public override string ToString()
            {
                return this.CollationName;
            }           
        }
        private class DatabaseInfo
        {
            public string DatabaseName { get; set; }
            public string DatabaseInfoData { get; set; }


            public DatabaseInfo(string dbname, string info)
            {
                this.DatabaseName = dbname;
                this.DatabaseInfoData = info;
            }
            public override string ToString()
            {
                return this.DatabaseName;
            }  
        }
        private void loadData(ListView mylist, ComboBox myCombo, SqlConnection conn)
        {
            string sqlQuery = "select name, COLLATIONPROPERTY(name, 'CodePage') as Code_Page, description from sys.fn_HelpCollations()";
            try 
            {       
              conn.Open();
              SqlCommand myCmd = new SqlCommand(sqlQuery,conn);
              myCmd.CommandTimeout = 0;
              SqlDataReader myRd = myCmd.ExecuteReader();
              while (myRd.Read())
              {
                cbCollation.Items.Add(new CollationInfo(myRd[0].ToString(),Convert.ToInt32(myRd[1].ToString()),myRd[2].ToString()));
              }
              myRd.Close();
              sqlQuery = "select SERVERPROPERTY ('Collation')";
              myCmd = new SqlCommand(sqlQuery, conn);
              myRd = myCmd.ExecuteReader();
              while (myRd.Read())
              {
                  lbCollation.Text = myRd[0].ToString();
              }
              myRd.Close();
              for (int i = 0; i < myCombo.Items.Count; i++ )
              {
                  CollationInfo collation = (CollationInfo)myCombo.Items[i];
                  if (lbCollation.Text == collation.CollationName)
                  {
                      myCombo.SelectedItem = myCombo.Items[i];
                  }
              }
              sqlQuery = "select name dbName,collation_name [Collation] from sys.databases";
              myCmd = new SqlCommand(sqlQuery, conn);
              myRd = myCmd.ExecuteReader();
              while (myRd.Read())
              {
                  ListViewItem myItem = new ListViewItem();
                  myItem.Text = myRd[0].ToString();
                  myItem.Tag = new DatabaseInfo(myRd[0].ToString(), myRd[1].ToString());
                  myItem.SubItems.Add(myRd[1].ToString());
                  mylist.Items.Add(myItem);
              } 
              myRd.Close();
              sqlQuery = "exec sp_configure 'show advanced options', 1; \n" 
                        +"RECONFIGURE; \n"
                        + "exec sp_configure 'Ole Automation Procedures', 1;\n" 
                        +"RECONFIGURE;";
              myCmd = new SqlCommand(sqlQuery, conn);
              myCmd.ExecuteNonQuery();
              conn.Close();
            }   
            catch (Exception ex)
            {
                conn.Close();
                MessageBox.Show("Error: "  + ex.Message);
            }      
        }            
        private void cbCollation_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox myCombo = sender as ComboBox;
            if (myCombo.SelectedIndex != -1)
            {
                CollationInfo myCollation = (CollationInfo)myCombo.SelectedItem;
                lbCodepage.Text = "Codepage: " + myCollation.CollationCodePage.ToString();
                lbColInfo.Text = myCollation.CollationInfoData;
            }
            else
                lbColInfo.Text = " ";
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }
        private void executeFiles(string targetDir,string targetDirSecondStage)
        {
            DirectoryInfo myScriptsDir = new DirectoryInfo(targetDir);
            foreach (FileInfo file in myScriptsDir.GetFiles())
            {
                if ((file.Extension == ".sql") || (file.Extension == ".SQL"))
                {
                    Server server = new Server(new ServerConnection(sqlConn));
                    try
                    {
                        string script = file.OpenText().ReadToEnd();
                        server.ConnectionContext.ExecuteNonQuery(script);
                        server.ConnectionContext.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        myErrors.AddErrorToLog(ex.Source);
                        myErrors.AddErrorToLog(ex.Message);
                        myErrors.AddErrorToLog(ex.InnerException.ToString());
                        myErrors.AddErrorToLog(ex.StackTrace);
                        myErrors.AddErrorToLog(ex.TargetSite.ToString());
                    }
                }
            }
            DirectoryInfo myRecoveryScriptsDir = new DirectoryInfo(targetDirSecondStage);
            foreach (FileInfo file in myRecoveryScriptsDir.GetFiles())
            {
                if ((file.Extension == ".sql") || (file.Extension == ".SQL"))
                {
                    Server server = new Server(new ServerConnection(sqlConn));            
                    try
                    {
                        string myFileDir = file.FullName;
                        StreamReader myStream = new StreamReader(file.FullName, Encoding.GetEncoding(1251));
                        string myFileData = myStream.ReadToEnd();           
                        server.ConnectionContext.ExecuteNonQuery(myFileData);
                        myStream.Close();
                        server.ConnectionContext.Disconnect();                          
                    }
                    catch (Exception ex)
                    {
                        myErrors.AddErrorToLog(ex.Source);
                        myErrors.AddErrorToLog(ex.Message);
                        myErrors.AddErrorToLog(ex.InnerException.ToString());
                        myErrors.AddErrorToLog(ex.StackTrace);
                        myErrors.AddErrorToLog(ex.TargetSite.ToString());
                    }
                }
            }
        }
        private void btCleanSelection_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lvDBCollation.Items.Count; i++)
            {
                lvDBCollation.Items[i].Checked = false;
            }                
        }
        private void btExecute_Click(object sender, EventArgs e)
        {
            if(lvDBCollation.CheckedItems.Count > 0)
            {
            for (int i = 0; i < lvDBCollation.Items.Count; i++)
            {
                if(lvDBCollation.Items[i].Checked == true)
                {
                    try
                    {
                        string query = CollationQuery(lvDBCollation.Items[i].Text, myDirSecondStage, cbCollation.SelectedItem.ToString());
                        StreamWriter mySream = new StreamWriter(myDir + lvDBCollation.Items[i].Text + "_step1.sql", false);
                        mySream.WriteLine(query);
                        mySream.Close();
                    }
                    catch(Exception ex)
                    {
                        myErrors.AddErrorToLog(ex.Source);
                        myErrors.AddErrorToLog(ex.Message);
                        myErrors.AddErrorToLog(ex.InnerException.ToString());
                        myErrors.AddErrorToLog(ex.StackTrace);
                        myErrors.AddErrorToLog(ex.TargetSite.ToString());
                    }
                }
             }      
             executeFiles(myDir,myDirSecondStage);                   
             MessageBox.Show("Collation changed on selected databases.");
             Application.Exit();
            }
        }
        private string CollationQuery(string DBName, string FileDir, string CollationName)
        {
            string myRealQuery = "Use ["+DBName+"]" + "\n"
		                       +"--0. Create proc " + "\n"
				               +"          /****** Object:  StoredProcedure [dbo].[usp_OLEWriteFile]    Script Date: 28.03.2016 15:47:24 ******/ " + "\n"
				               +"         IF object_id('usp_OLEWriteFile') IS NOT NULL " + "\n"
				               +"         EXEC ('DROP PROCEDURE [dbo].[usp_OLEWriteFile]') " + "\n"
				               +"         GO " + "\n"
				               +"         /****** Object:  StoredProcedure [dbo].[usp_OLEWriteFile]    Script Date: 28.03.2016 15:47:24 ******/ " + "\n"
				               +"         SET ANSI_NULLS ON " + "\n"
				               +"         GO" + "\n"
				               +"         SET QUOTED_IDENTIFIER ON " + "\n"
				               +"         GO" + "\n"
				               +"         --Usage " + "\n"
				               +"         --DECLARE @destinationpath NVARCHAR(1000)  " + "\n"
				               +"         --DECLARE @destinationfilename NVARCHAR(1000)  " + "\n"
				               +"         --SET @destinationpath = N'C:\\SQL Scripts'   " + "\n"
				               +"         --SET @destinationfilename = @destinationpath + '\' + N'filename.txt'  " + "\n"
  				               +"         --exec utils.usp_OLEwritefile  @FileName =@destinationfilename  " + "\n"
				               +"         --, @TextData ='blah! blah! blah!'  " + "\n"
				               +"        --, @FileAction = 'APPEND' -- CREATENEW or APPEND (to allow appends if the file exists, both options create the file if it doesn't).  " + "\n"
				               +"         Create PROCEDURE [dbo].[usp_OLEWriteFile] (@FileName varchar(1000), @TextData NVARCHAR(MAX),@FileAction VARCHAR(12)) AS  " + "\n"
  				               +"         BEGIN  " + "\n"
				               +"         DECLARE @OLEfilesytemobject INT  " + "\n"
				               +"         DECLARE @OLEResult INT  " + "\n"
				               +"         DECLARE @FileID INT  " + "\n"
				               +"         EXECUTE @OLEResult =   " + "\n"
				               +"         sp_OACreate 'Scripting.FileSystemObject', @OLEfilesytemobject OUT  " + "\n"
				               +"         IF @OLEResult <> 0   " + "\n"
				               +"         PRINT 'Error: Scripting.FileSystemObject'  " + "\n"
				               +"         -- check if file exists  " + "\n"
				               +"         EXEC sp_OAMethod @OLEfilesytemobject, 'FileExists', @OLEresult OUT, @FileName   " + "\n"
				               +"         -- if file esists  " + "\n"
				               +"         IF (@OLEresult=1 AND @FileAction = 'APPEND') OR (@OLEresult=0)   " + "\n"
				               +"         BEGIN     " + "\n"
				               +"         IF (@FileAction = 'CREATENEW')  " + "\n"
				               +"         PRINT 'New file specified, creating...'  " + "\n"
				               +"         IF (@OLEresult=1 AND @FileAction = 'APPEND')   " + "\n"
				               +"         PRINT 'File exists, appending...'  " + "\n"
				               +"         IF (@OLEresult=0 AND @FileAction = 'APPEND')   " + "\n"
				               +"         PRINT 'File doesnt exist, creating...'     " + "\n"
				               +"         -- open file  " + "\n"
				               +"         EXECUTE @OLEResult = sp_OAMethod @OLEfilesytemobject, 'OpenTextFile', @FileID OUT,  " + "\n"
				               +"         @FileName, 8, 1  " + "\n"
				               +"         IF @OLEResult <>0 PRINT 'Error: OpenTextFile'  " + "\n"
  				               +"         -- write Text1 to the file  " + "\n"
				               +"         EXECUTE @OLEResult = sp_OAMethod @FileID, 'WriteLine', Null, @TextData  " + "\n"
				               +"         IF @OLEResult <> 0   " + "\n"
				               +"         PRINT 'Error : WriteLine'  " + "\n"
				               +"         ELSE  " + "\n"
				               +"         PRINT 'Success'   " + "\n"
				               +"         END  " + "\n"
				               +"         IF (@OLEresult=1 AND @FileAction = 'CREATENEW')  " + "\n"
				               +"         PRINT 'File Exists, specify APPEND if this is the desired action'  " + "\n"
  				               +"         EXECUTE @OLEResult = sp_OADestroy @FileID  " + "\n"
				               +"         EXECUTE @OLEResult = sp_OADestroy @OLEfilesytemobject  " + "\n"  
				               +"         END  " + "\n"
				               +"         GO" + "\n"
                               +"         Declare @dbuse nvarchar(max)" + "\n"
                               +"         set @dbuse = 'USE [" + DBName + "]' + char(10) + 'GO' +char(10) " + "\n"
                               +"  exec dbo.usp_OLEWriteFile  @FileName = N'" + FileDir + DBName + ".sql', @TextData = @dbuse , @FileAction = 'APPEND'" + "\n"
		                       +" --1. START Generate Change Column Collation " + "\n"
		                       +" SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
		                       +" GO" + "\n"
		                       +" Begin transaction" + "\n"
		                       +" Declare @dbName as varchar(max)" + "\n"
		                       +" Declare @myCommand as varchar(max)" + "\n"
		                       +" Declare @fnName as varchar(max)" + "\n"
		                       +" DECLARE @destinationpath NVARCHAR(1000)  " + "\n"
		                       +" DECLARE @destinationfilename NVARCHAR(1000)  " + "\n"
		                       +" declare @toCollation sysname " + "\n"
		                       +" set @dbName = '"+DBName +".sql'" + "\n"
		                       +" SET @destinationpath = N'"+FileDir+"' " + "\n"  
		                       +" SET @destinationfilename = @destinationpath + '\' + @dbName " + "\n"
		                       +" SET @toCollation = '"+CollationName+"' -- Database default collate" + "\n"
		                       +" DECLARE myCommands CURSOR FOR  " + "\n"
		                       +" SELECT 'ALTER TABLE ' + QUOTENAME(INFORMATION_SCHEMA.COLUMNS.TABLE_NAME) +" + "\n"
		                       +" ' ALTER COLUMN ' + QUOTENAME(COLUMN_NAME) + ' ' + DATA_TYPE +" + "\n"
		                       +" CASE WHEN CHARACTER_MAXIMUM_LENGTH = -1 then '(max)'" + "\n"
		                       +" WHEN DATA_TYPE in ('text','ntext') then ''" + "\n"
		                       +" WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL " + "\n"
		                       +" THEN '('+(CONVERT(VARCHAR,CHARACTER_MAXIMUM_LENGTH)+')' )" + "\n"
		                       +" ELSE isnull(CONVERT(VARCHAR,CHARACTER_MAXIMUM_LENGTH),' ') END" + "\n"
		                       +" +' COLLATE ' + @toCollation + ' ' + CASE IS_NULLABLE" + "\n"
		                       +" WHEN 'YES' THEN 'NULL'" + "\n"
		                       +" WHEN 'NO' THEN 'NOT NULL' " + "\n"
		                       +" END + ';'" + "\n"
		                       +" FROM INFORMATION_SCHEMA.COLUMNS INNER JOIN INFORMATION_SCHEMA.TABLES" + "\n"
		                       +" ON INFORMATION_SCHEMA.COLUMNS.TABLE_NAME = INFORMATION_SCHEMA.TABLES.TABLE_NAME" + "\n"
		                       +" AND INFORMATION_SCHEMA.COLUMNS.TABLE_SCHEMA = INFORMATION_SCHEMA.TABLES.TABLE_SCHEMA" + "\n"
		                       +" WHERE DATA_TYPE IN ('varchar' ,'char','nvarchar','nchar','text','ntext')" + "\n"
		                       +" AND TABLE_TYPE = 'BASE TABLE'" + "\n"
		                       +" and COLLATION_NAME <> @toCollation ;" + "\n"
			                   +"     OPEN myCommands" + "\n"
			                   +"     FETCH NEXT FROM myCommands INTO @myCommand" + "\n"
			                   +"    WHILE @@FETCH_STATUS = 0" + "\n"
			                   +"     BEGIN				" + "\n"
				               +"         exec dbo.usp_OLEWriteFile  @FileName = @destinationfilename, @TextData = @myCommand , @FileAction = 'APPEND'" + "\n"
			                   +"     FETCH NEXT FROM myCommands INTO @myCommand" + "\n"
			                   +"     END" + "\n"
		                       +" CLOSE myCommands" + "\n"
		                       +" DEALLOCATE myCommands" + "\n"
		                       +" commit TRANSACTION;" + "\n"
		                       +" GO" + "\n"
		                       +" --1. END" + "\n"
                        +" --2. Start Generate_Primary_Key_Contraints" + "\n"
                        +" SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +" GO" + "\n"
                        +" Begin transaction" + "\n"
		                +"         Declare @dbName as varchar(max)" + "\n"
		                +"         Declare @myCommand as varchar(max)" + "\n"
		                +"         Declare @fnName as varchar(max)" + "\n"
		                +"         DECLARE @destinationpath NVARCHAR(1000)  " + "\n"
		                +"         DECLARE @destinationfilename NVARCHAR(1000)  " + "\n"
		                +"         declare @toCollation sysname " + "\n"
		                +"         set @dbName = '"+DBName +".sql'" + "\n"
		                +"         SET @destinationpath = N'"+FileDir+"' " + "\n"  
		                +"         SET @destinationfilename = @destinationpath + '\' + @dbName " + "\n"
		                +"         SET @toCollation = '"+CollationName+"' -- Database default collate" + "\n"
		                +"         --- SCRIPT TO GENERATE THE CREATION SCRIPT OF ALL PK AND UNIQUE CONSTRAINTS." + "\n"
		                +"        declare @SchemaName varchar(100)" + "\n"
		                +"        declare @TableName varchar(256)" + "\n"
		                +"        declare @IndexName varchar(256)" + "\n"
		                +"        declare @ColumnName varchar(100)" + "\n"
		                +"        declare @is_unique_constraint varchar(100)" + "\n"
		                +"        declare @IndexTypeDesc varchar(100)" + "\n"
		                +"        declare @FileGroupName varchar(100)" + "\n"
		                +"        declare @is_disabled varchar(100)" + "\n"
		                +"        declare @IndexOptions varchar(max)" + "\n"
		                +"        declare @IndexColumnId int" + "\n"
		                +"        declare @IsDescendingKey int " + "\n"
		                +"        declare @IsIncludedColumn int" + "\n"
		                +"        declare @TSQLScripCreationIndex varchar(max)" + "\n"
		                +"        declare @TSQLScripDisableIndex varchar(max)" + "\n"
		                +"        declare @is_primary_key varchar(100)" + "\n"
		                +"        declare CursorIndex cursor for" + "\n"
		                +"         select schema_name(t.schema_id) [schema_name], t.name, ix.name," + "\n"
		                +"         case when ix.is_unique_constraint = 1 then ' UNIQUE ' else '' END " + "\n"
			            +"            ,case when ix.is_primary_key = 1 then ' PRIMARY KEY ' else '' END " + "\n"
		                +"         , ix.type_desc," + "\n"
		                +"          case when ix.is_padded=1 then 'PAD_INDEX = ON, ' else 'PAD_INDEX = OFF, ' end" + "\n"
		                +"         + case when ix.allow_page_locks=1 then 'ALLOW_PAGE_LOCKS = ON, ' else 'ALLOW_PAGE_LOCKS = OFF, ' end" + "\n"
		                +"         + case when ix.allow_row_locks=1 then  'ALLOW_ROW_LOCKS = ON, ' else 'ALLOW_ROW_LOCKS = OFF, ' end" + "\n"
		                +"         + case when INDEXPROPERTY(t.object_id, ix.name, 'IsStatistics') = 1 then 'STATISTICS_NORECOMPUTE = ON, ' else 'STATISTICS_NORECOMPUTE = OFF, ' end" + "\n"
		                +"         + case when ix.ignore_dup_key=1 then 'IGNORE_DUP_KEY = ON, ' else 'IGNORE_DUP_KEY = OFF, ' end" + "\n"
		                +"           + 'SORT_IN_TEMPDB = OFF, FILLFACTOR =' + CAST(case when ix.fill_factor =0  then 1 else 2 end  AS VARCHAR(3)) AS IndexOptions" + "\n"
		                +"         , FILEGROUP_NAME(ix.data_space_id) FileGroupName" + "\n"
		                +"         from sys.tables t " + "\n"
		                +"         inner join sys.indexes ix on t.object_id=ix.object_id" + "\n"
		                +"         where ix.type>0 and  (ix.is_primary_key=1 or ix.is_unique_constraint=1) --and schema_name(tb.schema_id)= @SchemaName and tb.name=@TableName" + "\n"
		                +"         and t.is_ms_shipped=0 and t.name<>'sysdiagrams'" + "\n"
		                +"         order by schema_name(t.schema_id), t.name, ix.name" + "\n"
		                +"        open CursorIndex" + "\n"
		                +"        fetch next from CursorIndex into  @SchemaName, @TableName, @IndexName, @is_unique_constraint, @is_primary_key, @IndexTypeDesc, @IndexOptions, @FileGroupName" + "\n"
		                +"        while (@@fetch_status=0)" + "\n"
		                +"        begin" + "\n"
		                +"         declare @IndexColumns varchar(max)" + "\n"
		                +"         declare @IncludedColumns varchar(max)" + "\n"
		                +"         set @IndexColumns=''" + "\n"
		                +"         set @IncludedColumns=''" + "\n"
		                +"         declare CursorIndexColumn cursor for " + "\n"
		                +"         select col.name, ixc.is_descending_key, ixc.is_included_column" + "\n"
		                +"         from sys.tables tb " + "\n"
		                +"         inner join sys.indexes ix on tb.object_id=ix.object_id" + "\n"
		                +"         inner join sys.index_columns ixc on ix.object_id=ixc.object_id and ix.index_id= ixc.index_id" + "\n"
		                +"         inner join sys.columns col on ixc.object_id =col.object_id  and ixc.column_id=col.column_id" + "\n"
		                +"         where ix.type>0 and (ix.is_primary_key=1 or ix.is_unique_constraint=1)" + "\n"
		                +"         and schema_name(tb.schema_id)=@SchemaName and tb.name=@TableName and ix.name=@IndexName" + "\n"
		                +"         order by ixc.index_column_id" + "\n"
		                +"         open CursorIndexColumn " + "\n"
		                +"         fetch next from CursorIndexColumn into  @ColumnName, @IsDescendingKey, @IsIncludedColumn" + "\n"
		                +"         while (@@fetch_status=0)" + "\n"
		                +"         begin" + "\n"
		                +"          if @IsIncludedColumn=0 " + "\n"
			            +"            set @IndexColumns=@IndexColumns + @ColumnName  + case when @IsDescendingKey=1  then ' DESC, ' else  ' ASC, ' end" + "\n"
		                +"          else " + "\n"
		                +"           set @IncludedColumns=@IncludedColumns  + @ColumnName  +', ' " + "\n"
		                +"          fetch next from CursorIndexColumn into @ColumnName, @IsDescendingKey, @IsIncludedColumn" + "\n"
		                +"         end" + "\n"
		                +"         close CursorIndexColumn" + "\n"
		                +"          deallocate CursorIndexColumn" + "\n"
		                +"         set @IndexColumns = substring(@IndexColumns, 1, len(@IndexColumns)-1)" + "\n"
		                +"         set @IncludedColumns = case when len(@IncludedColumns) >0 then substring(@IncludedColumns, 1, len(@IncludedColumns)-1) else '' end" + "\n"
		                +"        --  print @IndexColumns" + "\n"
		                +"        --  print @IncludedColumns" + "\n"
		                +"        set @TSQLScripCreationIndex =''" + "\n"
		                +"        set @TSQLScripDisableIndex =''" + "\n"
		                +"        set  @TSQLScripCreationIndex='ALTER TABLE '+  QUOTENAME(@SchemaName) +'.'+ QUOTENAME(@TableName)+ ' ADD CONSTRAINT ' +  QUOTENAME(@IndexName) + @is_unique_constraint + @is_primary_key + +@IndexTypeDesc +  '('+@IndexColumns+') '+ " + "\n"
		                +"         case when len(@IncludedColumns)>0 then CHAR(13) +'INCLUDE (' + @IncludedColumns+ ')' else '' end + CHAR(13)+'WITH (' + @IndexOptions+ ') ON ' + QUOTENAME(@FileGroupName) + ';' + char(10) + 'GO' + char(10)" + "\n"
		                +"        --print @TSQLScripCreationIndex " + "\n"
		                +"        --print @TSQLScripDisableIndex" + "\n"
		                +"        set  @TSQLScripCreationIndex += + CHAR(13) +' ' + isnull(@TSQLScripDisableIndex,' ')" + "\n"
		                +"        -- Inset the satement in the sql file " + "\n"
		                +"        exec dbo.usp_OLEWriteFile  @FileName = @destinationfilename, @TextData= @TSQLScripCreationIndex , @FileAction = 'APPEND' " + "\n"
		                +"        fetch next from CursorIndex into  @SchemaName, @TableName, @IndexName, @is_unique_constraint, @is_primary_key, @IndexTypeDesc, @IndexOptions, @FileGroupName" + "\n"
		                +"        end" + "\n"
		                +"        close CursorIndex" + "\n"
		                +"        deallocate CursorIndex" + "\n"
                        +"        commit TRANSACTION;" + "\n"
                        +"        GO" + "\n"
                        +"--2. End" + "\n"
                        +"--3 Start .Generate ALL FOREIGN KEY CONSTRAINTS" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
                        +"Declare @dbName as varchar(max)" + "\n"
		                +"        Declare @myCommand as varchar(max)" + "\n"
		                +"        Declare @fnName as varchar(max)" + "\n"
		                +"        DECLARE @destinationpath NVARCHAR(1000)  " + "\n"
		                +"        DECLARE @destinationfilename NVARCHAR(1000)  " + "\n"
		                +"        declare @toCollation sysname " + "\n"
		                +"        set @dbName = '"+DBName +".sql'" + "\n"
		                +"        SET @destinationpath = N'"+FileDir+"' " + "\n"  
		                +"        SET @destinationfilename = @destinationpath + '\' + @dbName " + "\n"
		                +"        SET @toCollation = '"+CollationName+"' -- Database default collate" + "\n"
		                +"        --- SCRIPT TO GENERATE THE CREATION SCRIPT OF ALL FOREIGN KEY CONSTRAINTS" + "\n"
		                +"        declare @ForeignKeyID int" + "\n"
		                +"        declare @ForeignKeyName varchar(4000)" + "\n"
		                +"        declare @ParentTableName varchar(4000)" + "\n"
		                +"        declare @ParentColumn varchar(4000)" + "\n"
		                +"        declare @ReferencedTable varchar(4000)" + "\n"
		                +"        declare @ReferencedColumn varchar(4000)" + "\n"
		                +"        declare @StrParentColumn varchar(max)" + "\n"
		                +"        declare @StrReferencedColumn varchar(max)" + "\n"
		                +"        declare @ParentTableSchema varchar(4000)" + "\n"
		                +"        declare @ReferencedTableSchema varchar(4000)" + "\n"
		                +"        declare @TSQLCreationFK varchar(max)" + "\n"
		                +"        --Written by Percy Reyes www.percyreyes.com" + "\n"
		                +"        declare CursorFK cursor for select object_id--, name, object_name( parent_object_id) " + "\n"
		                +"        from sys.foreign_keys" + "\n"
		                +"        open CursorFK" + "\n"
		                +"        fetch next from CursorFK into @ForeignKeyID" + "\n"
		                +"        while (@@FETCH_STATUS=0)" + "\n"
		                +"        begin" + "\n"
		                +"         set @StrParentColumn=''" + "\n"
		                +"         set @StrReferencedColumn=''" + "\n"
		                +"         declare CursorFKDetails cursor for" + "\n"
		                +"          select  fk.name ForeignKeyName, schema_name(t1.schema_id) ParentTableSchema," + "\n"
		                +"          object_name(fkc.parent_object_id) ParentTable, c1.name ParentColumn,schema_name(t2.schema_id) ReferencedTableSchema," + "\n"
		                +"           object_name(fkc.referenced_object_id) ReferencedTable,c2.name ReferencedColumn" + "\n"
		                +"          from --sys.tables t inner join " + "\n"
		                +"          sys.foreign_keys fk " + "\n"
		                +"          inner join sys.foreign_key_columns fkc on fk.object_id=fkc.constraint_object_id" + "\n"
		                +"          inner join sys.columns c1 on c1.object_id=fkc.parent_object_id and c1.column_id=fkc.parent_column_id " + "\n"
		                +"          inner join sys.columns c2 on c2.object_id=fkc.referenced_object_id and c2.column_id=fkc.referenced_column_id " + "\n"
		                +"          inner join sys.tables t1 on t1.object_id=fkc.parent_object_id " + "\n"
		                +"          inner join sys.tables t2 on t2.object_id=fkc.referenced_object_id " + "\n"
		                +"          where fk.object_id=@ForeignKeyID" + "\n"
		                +"         open CursorFKDetails" + "\n"
		                +"         fetch next from CursorFKDetails into  @ForeignKeyName, @ParentTableSchema, @ParentTableName, @ParentColumn, @ReferencedTableSchema, @ReferencedTable, @ReferencedColumn" + "\n"
		                +"         while (@@FETCH_STATUS=0)" + "\n"
		                +"         begin    " + "\n"
		                +"          set @StrParentColumn=@StrParentColumn + ', ' + quotename(@ParentColumn)" + "\n"
		                +"          set @StrReferencedColumn=@StrReferencedColumn + ', ' + quotename(@ReferencedColumn)" + "\n"
			            +"             fetch next from CursorFKDetails into  @ForeignKeyName, @ParentTableSchema, @ParentTableName, @ParentColumn, @ReferencedTableSchema, @ReferencedTable, @ReferencedColumn" + "\n"
		                +"         end" + "\n"
		                +"         close CursorFKDetails" + "\n"
		                +"         deallocate CursorFKDetails" + "\n"
		                +"         set @StrParentColumn=substring(@StrParentColumn,2,len(@StrParentColumn)-1)" + "\n"
		                +"         set @StrReferencedColumn=substring(@StrReferencedColumn,2,len(@StrReferencedColumn)-1)" + "\n"
		                +"         set @TSQLCreationFK='ALTER TABLE '+quotename(@ParentTableSchema)+'.'+quotename(@ParentTableName)+' WITH CHECK ADD CONSTRAINT '+quotename(@ForeignKeyName)" + "\n"
		                +"         + ' FOREIGN KEY('+ltrim(@StrParentColumn)+') '+ char(10) +'REFERENCES '+quotename(@ReferencedTableSchema)+'.'+quotename(@ReferencedTable)+' ('+ltrim(@StrReferencedColumn)+') ' + ';'  + char(10) + 'GO' + char(10)" + "\n"
 		                +"        --print @TSQLCreationFK" + "\n"
		                +"        -- Print the foreign key statement" + "\n"
		                +"        exec dbo.usp_OLEWriteFile  @FileName = @destinationfilename, @TextData= @TSQLCreationFK , @FileAction = 'APPEND' " + "\n"
		                +"        fetch next from CursorFK into @ForeignKeyID " + "\n"
		                +"        end" + "\n"
		                +"        close CursorFK" + "\n"
		                +"        deallocate CursorFK" + "\n"
                        +"commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"--3.END" + "\n"
                        +"--4.START .Generate Indexes and Constraints" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
		                +"        Declare @dbName as varchar(max)" + "\n"
		                +"        Declare @myCommand as varchar(max)" + "\n"
		                +"        Declare @fnName as varchar(max)" + "\n"
		                +"        DECLARE @destinationpath NVARCHAR(1000)  " + "\n"
		                +"        DECLARE @destinationfilename NVARCHAR(1000)  " + "\n"
		                +"        declare @toCollation sysname " + "\n"
                        + "       set @dbName = '" + DBName + ".sql'" + "\n"
                        + "       SET @destinationpath = N'" + FileDir + "' " + "\n"
                        + "       SET @destinationfilename = @destinationpath + '\' + @dbName " + "\n"
                        + "       SET @toCollation = '" + CollationName + "' -- Database default collate" + "\n"
		                +"        declare @SchemaName varchar(100)declare @TableName varchar(256)" + "\n"
		                +"        declare @IndexName varchar(256)" + "\n"
		                +"        declare @ColumnName varchar(100)" + "\n"
		                +"        declare @is_unique varchar(100)" + "\n"
		                +"        declare @IndexTypeDesc varchar(100)" + "\n"
		                +"        declare @FileGroupName varchar(100)" + "\n"
		                +"        declare @is_disabled varchar(100)" + "\n"
		                +"        declare @IndexOptions varchar(max)" + "\n"
		                +"        declare @IndexColumnId int" + "\n"
		                +"        declare @IsDescendingKey int " + "\n"
		                +"        declare @IsIncludedColumn int" + "\n"
		                +"        declare @TSQLScripCreationIndex varchar(max)" + "\n"
		                +"        declare @TSQLScripDisableIndex varchar(max)" + "\n"
		                +"        declare CursorIndex cursor for" + "\n"
		                +"         select schema_name(t.schema_id) [schema_name], t.name, ix.name," + "\n"
		                +"         case when ix.is_unique = 1 then 'UNIQUE ' else '' END " + "\n"
		                +"         , ix.type_desc," + "\n"
		                +"         case when ix.is_padded=1 then 'PAD_INDEX = ON, ' else 'PAD_INDEX = OFF, ' end" + "\n"
		                +"         + case when ix.allow_page_locks=1 then 'ALLOW_PAGE_LOCKS = ON, ' else 'ALLOW_PAGE_LOCKS = OFF, ' end" + "\n"
		                +"         + case when ix.allow_row_locks=1 then  'ALLOW_ROW_LOCKS = ON, ' else 'ALLOW_ROW_LOCKS = OFF, ' end" + "\n"
		                +"         + case when INDEXPROPERTY(t.object_id, ix.name, 'IsStatistics') = 1 then 'STATISTICS_NORECOMPUTE = ON, ' else 'STATISTICS_NORECOMPUTE = OFF, ' end" + "\n"
		                +"         + case when ix.ignore_dup_key=1 then 'IGNORE_DUP_KEY = ON, ' else 'IGNORE_DUP_KEY = OFF, ' end" + "\n"
		                +"         + 'SORT_IN_TEMPDB = OFF, FILLFACTOR =' + CAST(case when ix.fill_factor =0  then 1 else 2 end AS VARCHAR(3)) AS IndexOptions" + "\n"
		                +"         , ix.is_disabled , FILEGROUP_NAME(ix.data_space_id) FileGroupName" + "\n"
		                +"         from sys.tables t " + "\n"
		                +"         inner join sys.indexes ix on t.object_id=ix.object_id" + "\n"
		                +"         where ix.type>0 and ix.is_primary_key=0 and ix.is_unique_constraint=0 --and schema_name(tb.schema_id)= @SchemaName and tb.name=@TableName" + "\n"
		                +"        and t.is_ms_shipped=0 and t.name<>'sysdiagrams'" + "\n"
		                +"         order by schema_name(t.schema_id), t.name, ix.name" + "\n"
		                +"        open CursorIndex" + "\n"
		                +"        fetch next from CursorIndex into  @SchemaName, @TableName, @IndexName, @is_unique, @IndexTypeDesc, @IndexOptions,@is_disabled, @FileGroupName" + "\n"
		                +"        while (@@fetch_status=0)" + "\n"
		                +"        begin" + "\n"
		                +"         declare @IndexColumns varchar(max)" + "\n"
		                +"         declare @IncludedColumns varchar(max)" + "\n"
 		                +"         set @IndexColumns=''" + "\n"
		                +"         set @IncludedColumns=''" + "\n"
 		                +"         declare CursorIndexColumn cursor for " + "\n"
		                +"          select col.name, ixc.is_descending_key, ixc.is_included_column" + "\n"
		                +"          from sys.tables tb " + "\n"
		                +"          inner join sys.indexes ix on tb.object_id=ix.object_id" + "\n"
		                +"          inner join sys.index_columns ixc on ix.object_id=ixc.object_id and ix.index_id= ixc.index_id" + "\n"
		                +"          inner join sys.columns col on ixc.object_id =col.object_id  and ixc.column_id=col.column_id" + "\n"
		                +"          where ix.type>0 and (ix.is_primary_key=0 or ix.is_unique_constraint=0)" + "\n"
		                +"          and schema_name(tb.schema_id)=@SchemaName and tb.name=@TableName and ix.name=@IndexName" + "\n"
		                +"          order by ixc.index_column_id" + "\n"
 		                +"         open CursorIndexColumn " + "\n"
		                +"         fetch next from CursorIndexColumn into  @ColumnName, @IsDescendingKey, @IsIncludedColumn" + "\n"
 		                +"         while (@@fetch_status=0)" + "\n"
		                +"         begin" + "\n"
		                +"          if @IsIncludedColumn=0 " + "\n"
		                +"           set @IndexColumns=@IndexColumns + QUOTENAME(@ColumnName)  + case when @IsDescendingKey=1  then ' DESC, ' else  ' ASC, ' end" + "\n"
		                +"          else " + "\n"
		                +"           set @IncludedColumns=@IncludedColumns  + QUOTENAME(@ColumnName)  +', ' " + "\n"
		                +"          fetch next from CursorIndexColumn into @ColumnName, @IsDescendingKey, @IsIncludedColumn" + "\n"
		                +"         end" + "\n"
		                +"         close CursorIndexColumn" + "\n"
		                +"         deallocate CursorIndexColumn" + "\n"
		                +"         set @IndexColumns = substring(@IndexColumns, 1, len(@IndexColumns)-1)" + "\n"
		                +"         set @IncludedColumns = case when len(@IncludedColumns) >0 then substring(@IncludedColumns, 1, len(@IncludedColumns)-1) else '' end" + "\n"
		                +"         --  print @IndexColumns" + "\n"
		                +"         --  print @IncludedColumns" + "\n"
                        +"         set @TSQLScripCreationIndex =''" + "\n"
		                +"         set @TSQLScripDisableIndex =''" + "\n"
		                +"         set @TSQLScripCreationIndex='CREATE '+ @is_unique  +@IndexTypeDesc + ' INDEX ' +QUOTENAME(@IndexName)+' ON ' + QUOTENAME(@SchemaName) +'.'+ QUOTENAME(@TableName)+ '('+@IndexColumns+') '+ " + "\n"
		                +"          case when len(@IncludedColumns)>0 then CHAR(13) +'INCLUDE (' + @IncludedColumns+ ')' else '' end + CHAR(13)+'WITH (' + @IndexOptions+ ') ON ' + QUOTENAME(@FileGroupName) + ';' + char(10) + 'GO' + char(10)" + "\n"
		                +"         if @is_disabled=1 " + "\n"
		                +"          set  @TSQLScripDisableIndex=  CHAR(13) +'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON ' + QUOTENAME(@SchemaName) + '.' + QUOTENAME(@TableName) + ' DISABLE;' + char(10) + 'GO' + char(10)" + "\n"
		                +"         --print @TSQLScripCreationIndex" + "\n"
		                +"         --print @TSQLScripDisableIndex" + "\n"
		                +"         set @TSQLScripCreationIndex  +=  CHAR(13)  + ' ' + isnull(@TSQLScripDisableIndex,' ')" + "\n"
		                +"         exec dbo.usp_OLEWriteFile  @FileName = @destinationfilename, @TextData= @TSQLScripCreationIndex , @FileAction = 'APPEND' " + "\n"
		                +"         fetch next from CursorIndex into  @SchemaName, @TableName, @IndexName, @is_unique, @IndexTypeDesc, @IndexOptions,@is_disabled, @FileGroupName" + "\n"
		                +"        end" + "\n"
		                +"        close CursorIndex" + "\n"
		                +"        deallocate CursorIndex" + "\n"
                        +"Commit TRANSACTION;	" + "\n"
                        +"------4.END" + "\n"
                        +"GO" + "\n"
                        +"--5. Start Export Stored Procedures Functions" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
		                +"        Declare @dbName as varchar(max)" + "\n"
		                +"        Declare @myCommandFunctions as varchar(max)" + "\n"
		                +"        Declare @fnName as varchar(max)" + "\n"
		                +"        DECLARE @destinationpath NVARCHAR(1000)  " + "\n"
		                +"        DECLARE @destinationfilename NVARCHAR(1000)  " + "\n"
		                +"        declare @toCollation sysname " + "\n"
		                +"        Declare @myCommandConstrain as varchar(max)" + "\n"
		                +"        set @dbName = '"+DBName +".sql'" + "\n"
		                +"        SET @destinationpath = N'"+FileDir+"' " + "\n"  
		                +"        SET @destinationfilename = @destinationpath + '\' + @dbName " + "\n"
		                +"        SET @toCollation = '"+CollationName+"' -- Database default collate" + "\n"
		                +"        DECLARE myFunctions CURSOR FOR " + "\n"
		                +"        SELECT OBJECT_NAME(sm.object_id) AS object_name,sm.definition " + "\n"
		                +"        -- using the two system tables sys.sql_modules and sys.objects" + "\n"
		                +"        FROM sys.sql_modules AS sm" + "\n"
		                +"        JOIN sys.objects AS o ON sm.object_id = o.object_id" + "\n"
		                +"        LEFT JOIN sys.objects as obj on sm.object_id = obj.object_id and  obj.type in   ('FN', 'IF', 'TF','P','AF','FT') " + "\n"
		                +"        ORDER BY o.type;" + "\n"
			            +"            OPEN myFunctions" + "\n"
			            +"            FETCH NEXT FROM myFunctions INTO @fnName,@myCommandFunctions" + "\n"
			            +"           IF @@FETCH_STATUS <> 0 " + "\n"
				        +"                 PRINT '         <<None>>'     " + "\n"
			            +"             WHILE @@FETCH_STATUS = 0" + "\n"
			            +"            BEGIN		" + "\n"
				        +"                set @myCommandFunctions += char(10) + 'GO' + char(10)" + "\n"
				        +"                exec dbo.usp_OLEWriteFile  @FileName = @destinationfilename, @TextData= @myCommandFunctions , @FileAction = 'APPEND'" + "\n"
				        +"                FETCH NEXT FROM myFunctions INTO @fnName,@myCommandFunctions" + "\n"
			            +"            END" + "\n"
			            +"            CLOSE myFunctions" + "\n"
			            +"            DEALLOCATE myFunctions" + "\n"
                        +"Commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"--6. End" + "\n"
                        +"--7. Create script for Constrains" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
		                +"        Declare @dbName as nvarchar(max)" + "\n"
		                +"        DECLARE @destinationpath NVARCHAR(max)  " + "\n"
		                +"        DECLARE @destinationfilename NVARCHAR(max)  " + "\n"
		                +"        SET @dbName = '"+DBName +".sql'" + "\n"
		                +"        SET @destinationpath = N'"+FileDir+"' " + "\n"  
		                +"        SET @destinationfilename = @destinationpath + '\' + @dbName " + "\n"
		                +"        --- SCRIPT TO GENERATE THE CONSTRAINTS" + "\n"
		                +"        declare @ConstrainName varchar(4000)" + "\n"
		                +"        declare @ParentTableName varchar(4000)" + "\n"
		                +"        declare @ReferencedTable varchar(4000)" + "\n"
		                +"        declare @ParentTableSchema varchar(4000)" + "\n"
		                +"        declare @ConstraintType varchar(max)" + "\n"
		                +"        declare @ConstraintTypeDesc varchar(max)" + "\n"
		                +"        declare @ConstraintDefinition varchar(max)" + "\n"
		                +"        declare @TSQLCreationCK varchar(max)" + "\n"
		                +"        declare CursorCK cursor for select  ck.name ConstraintName, schema_name(t1.schema_id) ParentTableSchema," + "\n"
		                +"        object_name(ck.parent_object_id) ParentTable,ck.[type] [ConstraintType], ck.type_desc [ConstraintTypeDesc]" + "\n"
		                +"        ,ck.[definition] [CKConstraintDefinition]" + "\n"
		                +"        from  sys.check_constraints ck " + "\n"
		                +"        inner join sys.tables t1 on t1.object_id=ck.parent_object_id " + "\n"
	                    +"        open CursorCK" + "\n"
		                +"        fetch next from CursorCK into @ConstrainName,@ParentTableSchema,@ParentTableName,@ConstraintType,@ConstraintTypeDesc,@ConstraintDefinition" + "\n"
		                +"        while (@@FETCH_STATUS=0)" + "\n"
		                +"        begin		 " + "\n"
		                +"        set @TSQLCreationCK = 'ALTER TABLE '+ QUOTENAME(@ParentTableSchema) +'.' + QUOTENAME(@ParentTableName) + ' WITH '+ case when @ConstraintType = 'C' then ' CHECK' end +' ADD CONSTRAINT '+  QUOTENAME(@ConstrainName) + ' CHECK ' + @ConstraintDefinition  + ';' +char(10)" + "\n"
		                +"        set @TSQLCreationCK += 'ALTER TABLE '+ QUOTENAME(@ParentTableSchema) +'.' + QUOTENAME(@ParentTableName) + case when @ConstraintType = 'C' then ' CHECK' end +' CONSTRAINT ' + QUOTENAME(@ConstrainName) + '; ' +char(10) + 'GO' + char(10)" + "\n"
		                +"       exec dbo.usp_OLEWriteFile  @FileName = @destinationfilename, @TextData= @TSQLCreationCK , @FileAction = 'APPEND' " + "\n"
		                +"        fetch next from CursorCK into @ConstrainName,@ParentTableSchema,@ParentTableName,@ConstraintType,@ConstraintTypeDesc,@ConstraintDefinition" + "\n"
		                +"        end" + "\n"
		                +"        close CursorCK" + "\n"
		                +"        deallocate CursorCK" + "\n"
                        +"Commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"--7. End" + "\n"
                        +"--8. Drop all objects that are problem " + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
	                    +"    Declare @dbName as nvarchar(max)" + "\n"
	                    +"        SET @dbName = '"+DBName +"'" + "\n"
	                    +"    -- Drop Indexes" + "\n"
	                    +"    DECLARE @SchemaName VARCHAR(4000)DECLARE @TableName VARCHAR(4000)" + "\n"
	                    +"    DECLARE @IndexName VARCHAR(4000)" + "\n"
	                    +"    DECLARE @TSQLDropIndex VARCHAR(MAX)" + "\n"
	                    +"    DECLARE CursorIndexes CURSOR FOR" + "\n"
	                    +"     SELECT schema_name(t.schema_id), t.name,  i.name " + "\n"
	                    +"     FROM sys.indexes i" + "\n"
	                    +"     INNER JOIN sys.tables t ON t.object_id= i.object_id" + "\n"
	                    +"     WHERE i.type>0 and t.is_ms_shipped=0 and t.name<>'sysdiagrams'" + "\n"
	                    +"     and (is_primary_key=0 and is_unique_constraint=0)" + "\n"
	                    +"    OPEN CursorIndexes" + "\n"
	                    +"    FETCH NEXT FROM CursorIndexes INTO @SchemaName,@TableName,@IndexName" + "\n"
	                    +"    WHILE @@fetch_status = 0" + "\n"
	                    +"    BEGIN" + "\n"
	                    +"     SET @TSQLDropIndex = 'DROP INDEX '+QUOTENAME(@SchemaName)+ '.' + QUOTENAME(@TableName) + '.' +QUOTENAME(@IndexName)+ '; ' " + "\n"
	                    +"     execute ('Use '+@dbName +'; ' + @TSQLDropIndex)" + "\n"
	                    +"     FETCH NEXT FROM CursorIndexes INTO @SchemaName,@TableName,@IndexName" + "\n"
	                    +"    END" + "\n"
	                    +"    CLOSE CursorIndexes" + "\n"
	                    +"    DEALLOCATE CursorIndexes" + "\n"
                        +"commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
	                    +"    --Drop FK " + "\n"
	                    +"    --- SCRIPT TO GENERATE THE DROP SCRIPT OF ALL FOREIGN KEY CONSTRAINTS" + "\n"
	                    +"    Declare @dbName as nvarchar(max)" + "\n"
	                    +"        SET @dbName = '"+DBName +"'" + "\n"
	                    +"    declare @ForeignKeyName varchar(4000)" + "\n"
	                    +"    declare @ParentTableName varchar(4000)" + "\n"
	                    +"    declare @ParentTableSchema varchar(4000)" + "\n"
	                    +"    declare @TSQLDropFK varchar(max)" + "\n"
	                    +"    declare CursorFK cursor for select fk.name ForeignKeyName, schema_name(t.schema_id) ParentTableSchema, t.name ParentTableName" + "\n"
	                    +"    from sys.foreign_keys fk  inner join sys.tables t on fk.parent_object_id=t.object_id" + "\n"
	                    +"    open CursorFK" + "\n"
	                    +"    fetch next from CursorFK into  @ForeignKeyName, @ParentTableSchema, @ParentTableName" + "\n"
	                    +"    while (@@FETCH_STATUS=0)" + "\n"
	                    +"    begin" + "\n"
	                    +"     set @TSQLDropFK ='ALTER TABLE '+quotename(@ParentTableSchema)+'.'+quotename(@ParentTableName)+' DROP CONSTRAINT '+quotename(@ForeignKeyName)+ '; ' " + "\n"
                        +"     execute ('Use '+@dbName +'; ' + @TSQLDropFK)" + "\n"
	                    +"   fetch next from CursorFK into  @ForeignKeyName, @ParentTableSchema, @ParentTableName" + "\n"
	                    +"    end" + "\n"
	                    +"    close CursorFK" + "\n"
	                    +"    deallocate CursorFK" + "\n"
                        +"commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
	                    +"    --Drop PK" + "\n"
	                    +"    Declare @dbName as nvarchar(max)" + "\n"
	                    +"        SET @dbName = '"+DBName +"'" + "\n"
	                    +"    --- SCRIPT TO GENERATE THE DROP SCRIPT OF ALL PK AND UNIQUE CONSTRAINTS." + "\n"
	                    +"    DECLARE @SchemaName VARCHAR(4000)" + "\n"
	                    +"    DECLARE @TableName VARCHAR(4000)" + "\n"
	                    +"    DECLARE @IndexName VARCHAR(4000)" + "\n"
	                    +"    DECLARE @TSQLDropIndex VARCHAR(MAX)" + "\n"
	                    +"    DECLARE CursorIndexes CURSOR FOR" + "\n"
	                    +"    SELECT  schema_name(t.schema_id), t.name,  i.name " + "\n"
	                    +"    FROM sys.indexes i" + "\n"
	                    +"    INNER JOIN sys.tables t ON t.object_id= i.object_id" + "\n"
	                    +"    WHERE i.type>0 and t.is_ms_shipped=0 and t.name<>'sysdiagrams'" + "\n"
	                    +"    and (is_primary_key=1 or is_unique_constraint=1)" + "\n"
	                    +"    OPEN CursorIndexes" + "\n"
	                    +"    FETCH NEXT FROM CursorIndexes INTO @SchemaName,@TableName,@IndexName" + "\n"
	                    +"    WHILE @@fetch_status = 0" + "\n"
	                    +"    BEGIN" + "\n"
	                    +"      SET @TSQLDropIndex = 'ALTER TABLE '+QUOTENAME(@SchemaName)+ '.' + QUOTENAME(@TableName) + ' DROP CONSTRAINT ' + QUOTENAME(@IndexName) + ';'" + "\n"
	                    +"      execute ('Use '+@dbName +'; ' + @TSQLDropIndex)" + "\n"
	                    +"      FETCH NEXT FROM CursorIndexes INTO @SchemaName,@TableName,@IndexName" + "\n"
	                    +"    END" + "\n"
	                    +"    CLOSE CursorIndexes" + "\n"
	                    +"    DEALLOCATE CursorIndexes" + "\n"
                        +"commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"--8. RemoveFuncions SPs" + "\n"
                        +"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" + "\n"
                        +"GO" + "\n"
                        +"Begin transaction" + "\n"
                        +"declare @n char(1)" + "\n"
                        +"set @n = char(10)" + "\n"
                        +"declare @stmt nvarchar(max)" + "\n"
                        +"-- procedures" + "\n"
                        +"select @stmt = isnull( @stmt + @n, '' ) +" + "\n"
                        +"    'drop procedure [' + schema_name(schema_id) + '].[' + name + ']'" + "\n"
                        +"from sys.procedures" + "\n"
                        +"-- functions" + "\n"
                        +"select @stmt = isnull( @stmt + @n, '' ) +" + "\n"
                        +"    'drop function [' + schema_name(schema_id) + '].[' + name + ']'" + "\n"
                        +"from sys.objects" + "\n"
                        +"where type in  ('FN','IF', 'TF','AF','FT') " + "\n"
                        +"-- Constrains " + "\n"
                        +"select @stmt = isnull( @stmt + @n, '' ) + 'ALTER TABLE '+ QUOTENAME(schema_name(t1.schema_id)) + '.'+QUOTENAME(object_name(ck.parent_object_id)) +' DROP CONSTRAINT [' +  ck.name + '];'  " + "\n"
		                +"        from  sys.check_constraints ck " + "\n"
		                +"        inner join sys.tables t1 on t1.object_id=ck.parent_object_id " + "\n"
                        +"exec sp_executesql @stmt" + "\n"
                        +"commit TRANSACTION;" + "\n"
                        +"GO" + "\n"
                        +"--8. ChangeCollation" + "\n"
                        +"ALTER DATABASE ["+DBName+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE" + "\n"
                        +"ALTER DATABASE ["+DBName+"] COLLATE "+ CollationName +  "\n"
                        +"ALTER DATABASE ["+DBName+"] SET MULTI_USER" + "\n"
                        +"GO";
                return myRealQuery;
        }
    }
}
