<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Resp_UtentiModule.ascx.cs"
    Inherits="PowerWeb.Modules.Resp_UtentiModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView ID="gvResp_Utenti" runat="server" AutoGenerateColumns="False" Width="100%"
    OnDetailRowExpandedChanged="gvResp_Utenti_DetailRowExpandedChanged">
    <Columns>              
        <dx:GridViewDataTextColumn FieldName="Codice_Resp" VisibleIndex="10" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Resp" VisibleIndex="80" ReadOnly ="true" Width="10%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Resp" VisibleIndex="90" ReadOnly ="true"  Width="15%">
             <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>                
        <dx:GridViewDataTextColumn FieldName="Descrizione_Resp" VisibleIndex="20" Width="20%" >
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Resp" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>      
        <dx:GridViewDataTextColumn FieldName="Note_Resp" VisibleIndex="100" Width="30%">
        </dx:GridViewDataTextColumn>  
        <dx:GridViewDataTextColumn FieldName="N_Resp_Utenti" VisibleIndex="1" Width="5%" ReadOnly="True" CellStyle-HorizontalAlign="Center"> 
        </dx:GridViewDataTextColumn> 
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvResp_Utenti_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvResp_Utenti_Detail_Init"
                OnInitNewRow="gvResp_Utenti_Detail_InitNewRow"
                OnRowValidating="gvResp_Utenti_Detail_RowValidating"
                OnRowInserting="gvResp_Utenti_Detail_RowInserting"
                OnRowUpdating="gvResp_Utenti_Detail_RowUpdating"
                OnRowDeleting="gvResp_Utenti_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvResp_Utenti_Detail_BeforePerformDataSelect">
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
                    <dx:GridViewDataComboBoxColumn FieldName="Utenti_Id" VisibleIndex="20" Width="30%">
                    </dx:GridViewDataComboBoxColumn>                     
                  <dx:GridViewDataComboBoxColumn FieldName="Dominio_Utenti_Resp" VisibleIndex="40" Width="60%">
                    </dx:GridViewDataComboBoxColumn>   
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
        </tr>
    </table>
