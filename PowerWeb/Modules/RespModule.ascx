<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RespModule.ascx.cs" Inherits="PowerWeb.Modules.RespModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<table>
    <tr>
        <dx:ASPxGridView ID="gvResp" runat="server" AutoGenerateColumns="False" Width="100%"
            OnInitNewRow="gvResp_InitNewRow"
            OnRowValidating="gvResp_RowValidating"
            OnRowInserting="gvResp_RowInserting"
            OnRowUpdating="gvResp_RowUpdating"
            OnRowDeleting="gvResp_RowDeleting"
            OnDataBinding="gvResp_DataBinding">
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
                <dx:GridViewDataTextColumn FieldName="Resp_Id" Visible="false">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Resp" VisibleIndex="10" Width="10%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Resp" VisibleIndex="80" ReadOnly="true" Width="10%">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Resp" VisibleIndex="90" ReadOnly="true" Width="15%">
                    <PropertiesDateEdit EditFormat="DateTime" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataTextColumn FieldName="Descrizione_Resp" VisibleIndex="20" Width="20%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Resp" VisibleIndex="110" Width="5%">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataTextColumn FieldName="Note_Resp" VisibleIndex="100" Width="30%">
                </dx:GridViewDataTextColumn>
            </Columns>
        </dx:ASPxGridView>
    </tr>
</table>
