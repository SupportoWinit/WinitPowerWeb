<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Utenti_FilModule.ascx.cs"
    Inherits="PowerWeb.Modules.Utenti_FilModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<table>
    <tr>
<dx:ASPxGridView ID="gvUtenti_Fil" runat="server" AutoGenerateColumns="False" Width="100%" 
    OnDetailRowExpandedChanged="gvUtenti_Fil_DetailRowExpandedChanged">
    <Columns>        
        <dx:GridViewDataTextColumn FieldName="Utenti_Id" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Utente" VisibleIndex="10" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="SaltKey_Utente" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="PasswordHash_Utente" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Password" Visible="False">
            <PropertiesTextEdit Password="True" />
            <EditFormSettings Visible="True" />
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Utente" VisibleIndex="50" Width="10%" ReadOnly="True">           
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Utente" VisibleIndex="60" Width="15%" ReadOnly="True">
             <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="Disabilitazione_Utente" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>              
        <dx:GridViewDataComboBoxColumn FieldName="Lingue_Id" Visible="false">
        </dx:GridViewDataComboBoxColumn>        
        <dx:GridViewDataTextColumn FieldName="Liv_Utente" VisibleIndex="20" CellStyle-HorizontalAlign="Center" Width="5%">
            <PropertiesTextEdit MaxLength="2" NullDisplayText="Da 0 a 10">
                <MaskSettings Mask="<0..99>" />
            </PropertiesTextEdit>
            <EditCellStyle HorizontalAlign="Center"></EditCellStyle>
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Menu_Tipo_Id" Visible="false">
        </dx:GridViewDataComboBoxColumn>        
        <dx:GridViewDataTextColumn FieldName="Note_Utente" VisibleIndex="120" Width="55%">
        </dx:GridViewDataTextColumn> 
        <dx:GridViewDataTextColumn FieldName="N_Utenti_Fil" VisibleIndex="1" Width="5%" ReadOnly="True" CellStyle-HorizontalAlign="Center"> 
        </dx:GridViewDataTextColumn>                 
    </Columns>
    <Templates>
        <DetailRow>
            <dx:ASPxGridView ID="gvUtenti_Fil_Detail" runat="server" AutoGenerateColumns="False" Width="100%"                                 
                OnInit="gvUtenti_Fil_Detail_Init"
                OnInitNewRow="gvUtenti_Fil_Detail_InitNewRow"
                OnRowValidating="gvUtenti_Fil_Detail_RowValidating"
                OnRowInserting="gvUtenti_Fil_Detail_RowInserting" 
                OnRowUpdating="gvUtenti_Fil_Detail_RowUpdating"
                OnRowDeleting="gvUtenti_Fil_Detail_RowDeleting"
                OnBeforePerformDataSelect="gvUtenti_Fil_Detail_BeforePerformDataSelect">                                
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
                    <dx:GridViewDataComboBoxColumn FieldName="Fil_Id" VisibleIndex="20" Width="30%">
                    </dx:GridViewDataComboBoxColumn>                          
                    <dx:GridViewDataComboBoxColumn FieldName="Dominio_Utenti_Fil" VisibleIndex="40" Width="60%">
                    </dx:GridViewDataComboBoxColumn>                                                                                             
                </Columns>               
            </dx:ASPxGridView>
        </DetailRow>
    </Templates>
    <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded ="true" />    
</dx:ASPxGridView>
</tr>
    </table>

