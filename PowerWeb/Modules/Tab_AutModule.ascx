<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_AutModule.ascx.cs"
    Inherits="PowerWeb.Modules.TabAutModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<dx:ASPxGridView ID="gvTabAut" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvTabAut_DataBinding"
    OnInitNewRow="gvTabAut_InitNewRow"
    onRowValidating="gvTabAut_RowValidating"
    OnRowInserting="gvTabAut_RowInserting"    
    OnRowUpdating="gvTabAut_RowUpdating" 
    OnRowDeleting="gvTabAut_RowDeleting">    
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
        <dx:GridViewDataComboBoxColumn FieldName="Aut_Id" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Utenti_Id" VisibleIndex="10" Width="15%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Tab_Funz_Id" VisibleIndex="25" Width="25%">
        </dx:GridViewDataComboBoxColumn> 
        <dx:GridViewDataComboBoxColumn FieldName="Nome_Tab_Funz" Visible="false">
        </dx:GridViewDataComboBoxColumn>        
         <dx:GridViewDataComboBoxColumn FieldName="Codice_Utente_Aut" Visible="false">
        </dx:GridViewDataComboBoxColumn>                   
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Aut" ReadOnly ="true" Visible="false">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Aut" ReadOnly="true" Visible="false" >
             <PropertiesDateEdit EditFormat="DateTime" />      
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Del_Aut" VisibleIndex="80" Width="5%">
            <PropertiesSpinEdit DisplayFormatString="g" MaxValue="12"/>
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Funz_Aut" VisibleIndex="50" Width="5%">
            <PropertiesSpinEdit DisplayFormatString="g" MaxValue="12"/>
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Ins_Aut" VisibleIndex="70" Width="5%">
            <PropertiesSpinEdit DisplayFormatString="g" MaxValue="12"/>
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Mod_Aut" VisibleIndex="60" Width="5%">
            <PropertiesSpinEdit DisplayFormatString="g" MaxValue="12"/>
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="MsgDel1_Aut" VisibleIndex="140" Width="6%">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="MsgDel2_Aut" VisibleIndex="150" Width="6%">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="MsgIns1_Aut" VisibleIndex="120" Width="6%">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="MsgIns2_Aut" VisibleIndex="130" Width="6%">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="MsgMod1_Aut" VisibleIndex="100" Width="6%">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="MsgMod2_Aut" VisibleIndex="110" Width="6%">
        </dx:GridViewDataSpinEditColumn>              
    </Columns>    
</dx:ASPxGridView>
