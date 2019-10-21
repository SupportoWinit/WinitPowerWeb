<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Fru_CantModule.ascx.cs"
    Inherits="PowerWeb.Modules.Fru_CantModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<dx:ASPxGridView ID="gvFru_Cant" runat="server" AutoGenerateColumns="False" Width="100%" OnDetailRowExpandedChanged="gvFru_Cant_DetailRowExpandedChanged" OnDataBinding="gvFru_Cant_DataBinding">
    <Columns>
        <dx:GridViewDataTextColumn FieldName="Codice_Fru" VisibleIndex="20" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Fru" Visible="False" ReadOnly="true">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Fru" Visible="False" ReadOnly="true">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Fru" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="N_Serie_Fru" VisibleIndex="30" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Fru" VisibleIndex="100" Width="60%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="N_Fru_Cant" VisibleIndex="10" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Singola_Reg_Fru" VisibleIndex="70" Width="7%">
        </dx:GridViewDataCheckColumn>
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvFru_Cant_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                OnInit="gvFru_Cant_Detail_Init"
                OnInitNewRow="gvFru_Cant_Detail_InitNewRow"
                OnRowValidating="gvFru_Cant_Detail_RowValidating"
                OnRowInserting="gvFru_Cant_Detail_RowInserting"
                OnRowUpdating="gvFru_Cant_Detail_RowUpdating"
                OnRowDeleting="gvFru_Cant_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvFru_Cant_Detail_BeforePerformDataSelect">
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
                    <dx:GridViewDataTextColumn FieldName="Descrizione_Can" Visible="false">
                        <EditFormSettings Visible="False" />
                    </dx:GridViewDataTextColumn>
                    <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" VisibleIndex="20" Width="10%" Visible="true">
                        <Settings AllowHeaderFilter="False"/>
                        <EditFormSettings Visible="True" />
                    </dx:GridViewDataComboBoxColumn>
                    <dx:GridViewDataDateColumn FieldName="Abilitazione_Data_Inizio_Fru_Can" VisibleIndex="10" Width="15%">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Fru_Can" Visible="false" ReadOnly="True">
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Fru_Can" Visible="false" ReadOnly="true">
                        <PropertiesDateEdit EditFormat="DateTime" />
                    </dx:GridViewDataDateColumn>
                    <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Fru_Can" VisibleIndex="110" Width="5%">
                    </dx:GridViewDataCheckColumn>
                    <dx:GridViewDataTextColumn FieldName="Note_Fru_Can" VisibleIndex="100" Width="40%">
                    </dx:GridViewDataTextColumn>
                </Columns>
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
</dx:ASPxGridView>
