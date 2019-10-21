using System;
using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using Domain;
using DevExpress.Web.ASPxGridView;
using Common;
using DevExpress.Web.Data;
using log4net;
using Business;
using Reports;
using System.Reflection;
using System.Drawing;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Parameters;
using System.IO;
using DevExpress.XtraPrinting;
using DevExpress.Web.ASPxEditors;
using DevExpress.Data.Filtering;
using System.Globalization;
using System.Web.UI;
using Business.Repository.Custom;
using System.Collections;
using Common.Properties;

namespace PowerWeb.Modules
{

    public partial class ImportErrorsModule : BaseGridModule, ILogModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ImportErrorsModule));

        class ErrorRow
        {
            public String Matr_Pru_Fru { get; set; }
            public String Error_Message { get; set; }
        }

        public override ASPxGridView GridView
        {
            get
            {
                return gvErrori_Import;
            }
        }

        public override ASPxGridView GridViewDetail
        {
            get { return null; }
        }

        public override PowerFormTemplate EditFormTemplate
        {
            get { return null; }
        }

        public override PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        protected void Page_Init(object sender, EventArgs e)
        {
            //CommonServiceWeb.FillGridLabels(typeof(Errori_Import), GridView);
            BindGrid();
        }

        List<ErrorRow> Load_Suspended_Regs()
        {
            Dictionary<string, ErrorRow> errDic = new Dictionary<string, ErrorRow>();

            string filesInputFolderName = Server.MapPath(Settings.Default.Files_Input_Path);
            List<string> filesSuspendedNames = BusinessService.CalcolaFilesRegSospese(filesInputFolderName);
            
            if (filesSuspendedNames.Count > 0)
            {
                foreach (string file in filesSuspendedNames)
                {
                    string[] suspendedRegs = File.ReadAllLines(file);

                    for (int i = 0; i < suspendedRegs.Length; i += 2)
                    {
                        ErrorRow errorRow = new ErrorRow();
                        errorRow.Error_Message = suspendedRegs[i].Substring(1);

                        if (!errDic.ContainsKey(errorRow.Error_Message))
                            errDic.Add(errorRow.Error_Message, errorRow);
                    }
                }
            }

            List<ErrorRow> errorRows = new List<ErrorRow>();

            foreach (KeyValuePair<string, ErrorRow> keyValuePairString in errDic)
                errorRows.Add(keyValuePairString.Value);

            return errorRows;
        }

        private void BindGrid()
        {
            gvErrori_Import.DataSource = Load_Suspended_Regs();

            if (!Page.IsPostBack && !Page.IsCallback)
                gvErrori_Import.DataBind();
        }

        public override Type EntityType
        {
            get
            {
                return null;
            }
        }

        public ILog Log
        {
            get
            {
                return _log;
            }
        }


        public override void HeaderFilterFillItems(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewHeaderFilterEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}