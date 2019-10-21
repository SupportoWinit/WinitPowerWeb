<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Pru_ColModule.ascx.cs"
    Inherits="PowerWeb.Modules.Pru_ColModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<dx:ASPxGridView ID="gvPru_Col" runat="server" AutoGenerateColumns="False" Width="100%" OnDetailRowExpandedChanged="gvPru_Col_DetailRowExpandedChanged" OnDataBinding="gvPru_Col_DataBinding">
    <Columns>
        <dx:GridViewDataTextColumn FieldName="Codice_Pru" VisibleIndex="20" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Pru" Visible="false" ReadOnly="true">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Pru" Visible="false" ReadOnly="true">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Pru" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="N_Pru_Col" VisibleIndex="10" ReadOnly="true" CellStyle-HorizontalAlign="Center" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="N_Serie_Pru" VisibleIndex="30" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Pru" VisibleIndex="100" Width="60%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Singola_Reg_Pru" VisibleIndex="70" Width="7%">
        </dx:GridViewDataCheckColumn>
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvPru_Col_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvPru_Col_Detail_Init"
                OnInitNewRow="gvPru_Col_Detail_InitNewRow"
                OnRowValidating="gvPru_Col_Detail_RowValidating"
                OnRowInserting="gvPru_Col_Detail_RowInserting"
                OnRowUpdating="gvPru_Col_Detail_RowUpdating"
                OnRowDeleting="gvPru_Col_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvPru_Col_Detail_BeforePerformDataSelect">
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
                            <Image Url="../Icons/Edit/Edit.png"/>
                        </EditButton>
                        <ClearFilterButton Visible="True">
                            <Image Url="../Icons/Undo/Undo.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="20" Width="10%">
                        <Settings AllowHeaderFilter="False"/>
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataDateColumn FieldName="Abilitazione_Data_Inizio_Pru_Col" VisibleIndex="10" Width="15%">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Pru_Col" Visible="false" ReadOnly="true">
                        <PropertiesDateEdit EditFormat="DateTime" />
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Pru_Col" Visible="false" ReadOnly="true">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Pru_Col" VisibleIndex="110" Width="5%">
                    </dx:GridViewDataCheckColumn>
                    <dx:GridViewDataTextColumn FieldName="Note_Pru_Col" VisibleIndex="100" Width="50%">
                    </dx:GridViewDataTextColumn>
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
