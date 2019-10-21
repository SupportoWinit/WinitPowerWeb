<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="PruModule.ascx.cs" Inherits="PowerWeb.Modules.PruModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxUploadControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>


<script type="text/javascript">
    function Uploader_OnUploadStart() {
        btnUpload.SetEnabled(false);
    }

    function Uploader_OnFilesUploadComplete(e) {
        UpdateUploadButton();
        if (e.errorText) {
            DisplayDialogError("Import matricole portatili", e.errorText);
        }
        else {
            DisplayDialogInfo("Import matricole portatili", "Import Matricole Terminato Regolarmente");
        }
    }

    function UpdateUploadButton() {
        if (uploader.GetText(0) != "")
            btnUpload.SetEnabled(true);
        else
            btnUpload.SetEnabled(false);
    }
</script>

<table>
    <tr>
<dx:ASPxGridView ID="gvPru" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvPru_DataBinding"
    OnInitNewRow="gvPru_InitNewRow"
    OnRowValidating="gvPru_RowValidating"
    OnRowInserting="gvPru_RowInserting"
    OnRowUpdating="gvPru_RowUpdating"
    OnRowDeleting="gvPru_RowDeleting"
    >
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
        <dx:GridViewDataTextColumn FieldName="Codice_Pru" VisibleIndex="10" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Pru" VisibleIndex="60" ReadOnly="true" Width="10%">
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Pru" VisibleIndex="70" ReadOnly="true" Width="15%">
            <PropertiesDateEdit EditFormat="DateTime" />
        </dx:GridViewDataDateColumn>
        <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Pru" VisibleIndex="110" Width="5%">
        </dx:GridViewDataCheckColumn>
        <dx:GridViewDataTextColumn FieldName="N_Serie_Pru" VisibleIndex="30" Width="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Note_Pru" VisibleIndex="100" Width="45%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataCheckColumn FieldName="Singola_Reg_Pru" VisibleIndex="40" Width="7%">
        </dx:GridViewDataCheckColumn>
    </Columns>
</dx:ASPxGridView>
    </tr>
        </table>
    

<dx:ASPxFormLayout ID="flImportTXT" runat="server" Width="80%">
    <Items>
        <dx:LayoutGroup Caption="Import Matricole Unità Portatili da File TXT" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption="File TXT da importare" HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <div style="float: left">
                                <table>
                                    <tr>
                                        <td>
                                            <dx:ASPxUploadControl ID="uploader" Width="600px" runat="server" ClientInstanceName="uploader"
                                                ShowProgressPanel="True"
                                                OnFileUploadComplete="upldImport_FileUploadComplete"
                                                FileUploadMode="OnPageLoad">
                                                <ClientSideEvents TextChanged="function(s, e) { UpdateUploadButton(); }" FileUploadStart="function(s, e) { Uploader_OnUploadStart(); }"
                                                    FileUploadComplete="function(s, e) { Uploader_OnFilesUploadComplete(e); }" />
                                                <ValidationSettings AllowedFileExtensions=".txt">
                                                </ValidationSettings>
                                            </dx:ASPxUploadControl>
                                        </td>

                                       <td style="width: 30%; padding-left: 5px;">
                                                        <dx:ASPxButton ID="btnUpload" Width="100%" runat="server" ClientInstanceName="btnUpload" HorizontalAlign="Center" VerticalAlign="Top"
                                                            ClientEnabled="False" UseSubmitBehavior="false"> 
                                                            <ClientSideEvents Click="function(s, e) { uploader.Upload(); }" />
                                                        </dx:ASPxButton>
                                                    </td>


                                    </tr>
                                </table>
                            </div>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>
