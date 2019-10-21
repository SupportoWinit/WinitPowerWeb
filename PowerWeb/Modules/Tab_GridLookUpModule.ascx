<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_GridLookUpModule.ascx.cs"
    Inherits="PowerWeb.Modules.TabGridLookUpModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<table>
    <tr>
<dx:ASPxGridView id="gvTabGridLookUp" runat="server" autogeneratecolumns="False" width="100%" OnDataBinding="gvTabGridLookUp_DataBinding"
    OnInitNewRow="gvTabGridLookUp_InitNewRow"
    OnRowValidating="gvTabGridLookUp_RowValidating"
    OnRowInserting="gvTabGridLookUp_RowInserting"
    OnRowUpdating="gvTabGridLookUp_RowUpdating"
    OnRowDeleting="gvTabGridLookUp_RowDeleting">
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
            <CancelButton Visible="true">
                <Image Url="../Icons/Undo/Undo.png" />
            </CancelButton>
            <UpdateButton Visible="true">
                <Image Url="../Icons/Check/Check.png" />
            </UpdateButton>
            <ClearFilterButton Visible="True">
                <Image Url="../Icons/Undo/Undo.png" />
            </ClearFilterButton>
        </dx:GridViewCommandColumn>
        <dx:GridViewDataTextColumn FieldName="Tab_GridLookup_Id" Visible="false" ShowInCustomizationForm="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Campo_Db" VisibleIndex="70" Width="4%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="Campo_Filtro" VisibleIndex="105" Width="4%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Campo_Localizzato" VisibleIndex="100" Width="4%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Campo_NotOnlyInList" VisibleIndex="105" Width="4%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Campo_Selezionato" VisibleIndex="90" Width="4%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataCheckColumn FieldName="Campo_Video" VisibleIndex="80" Width="4%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="NomeCampo" VisibleIndex="50" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="NomeRicerca" VisibleIndex="10" Width="15%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Nome_Risorsa" VisibleIndex="60" Width="15%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="NomeTab" VisibleIndex="20" Width="7%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="NomeTab_Decod" VisibleIndex="30" Width="15%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Ordinamento" VisibleIndex="25" Width="5%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Utenti_Id" VisibleIndex="110" Width="5%">
        </dx:GridViewDataTextColumn>
    </Columns>
</dx:ASPxGridView>
        </tr>
    </table>
