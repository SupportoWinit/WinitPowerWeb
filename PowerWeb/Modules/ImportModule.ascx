<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ImportModule.ascx.cs" Inherits="PowerWeb.Modules.ImportModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxUploadControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxTimer" TagPrefix="dx" %>
<% if (DesignMode)
   {  %><script src="~/Scripts/ASPxScriptIntelliSense.js" type="text/javascript"></script><% }  %>

<script type="text/javascript">

    //#region -------------- Gestione UPLODA LOGO---------------------------------
    var uploadLogoCompleteFlag;

    function UploaderLogo_OnUploadStart()
        //Quando si preme il Tasto di Start dell'UPLOAD del Logo aziendale
        //Disabilita il Tasto di Upload del Logo
    {
        btnUploadLogo.SetEnabled(false);
    }

    function UploaderLogo_OnFilesUploadComplete(args)
        //Quando è stato completato l'UPLOAD del Logo Aziendale
        //Mostra gli eventuale Messaggi di Errore (se ce ne sono)
    {
        UpdateUploadLogoButton();
    }

    function UpdateUploadLogoButton() {
        btnUploadLogo.SetEnabled(upldLogo.GetText(0) != "");
    }

    //#endregion

    //#region ----------------Gestione UPLOAD da POWER/COMO ACCESS ---------------------
    var uploadAccessCompleteFlag;

    function UploaderAccess_OnUploadStart() {
        btnUploadAccess.SetEnabled(false);
    }

    function UploaderAccess_OnFilesUploadComplete(e) {
        if (e.errorText) {
            var errorResult = e.errorText.split('|');
            DisplayDialogError(errorResult[0], errorResult[1]);
        } else {
            tUplAccessPing.SetEnabled(true);
            cUplAccessCommand.PerformCallback();
        }
    }

    function UpdateUploadAccessButton() {
        btnUploadAccess.SetEnabled(upldAccess.GetText(0));
    }

    function btnUploadAccess_OnClick(s, e) {
        cUplAccessCommand.PerformCallback();
        tUplAccessPing.SetEnabled(true);
    }

    function tUplAccessPing_OnTick(s, e) {
        cUplAccessPing.PerformCallback();
    }

    function cUplAccessCommand_OnCallbackComplete(s, e) {
        tUplAccessPing.SetEnabled(false);
        pbProgress.SetPosition(100);
        var callbackResult = e.result.split('|');
        if (callbackResult[0] == 'Error') {
            DisplayDialogError(callbackResult[1], callbackResult[2]);
        } else {
            lblResult.SetText(callbackResult[1]);
        }
        btnUploadAccess.SetEnabled(false);
    }

    function cUplAccessPing_OnCallbackComplete(s, e) {
        var progrAndCommandArray = e.result.split('|');
        var currentPB = ASPxClientProgressBar.Cast(pbProgress);
        var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
        currentPB.SetPosition(currentProgress);
        lblResult.SetText(progrAndCommandArray[1]);
    }

    //#endregion

</script>

<div style="clear: both">
    <dx:ASPxFormLayout ID="flImportMdb" runat="server" Width="80%">
        <Items>
            <dx:LayoutGroup Caption="Import da Access" SettingsItemHelpTexts-Position="Bottom">
                <Items>
                    <dx:LayoutItem Caption="File da importare" HelpText="">
                        <LayoutItemNestedControlCollection>
                            <dx:LayoutItemNestedControlContainer>
                                <table>
                                    <tr>
                                        <td>
                                            <dx:ASPxUploadControl ID="UploadAccess" Width="600px" runat="server" ClientInstanceName="upldAccess"
                                                ShowProgressPanel="True"
                                                OnFileUploadComplete="upldAccess_FileUploadComplete">
                                                <ClientSideEvents
                                                    TextChanged="function(s, e) { UpdateUploadAccessButton(); }"
                                                    FileUploadStart="function(s, e) { UploaderAccess_OnUploadStart(); }"
                                                    FileUploadComplete="function(s, e) { UploaderAccess_OnFilesUploadComplete(e); }" />
                                                <ValidationSettings AllowedFileExtensions=".mdb">
                                                </ValidationSettings>
                                            </dx:ASPxUploadControl>
                                        </td>
                                        <td style="width: 30%; padding-left: 5px;">
                                            <dx:ASPxButton ID="btnUploadAccess" Width="100%" runat="server" AutoPostBack="False" ClientInstanceName="btnUploadAccess"
                                                ClientEnabled="False" HorizontalAlign="Center" VerticalAlign="Top" UseSubmitBehavior="false">
                                                <ClientSideEvents Click="function(s, e) { upldAccess.Upload(); }" />
                                            </dx:ASPxButton>
                                        </td>
                                    </tr>
                                </table>
                            </dx:LayoutItemNestedControlContainer>
                        </LayoutItemNestedControlCollection>
                    </dx:LayoutItem>
                    <dx:LayoutItem Caption="Avanzamento" HelpText="">
                        <LayoutItemNestedControlCollection>
                            <dx:LayoutItemNestedControlContainer>
                                <table>
                                    <tr>
                                        <td style="vertical-align: top">
                                            <dx:ASPxProgressBar ID="ASPxProgressBar1" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="pbProgress">
                                            </dx:ASPxProgressBar>
                                        </td>
                                        <td style="padding-left: 5px;">
                                            <dx:ASPxLabel ID="lblResult" runat="server" Width="100%" ClientInstanceName="lblResult">
                                            </dx:ASPxLabel>
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

    <dx:ASPxFormLayout ID="flImportLogo" runat="server" Width="80%">
        <Items>
            <dx:LayoutGroup Caption="Import logo aziendale (in formato jpg)" SettingsItemHelpTexts-Position="Bottom">
                <Items>
                    <dx:LayoutItem Caption="File da importare" HelpText="">
                        <LayoutItemNestedControlCollection>
                            <dx:LayoutItemNestedControlContainer>
                                <table cellspacing="0" cellpadding="0">
                                    <tr>
                                        <td>
                                            <dx:ASPxUploadControl ID="UploadLogo" Width="600px" runat="server" ClientInstanceName="upldLogo"
                                                ShowProgressPanel="True" OnFileUploadComplete="upldLogo_FileUploadComplete">
                                                <ClientSideEvents
                                                    TextChanged="function(s, e) { UpdateUploadLogoButton(); }"
                                                    FileUploadStart="function(s, e) { UploaderLogo_OnUploadStart(); }"
                                                    FilesUploadComplete="function(s, e) { UploaderLogo_OnFilesUploadComplete(e); }" />
                                                <ValidationSettings AllowedFileExtensions=".jpg,.jpeg,.jpe,.gif,.png">
                                                </ValidationSettings>
                                            </dx:ASPxUploadControl>
                                        </td>
                                        <td style="width: 30%; padding-left: 5px;">
                                            <dx:ASPxButton ID="btnUploadLogo" Width="100%" runat="server" AutoPostBack="False" ClientInstanceName="btnUploadLogo"
                                                ClientEnabled="False" UseSubmitBehavior="false">
                                                <ClientSideEvents Click="function(s, e) { upldLogo.Upload(); }" />
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
</div>

<div>
    <dx:ASPxCallback ID="cUplAccessCommand" ClientInstanceName="cUplAccessCommand" runat="server" OnCallback="cUplAccessCommand_Callback">
        <ClientSideEvents CallbackComplete="cUplAccessCommand_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cUplAccessPing" ClientInstanceName="cUplAccessPing" runat="server" OnCallback="cUplAccessPing_Callback">
        <ClientSideEvents CallbackComplete="cUplAccessPing_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxTimer ID="tUplAccessPing" ClientInstanceName="tUplAccessPing" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tUplAccessPing_OnTick" />
    </dx:ASPxTimer>
</div>






