<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_FunzModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_FunzModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView id="gvTab_Funz" runat="server" autogeneratecolumns="False" width="100%" OnDataBinding="gvTab_Funz_DataBinding"
    OnInitNewRow="gvTab_Funz_InitNewRow"
    OnRowValidating="gvTab_Funz_RowValidating"
    OnRowInserting="gvTab_Funz_RowInserting"
    OnRowUpdating="gvTab_Funz_RowUpdating"
    OnRowDeleting="gvTab_Funz_RowDeleting">
    <Columns>
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
            <CustomButtons>
            <dx:GridViewCommandColumnCustomButton ID="add">
                    <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="addClone">
                    <Image ToolTip="AddClone" Url="../Icons/Add/Add.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="delete">
                    <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="view">
                    <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                </dx:GridViewCommandColumnCustomButton>
            </CustomButtons>
            <EditButton Visible="True">
                <Image Url="../Icons/Edit/Edit.png" />
            </EditButton>
            <ClearFilterButton Visible="True">
                <Image Url="../Icons/Undo/Undo.png" />
            </ClearFilterButton>
        </dx:GridViewCommandColumn>
        <dx:GridViewDataTextColumn FieldName="Tab_Funz_Id" Visible="False" ReadOnly="true" >
        </dx:GridViewDataTextColumn>                       
       <dx:GridViewDataComboBoxColumn FieldName="Dflt_Grid_Edit_Mode" VisibleIndex="40" Width="80%">
        </dx:GridViewDataComboBoxColumn>
         <dx:GridViewDataTextColumn FieldName="Link_Tab_Funz" VisibleIndex="30" Width="80%">
        </dx:GridViewDataTextColumn>
         <dx:GridViewDataTextColumn FieldName="Nome_Tab_Funz" VisibleIndex="20" Width="80%">
        </dx:GridViewDataTextColumn>
       <dx:GridViewDataComboBoxColumn FieldName="Dflt_OnOffBtnVisible" VisibleIndex="50" Width="80%">
        </dx:GridViewDataComboBoxColumn> 
    </Columns>
</dx:ASPxGridView>
        </tr>
    </table>
