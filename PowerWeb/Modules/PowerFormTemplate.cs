using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using DevExpress.Web.ASPxGridView;
using DevExpress.Web.ASPxTabControl;
using System.Web.UI.HtmlControls;
using DevExpress.Web.ASPxEditors;
using Common;
using System.Web.UI.WebControls;
using DevExpress.Web.ASPxGridLookup;
using Business;
using Domain;

namespace PowerWeb.Modules
{
    public class PowerFormTemplate : ITemplate
    {
        private bool _isShowSelection;

        private Dictionary<TabPageExtended, List<TabPageItemExtended>> _dic;

        public BaseGridModule BaseModule { get; set; }

        public Dictionary<TabPageExtended, List<TabPageItemExtended>> Dic
        {
            get
            {
                return _dic;
            }
            set
            {
                _dic = value;
            }
        }

        public bool IsDetail { get; set; }

        public bool IsShowSelection
        {
            get
            {
                return _isShowSelection;
            }
            set
            {
                _isShowSelection = value;
            }
        }

        public ASPxGridView Grid { get; private set; }

        public PowerFormTemplate(BaseGridModule module, Dictionary<TabPageExtended, List<TabPageItemExtended>> dic, bool isShowSelection = true)
        {
            _dic = dic;
            _isShowSelection = isShowSelection;
            BaseModule = module;
        }

        public void InstantiateIn(Control container)
        {
            HtmlGenericControl tabDiv = new HtmlGenericControl("div");

            tabDiv.Attributes["class"] = "power_dxgvEditFormTable_DevEx";
            tabDiv.Attributes["width"] = "100%";

            #region Controls div

            ASPxPageControl pc = new ASPxPageControl();
            pc.ID = "PowerFormTemplatePageControl";

            foreach (KeyValuePair<TabPageExtended, List<TabPageItemExtended>> keyValuePairString in _dic)
            {
                TabPage currentTabPage = new TabPage(keyValuePairString.Key.Caption, keyValuePairString.Key.Name);

                HtmlTable table = new HtmlTable();

                table.Width = "100%";

                int listCount = keyValuePairString.Value.Count;

                int rowCount = (listCount + (keyValuePairString.Key.Columns - 1)) / keyValuePairString.Key.Columns;


                int columnWidth = 100 / keyValuePairString.Key.Columns;

                int cellIndex = 0;

                for (int i = 0; i < rowCount; i++)
                {
                    HtmlTableRow tr = new HtmlTableRow();

                    for (int j = 0; j < keyValuePairString.Key.Columns * 2; j++)
                    {
                        HtmlTableCell td = new HtmlTableCell();

                        TabPageItemFieldTypeEnum fieldType = TabPageItemFieldTypeEnum.EmptyField;
                        string fieldName = String.Empty;

                        if (cellIndex < keyValuePairString.Value.Count)
                        {
                            fieldType = keyValuePairString.Value[cellIndex].Type;
                            fieldName = keyValuePairString.Value[cellIndex].Field;
                        }

                        if (j % 2 == 0)
                        {
                            ASPxLabel lbl = new ASPxLabel();
                            td.Attributes["class"] = "dxgvEditFormCaption_DevEx";

                            if (!String.IsNullOrEmpty(fieldName))
                            {
                                lbl.Text = BusinessService.GetLocalizedString(fieldName, ResourceTypeEnum.Field);
                                td.Controls.Add(lbl);
                            }

                        }
                        else
                        {
                            Control editFormControl = null;

                            td.Attributes["id"] = fieldName;
                            td.Attributes["class"] = "myHelp dxgvEditFormCell_DevEx";
                            td.Attributes["width"] = columnWidth + "%";

                            if ((fieldType & TabPageItemFieldTypeEnum.ColumnSpan) == TabPageItemFieldTypeEnum.ColumnSpan)
                                td.Attributes["colspan"] = keyValuePairString.Value[cellIndex].ColumnSpan.ToString();

                            if ((fieldType & TabPageItemFieldTypeEnum.EmptyField) != TabPageItemFieldTypeEnum.EmptyField)
                            {
                                var tempRepl = new ASPxGridViewTemplateReplacement();
                                tempRepl.Width = new Unit(columnWidth, UnitType.Percentage);
                                tempRepl.ReplacementType = GridViewTemplateReplacementType.EditFormCellEditor;
                                tempRepl.ColumnID = fieldName;
                                editFormControl = tempRepl;
                            }

                            if (editFormControl != null)
                            {
                                td.Controls.Add(editFormControl);
                            }
                            cellIndex++;
                        }
                        tr.Cells.Add(td);
                    }
                    table.Rows.Add(tr);
                }

                currentTabPage.Controls.Add(table);
                pc.TabPages.Add(currentTabPage);
            }

            #endregion

            tabDiv.Controls.Add(pc);

            container.Controls.Add(tabDiv);

            HtmlGenericControl buttonsDiv = new HtmlGenericControl("div");
            buttonsDiv.Attributes["class"] = "dxgvCommandColumn_DevEx power_dxgvCommandColumn_DevEx";

            #region Buttons div

            var alignment = "left";
            var padding = "10px";

            ASPxButton btnUpdate = new ASPxButton();
            btnUpdate.ID = "update";
            btnUpdate.ImageUrl = "../Icons/Check/Check.png";
            btnUpdate.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNAGGIORNA);
            btnUpdate.Style.Add(HtmlTextWriterStyle.Cursor, "pointer");
            btnUpdate.EnableTheming = false;
            btnUpdate.RenderMode = ButtonRenderMode.Link;
            btnUpdate.UseSubmitBehavior = false;
            btnUpdate.Style.Add("float", alignment);
            btnUpdate.Style.Add("padding", padding);

            GridViewEditFormTemplateContainer currentContainer = container as GridViewEditFormTemplateContainer;

            if (currentContainer != null && currentContainer.Grid != null)
            {
                if (currentContainer.Grid.IsNewRowEditing)
                {
                    btnUpdate.ClientSideEvents.Click = "OnCustomNewTemplateButtonClick";
                    if (IsDetail)
                        btnUpdate.ClientSideEvents.Click = "OnCustomNewTemplateButtonDetailClick";
                }
                else
                {
                    btnUpdate.ClientSideEvents.Click = "OnCustomEditTemplateButtonClick";
                    if (IsDetail)
                        btnUpdate.ClientSideEvents.Click = "OnCustomEditTemplateButtonDetailClick";
                }
                btnUpdate.AutoPostBack = false;

                if (!PowerWebContext.GetFromSession<bool>("IsReadOnly_" + currentContainer.Grid.ClientID))
                    buttonsDiv.Controls.Add(btnUpdate);

            }

            ASPxButton btnCancel = new ASPxButton();
            btnCancel.ID = "cancel";
            btnCancel.ImageUrl = "../Icons/Undo/Undo.png";
            btnCancel.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNANNULLA);
            btnCancel.Style.Add(HtmlTextWriterStyle.Cursor, "pointer");
            btnCancel.EnableTheming = false;
            btnCancel.RenderMode = ButtonRenderMode.Link;
            btnCancel.Style.Add("float", alignment);
            btnCancel.Style.Add("padding", padding);
            btnCancel.AutoPostBack = false;
            btnCancel.UseSubmitBehavior = false;
            btnCancel.ClientSideEvents.Click = "function (s,e) { OnCustomCancelTemplateButtonClick(s,e,\"" + currentContainer.Grid.ClientID + "\")}";
            if (IsDetail)
                btnCancel.ClientSideEvents.Click = "function (s,e) { OnCustomCancelTemplateButtonDetailClick(s,e,\"" + currentContainer.Grid.ClientID + "\")}"; ;
            buttonsDiv.Controls.Add(btnCancel);

            if (currentContainer != null && currentContainer.Grid != null && (currentContainer.Grid.ID.ToUpper() == "GVREGV" || currentContainer.Grid.ID.ToUpper() == "GVREGVM"))
            {
                ASPxButton btnRefresh = new ASPxButton();
                btnRefresh.ID = "refresh";
                btnRefresh.ImageUrl = "../Icons/Refresh/Refresh.png";
                btnRefresh.ToolTip = BusinessService.GetLocalizedString(PowerWebResources.CTRL_BTNREFRESH);
                btnRefresh.Style.Add(HtmlTextWriterStyle.Cursor, "pointer");
                btnRefresh.EnableTheming = false;
                btnRefresh.RenderMode = ButtonRenderMode.Link;
                btnRefresh.UseSubmitBehavior = false;
                btnRefresh.Style.Add("float", alignment);
                btnRefresh.Style.Add("padding", padding);

                if (currentContainer.Grid.IsNewRowEditing)
                {
                    btnRefresh.ClientSideEvents.Click = "OnCustomRefreshButtonClick";
                    if (IsDetail)
                        btnRefresh.ClientSideEvents.Click = "OnCustomRefreshButtonDetailClick";
                }
                else
                {
                    btnRefresh.ClientSideEvents.Click = "OnCustomRefreshButtonClick";
                    if (IsDetail)
                        btnRefresh.ClientSideEvents.Click = "OnCustomRefreshButtonDetailClick";
                }
                btnRefresh.AutoPostBack = false;

                if (!PowerWebContext.GetFromSession<bool>("IsReadOnly_" + currentContainer.Grid.ClientID))
                    buttonsDiv.Controls.Add(btnRefresh);
            }

            #endregion

            container.Controls.Add(buttonsDiv);

        }
    }
}