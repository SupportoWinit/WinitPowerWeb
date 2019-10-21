<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Fil_UtentiModule.ascx.cs"
    Inherits="PowerWeb.Modules.Fil_UtentiModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<table>
    <tr>
<dx:ASPxGridView ID="gvFil_Utenti" runat="server" AutoGenerateColumns="False" Width="100%"
    OnDetailRowExpandedChanged="gvFil_Utenti_DetailRowExpandedChanged">
    <Columns>        
        <dx:GridViewDataTextColumn FieldName="Codice_Fil" VisibleIndex="1" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Fil" VisibleIndex="80" ReadOnly="true" Width="10%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Fil" VisibleIndex="90" ReadOnly="true" Width="15%">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataTextColumn FieldName="Descrizione_Fil" VisibleIndex="20" Width="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Fil" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Fil" VisibleIndex="100" Width="30%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="N_Fil_Utenti" VisibleIndex="1" Width="5%" ReadOnly="True" CellStyle-HorizontalAlign="Center"> 
        </dx:GridViewDataTextColumn> 
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvFil_Utenti_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvFil_Utenti_Detail_Init"
                OnInitNewRow="gvFil_Utenti_Detail_InitNewRow"
                OnRowValidating="gvFil_Utenti_Detail_RowValidating"
                OnRowInserting="gvFil_Utenti_Detail_RowInserting"
                OnRowUpdating="gvFil_Utenti_Detail_RowUpdating"
                OnRowDeleting="gvFil_Utenti_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvFil_Utenti_Detail_BeforePerformDataSelect">
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
                    <dx:GridViewDataComboBoxColumn FieldName="Utenti_Id" VisibleIndex="20" Width="10%">
                    </dx:GridViewDataComboBoxColumn>                          
                    <dx:GridViewDataComboBoxColumn FieldName="Dominio_Utenti_Fil" VisibleIndex="40" Width="50%">
                    </dx:GridViewDataComboBoxColumn>   
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
        </tr>
    </table>
