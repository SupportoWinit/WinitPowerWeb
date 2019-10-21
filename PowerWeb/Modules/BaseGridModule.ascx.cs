using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Protocols;
using System.Web.UI;
using System.Web.UI.WebControls;
using DevExpress.Web.ASPxGridLookup;
using Domain;
using Business;
using Business.Repository;
using System.Text;
using System.Data.Common;
using System.Data.Entity.Core.Objects;
using System.Reflection;
using System.Dynamic;
using Business.Repository.Custom;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxEditors;
using Common;
using DevExpress.Data.Filtering;
using DevExpress.Web.Data;

namespace PowerWeb.Modules
{

    public class TabPageExtended
    {
        private static int _counterId = 0;

        public int Id { get; private set; }
        public string Name { get; set; }
        public int Columns { get; set; }
        public string Caption { get; set; }

        public TabPageExtended()
        {
            Id = _counterId;
            _counterId++;
        }
    }

    [Flags]
    public enum TabPageItemFieldTypeEnum
    {
        None = 0,
        NotInGroup = 1,
        EmptyField = 2,
        ColumnSpan = 4,
    }

    public class TabPageItemExtended
    {
        public string Field { get; set; }
        public TabPageItemFieldTypeEnum Type { get; set; }
        public int ColumnSpan { get; set; }

        public TabPageItemExtended(string field)
            : this(field, TabPageItemFieldTypeEnum.None)
        {
        }

        public TabPageItemExtended(string field, TabPageItemFieldTypeEnum type, int columnSpan = 0)
        {
            Field = field;
            Type = type;
            if (columnSpan != 0)
                columnSpan = (columnSpan * 2) - 1;
            ColumnSpan = columnSpan;
        }
    }

    public enum DialogType
    {
        Success,
        Info,
        Warning,
        Error
    }

    public partial class BaseGridModule : UserControl, IGridModule
    {
        public virtual IList<string> ForceWritableCombo
        {
            get { return new List<string>(); }
        }

        public virtual void GridView_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            var masterPage = Page.Master as GridMasterPage;

            if (masterPage != null)
            {
                masterPage.GridView_CellEditorInitialize(sender, e);

                ASPxComboBox cmbx = e.Editor as ASPxComboBox;
                if (cmbx != null)
                {
                    if (ForceWritableCombo.Contains(e.Column.FieldName))
                        cmbx.ReadOnly = false;

                    if (!cmbx.ReadOnly)
                    {
                        EditButton btnEdit = new EditButton("X");
                        cmbx.Buttons.Add(btnEdit);

                        cmbx.ClientSideEvents.ButtonClick = "onCustomEditButtonComboBoxClick";
                        //CommonServiceWeb.FillComboboxes(cmbx, e.Column.FieldName);}
                    }
                }
            }

        }

        public virtual void DetailGridView_CellEditorInitialize(object sender, ASPxGridViewEditorEventArgs e)
        {
            var masterPage = Page.Master as GridMasterPage;
            if (masterPage != null)
                this.GridView_CellEditorInitialize(sender, e);

        }

        private string EscapeForbiddenChars(string message)
        {
            return message.Replace("'", @"\'").Replace('"', '\"');
        }

        public virtual void BatchUpdate(object sender, ASPxDataBatchUpdateEventArgs e)
        {

        }

        public void GenericGridLookup_Init(object sender, EventArgs e)
        {
            ASPxGridLookup gridLookup = sender as ASPxGridLookup;
            //CommonServiceWeb.GridLookupInit(gridLookup);
        }

        public virtual Boolean IsToPopulateGrid
        {
            get
            {
                Boolean res = false;
                if (GridView != null)
                    res = PowerWebContext.GetFromSession<Boolean>("IsToPopulate" + GridView.ID);
                return res;
            }

            set
            {
                if (GridView != null)
                    PowerWebContext.SetToSession("IsToPopulate" + GridView.ID, value);
            }
        }

        public virtual void ResetSession()
        {
            IsToPopulateGrid = false;
        }

        public virtual ASPxGridView GridView
        {
            get { return null; }
        }

        public virtual ASPxGridView GridViewDetail
        {
            get { return null; }
        }

        /// <summary>
        /// Recupera la griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IPrintModule"/>
        /// </summary>
        /// <value>
        /// La griglia utilizzata per il recupero dei dati di stampa;
        /// se impostato a null si utilizza la griglia impostata nella proprietà GridView.
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IPrintModule"/>
        /// </value>
        public virtual ASPxGridView PrintGridView { get { return null; } }

        /// <summary>
        /// Recupera la griglia utilzizata per la raccolta dei dati da esportare.
        /// Se impostato a null si utilizza la griglia principale del modulo
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IExportXLSXModule"/>
        /// </summary>
        /// <value>
        /// La griglia utilizzata per la raccolta dei dati da esportare.
        /// Se impostato a null si utilizza la griglia principale del modulo.
        /// Proprietà utilizzata solamente in caso di implementazione dell'interfaccia <see cref="IExportXLSXModule"/>
        /// </value>
        public virtual ASPxGridView ExportGridView { get { return null; } }

        public virtual PowerFormTemplate EditFormTemplate
        {
            get { return null; }
        }

        public virtual PowerFormTemplate EditDetailFormTemplate
        {
            get { return null; }
        }

        public virtual Type EntityType
        {
            get { return null; }
        }

        public virtual void HeaderFilterFillItems(object sender, ASPxGridViewHeaderFilterEventArgs e)
        {
        }

        public virtual CriteriaOperator DefaultFilter
        {
            get { return null; }
        }

        public virtual Type DetailGridEntityType
        {
            get { return null; }
        }
    }
}