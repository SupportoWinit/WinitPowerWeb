<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CliModule.ascx.cs"
    Inherits="PowerWeb.Modules.CliModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>

<% if(DesignMode) {  %><script src="~/Scripts/ASPxScriptIntelliSense.js" type="text/javascript"></script><% }  %>
<script type="text/javascript">


    function OnResLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var resLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Provincia_Cli"));
        var resLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Cap_Cli"));
        resLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        resLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
    }
    function OnDomLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var domLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Provincia_Cli"));
        var domLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Cap_Cli"));
        domLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        domLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
    }
</script>
<table>
    <tr>
<dx:ASPxGridView ID="gvCli" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvCli_DataBinding"
    OnInitNewRow="gvCli_InitNewRow"
    OnRowValidating="gvCli_RowValidating"
    OnRowInserting="gvCli_RowInserting"
    OnRowUpdating="gvCli_RowUpdating"
    OnRowDeleting="gvCli_RowDeleting">
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
        <dx:GridViewDataTextColumn FieldName="Banca_Descrizione_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Banca_Iban_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Cliente" VisibleIndex="10" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Ditta_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Fiscale_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Cognome_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="CognomeNome_Cli" VisibleIndex="15" Width="20%" >
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_Cli" Visible="false">   
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_Cli" Visible="false">                    
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Cli" Visible="False" ReadOnly="true">            
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Cli" Visible="false" ReadOnly="true" >
           <PropertiesDateEdit EditFormat="DateTime"/>  
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Cli" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Cap_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Domicilio_Indirizzo_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Luogo_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Provincia_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Nazione_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_1_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_1_Rif_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_2_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Fax_2_Rif_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Lingua_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Nome_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_1Mese_Escl_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_2Mese_Escl_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Pagamento_Codice_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_GF_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_GG_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_Intervallo_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_Rate_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataSpinEditColumn FieldName="Pagamento_Sconto_Cli" Visible="false">
        </dx:GridViewDataSpinEditColumn>
        <dx:GridViewDataTextColumn FieldName="Partita_Iva_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Cap_Cli" VisibleIndex="40" Width="5%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Residenza_Indirizzo_Cli" VisibleIndex="60" Width="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Luogo_Cli" VisibleIndex="50" Width="20%">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Nazione_Cli" Visible="false">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Residenza_Provincia_Cli" VisibleIndex="30" Width="7%" CellStyle-HorizontalAlign="Center">
        </dx:GridViewDataComboBoxColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_1_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_1_Rif_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_2_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_2_Rif_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_3_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_3_Rif_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_4_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Telefono_4_Rif_Cli" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataComboBoxColumn FieldName="Valuta_Cli" VisibleIndex="100" Width="5%">
        </dx:GridViewDataComboBoxColumn>
    </Columns>
</dx:ASPxGridView>
        </tr>
</table>

