<%@ Page Title="PowerWeb - Centro di costo" MasterPageFile="~/GridMasterPage.master" Language="C#" AutoEventWireup="true" CodeBehind="CentroDiCostoPage.aspx.cs" Inherits="PowerWeb.Pages.CentroDiCostoPage" %>

<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxUploadControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <dx:ASPxGridView ID="gvCentroDiCosto" ClientInstanceName="grid" runat="server" AutoGenerateColumns="False" Width="100%"
        OnDetailRowExpandedChanged="gvCentroDiCosto_DetailRowExpandedChanged"
        OnDataBinding="gvCentroDiCosto_DataBinding"
        OnRowInserting="gvCentroDiCosto_RowInserting"
        OnRowUpdating="gvCentroDiCosto_RowUpdating"
        OnRowDeleting="gvCentroDiCosto_RowDeleting">
        <Columns>
            <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
                <NewButton Visible="True">
                    <Image Url="../Icons/Add/Add.png" />
                </NewButton>
                <EditButton Visible="True">
                    <Image Url="../Icons/Edit/Edit.png" />
                </EditButton>
                <DeleteButton Visible="True">
                    <Image Url="../Icons/Delete/Delete.png" />
                </DeleteButton>
                <CancelButton Visible="True">
                    <Image Url="../Icons/Undo/Undo.png" />
                </CancelButton>
                <UpdateButton Visible="True">
                    <Image Url="../Icons/Check/Check.png" />
                </UpdateButton>
            </dx:GridViewCommandColumn>
            <dx:GridViewDataTextColumn FieldName="Codice" VisibleIndex="20">
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataTextColumn FieldName="Descrizione" VisibleIndex="20">
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataDateColumn FieldName="Inizio" VisibleIndex="20">
            </dx:GridViewDataDateColumn>
            <dx:GridViewDataDateColumn FieldName="Fine" VisibleIndex="20">
            </dx:GridViewDataDateColumn>
        </Columns>
        <Templates>
            <DetailRow>
                <dx:ASPxGridView ID="gvCentroDiCosto_Detail" runat="server" AutoGenerateColumns="False" Width="100%"
                    OnInit="gvCentroDiCosto_Detail_Init"
                    OnCellEditorInitialize="gvCentroDiCosto_Detail_CellEditorInitialize"
                    OnRowInserting="gvCentroDiCosto_Detail_RowInserting"
                    OnRowUpdating="gvCentroDiCosto_Detail_RowUpdating"
                    OnRowDeleting="gvCentroDiCosto_Detail_RowDeleting"
                    OnBeforePerformDataSelect="gvCentroDiCosto_Detail_BeforePerformDataSelect">
                    <Columns>
                        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
                            <NewButton Visible="True">
                                <Image Url="../Icons/Add/Add.png" />
                            </NewButton>
                            <EditButton Visible="True">
                                <Image Url="../Icons/Edit/Edit.png" />
                            </EditButton>
                            <DeleteButton Visible="True">
                                <Image Url="../Icons/Delete/Delete.png" />
                            </DeleteButton>
                            <CancelButton Visible="true">
                                <Image Url="../Icons/Undo/Undo.png" />
                            </CancelButton>
                            <UpdateButton Visible="true">
                                <Image Url="../Icons/Check/Check.png" />
                            </UpdateButton>
                        </dx:GridViewCommandColumn>
                        <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="False">
                            <EditFormSettings Visible="True" />
                        </dx:GridViewDataComboBoxColumn>
                        <dx:GridViewDataTextColumn FieldName="Codice_Cantiere" Visible="true">
                            <EditFormSettings Visible="False" />
                        </dx:GridViewDataTextColumn>
                        <dx:GridViewDataTextColumn FieldName="Descrizione_Can" Visible="true">
                            <EditFormSettings Visible="False" />
                        </dx:GridViewDataTextColumn>
                    </Columns>
                    <Settings ShowFilterRow="true" />
                </dx:ASPxGridView>
            </DetailRow>
        </Templates>
        <Settings ShowFilterRow="true" />
        <SettingsDetail ShowDetailRow="true" AllowOnlyOneMasterRowExpanded="true" />
    </dx:ASPxGridView>

    <dx:ASPxFormLayout ID="importForm" runat="server" Width="80%" Visible="true" Style="margin-top: 0px">
        <Items>
            <dx:LayoutGroup Caption="Import Anagrafiche" SettingsItemHelpTexts-Position="Bottom">
                <Items>
                    <dx:LayoutItem Caption=" " HelpText="">
                        <LayoutItemNestedControlCollection>
                            <dx:LayoutItemNestedControlContainer>
                                <table>
                                    <tr>
                                        <td>
                                            <dx:ASPxLabel runat="server" ID="ASPxLabel2" ClientInstanceName="fileDaImportareLbl" Text="File da importare:" />
                                        </td>
                                        <td>
                                            <dx:ASPxUploadControl ID="CentroDiCostoUploader" Width="600px" runat="server" ClientInstanceName="uploader"
                                                ShowProgressPanel="True" OnFileUploadComplete="upldImport_FileUploadComplete" FileUploadMode="OnPageLoad"
                                                CssClass="btnInline">
                                                <ValidationSettings AllowedFileExtensions=".csv,.xls,.xlsx">
                                                </ValidationSettings>
                                                <ClientSideEvents TextChanged="function(s, e) { UpdateUploadButton(); }" FileUploadStart="function(s, e) { Uploader_OnUploadStart(); }"
                                                FileUploadComplete="function(s, e) { Uploader_OnFilesUploadComplete(e); }" />
                                            </dx:ASPxUploadControl>
                                        </td>
                                        <td style="width: 30%; padding-left: 5px;">
                                            <dx:ASPxButton ID="ASPxButton2" Width="100%" runat="server" ClientInstanceName="btnUpload" HorizontalAlign="Center" VerticalAlign="Top"
                                                ClientEnabled="False" UseSubmitBehavior="false" Text="Import">
                                                <ClientSideEvents Click="function(s, e) { uploader.Upload(); }" />
                                            </dx:ASPxButton>
                                        </td>
                                    </tr>
                                </table>
                            </dx:LayoutItemNestedControlContainer>
                        </LayoutItemNestedControlCollection>
                    </dx:LayoutItem>
                </Items>
            </dx:LayoutGroup>
        </Items>
    </dx:ASPxFormLayout>

    <script type="text/javascript">
        function UpdateUploadButton() {

            if (typeof cmbFil != 'undefined') {

                var comboFil = ASPxClientComboBox.Cast(cmbFil);

                if (comboFil.GetValue() != null && comboFil.GetValue() != null && uploader.GetText(0) != "")
                    btnUpload.SetEnabled(true);
                else
                    btnUpload.SetEnabled(false);
            }
            else {
                if (uploader.GetText(0) != "")
                    btnUpload.SetEnabled(true);
                else
                    btnUpload.SetEnabled(false);
            }
        }

        function Uploader_OnUploadStart() {
            btnUpload.SetEnabled(false);
        }

        function Uploader_OnFilesUploadComplete(e) {
            if (e.errorText) {
                var errorResult = e.errorText.split('|');
                DisplayDialogError(errorResult[0], errorResult[1]);
            } else {

            }
        }
    </script>

</asp:Content>
