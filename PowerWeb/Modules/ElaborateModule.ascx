<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ElaborateModule.ascx.cs" Inherits="PowerWeb.Modules.ElaborateModule" %>
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

    //#region -------------- Gestione UPLOAD--------------------------------
    function Uploader_OnUploadStart() {
        tPingImportTxt.SetEnabled(true);
        btnUpload.SetEnabled(false);
    }

    function Uploader_OnFilesUploadComplete(e) {
        UpdateUploadButton();
        tPingImportTxt.SetEnabled(false);
        if (e.errorText) {
            DisplayDialogError('Power', e.errorText);
        }
    }

    function UpdateUploadButton() {
        btnUpload.SetEnabled(uploader.GetText(0) != "");
    }
    //#endregion -----------------------------------------------------------

    //#region -------------- Gestione IMPORT FILE FROM SERVER --------------

    var returnMessage;

    var sendDelayButton_OnClick = function () {
        cSendDelay.PerformCallback();
    }

    var sendChiamateButton_OnClick = function () {
        cSendChiamate.PerformCallback();
    }

    var scheduleDelayButton_OnClick = function () {
        OpenSchedulerPopup('SendDelayJobData', 'Invio Mail Ritardi', false, '', '', '');
    }

    var scheduleChiamateButton_OnClick = function () {
        OpenSchedulerPopup('SendChiamateJobData', 'Invio Mail Chiamate', false, '', '', '');
    }

    var cSendDelay_OnCallbackComplete = function (s, e) {
        alert(s.cpResult);
    }

    var cSendChiamate_OnCallbackComplete = function (s, e) {
        alert(s.cpResult);
    }

    function LaunchImportFormServer() {

        btnElaborate.SetEnabled(false);
        btnTrips.SetEnabled(false);
        if (typeof CmbSelTipoSched != 'undefined') {
            if (CmbSelTipoSched.GetSelectedItem().value == 'S') {
                OpenSchedulerPopup('ImportJobData', 'Importazione timbrature', false, '', '', '');
            }
            else {
                tPingImportTxt.SetEnabled(true);
                btnImportFromServer.SetEnabled(false);
                cImportFormServer.PerformCallback();
            }
        }
        else {
            tPingImportTxt.SetEnabled(true);
            btnImportFromServer.SetEnabled(false);
            cImportFormServer.PerformCallback();
        }
    }



    function EndImportFormServer(s) {
        tPingImportTxt.SetEnabled(false);
        btnImportFromServer.SetEnabled(false);
        btnTrips.SetEnabled(true);
        btnElaborate.SetEnabled(true);
        returnMessage = s.cpResult;
        delete s.cpResult;
        importTxtPanel.PerformCallback();
    }

    function cImportFormServer_OnCallbackComplete(s, e) {
        console.log(e);
        if(e.result != null || e.result != ""){
            DisplayDialogInfo('Power', e.result);
        }
        EndImportFormServer(s);
    }

    function importTxtPanel_EndCallback(s, e) {
        lblResultImpTxt.SetText(returnMessage);
    }

    //#endregion -----------------------------------------------------------

    //#region -------------- Gestione Cancellazione Reg Sospese ------------
    //#region -------------- Gestione Cancellazione Reg Sospese ------------
    // Le registrazioni sospese vengono spostate nella cartella di backup

    function btnDeleteSuspended_OnClick() {
        // Mostra il pannello di conferma
        DisplayJConfirm("Power", btnDeleteSuspended.cpMessage, function (r) {
            if (r) {
                // In caso di conferma, lancia la 'cancellazione' delle registrazioni sospese
                LaunchDeleteSuspended();
            }
        });
    }

    function LaunchDeleteSuspended() {
        if (typeof CmbSelTipoSched != 'undefined' && CmbSelTipoSched.GetSelectedItem().value == 'S') {
            OpenSchedulerPopup('ImportJobData', 'Importazione timbrature', false, '', '', '');
        }

        else {
            btnDeleteSuspended.SetEnabled(false);
            cDeleteSuspended.PerformCallback();
        }
    }

    function cDeleteSuspended_OnCallbackComplete(s, e) {
        returnMessage = s.cpResult;
        delete s.cpResult;
        importTxtPanel.PerformCallback(s, e);
    }

    //#endregion -----------------------------------------------------------

    //#region -------------- Gestione Scheduler-----------------------------
    function OnAppointmentsSelectionChanged(scheduler, appointmentIds) {
        if (appointmentIds != null && appointmentIds.length == 1) {

            var string = "";

            for (var i = 0; i < appointmentIds.length; i++) {
                string += appointmentIds[i];
            }
        }
    }
    //#endregion -----------------------------------------------------------

    //#region -----------Elaborazione Viaggi -------------------------------
    function btnTrips_onClick(s, e) {
        if (typeof CmbSelTipoSched != 'undefined') {
            if (CmbSelTipoSched.GetSelectedItem().value == 'S') {
                OpenSchedulerPopup('ElaborateTripsJobData', 'Elaborazione viaggi', true, 'StaticPeriod', deFrom.GetValue(), deTo.GetValue());
            } else {
                var to = deTo.GetDate();
                var from = deFrom.GetDate();
                if (to != null && from != null && to >= from) {
                    DisplayJConfirm("Power", btnElaborate.cpMessage, function (r) {
                        if (r) {
                            tPing.SetEnabled(true);
                            cTrips.PerformCallback();
                        }
                    });
                } else DisplayDialogError('Power', btnElaborate.cpErrorMessage);
            }
        }
        else {
            var to = deTo.GetDate();
            var from = deFrom.GetDate();
            if (to != null && from != null && to >= from) {
                DisplayJConfirm("Power", btnElaborate.cpMessage, function (r) {
                    if (r) {
                        tPing.SetEnabled(true);
                        cTrips.PerformCallback();
                    }
                });
            } else DisplayDialogError('Power', btnElaborate.cpErrorMessage);
        }
    }
    //#endregion -----------------------------------------------------------

    //#region -----------Elaborazione REG ----------------------------------
    function btnElaborate_OnClick(s, e) {

        if (typeof CmbSelTipoSched != 'undefined') {
            if (CmbSelTipoSched.GetSelectedItem().value == 'S') {
                OpenSchedulerPopup('ElaborateJobData', 'Elaborazione registrazioni', true, 'StaticPeriod', deFrom.GetValue(), deTo.GetValue());
            }
            else {
                var to = deTo.GetDate();
                var from = deFrom.GetDate();
                if (to != null && from != null && to >= from) {
                    DisplayJConfirm("Power", btnElaborate.cpMessage, function (r) {
                        if (r) {
                            tPing.SetEnabled(true);
                            cElaborate.PerformCallback();
                        }
                    });
                } else DisplayDialogError('Power', btnElaborate.cpErrorMessage);
            }
        }
        else {
            var to = deTo.GetDate();
            var from = deFrom.GetDate();
            if (to != null && from != null && to >= from) {
                DisplayJConfirm("Power", btnElaborate.cpMessage, function (r) {
                    if (r) {
                        tPing.SetEnabled(true);
                        cElaborate.PerformCallback();
                    }
                });
            } else DisplayDialogError('Power', btnElaborate.cpErrorMessage);
        }
    }

    //#endregion -----------------------------------------------------------

    //#region -----------Elab ----------------------------------------------
    function btnAdd2Minutes_OnClick(s, e) {
        var to = deTo.GetDate();
        var from = deFrom.GetDate();
        if (to != null && from != null && to >= from) {
            DisplayJConfirm("Power", cAdd2Minutes.cpMessage, function (r) {
                if (r) {
                    tPing.SetEnabled(true);
                    cAdd2Minutes.PerformCallback();
                }
            });
        } else DisplayDialogError('Power', btnAdd2Minutes.cpErrorMessage);
    }




    function cComplete_OnCallbackComplete(s, e) {
        tPing.SetEnabled(false);
        console.log(e);
        var currentPB = ASPxClientProgressBar.Cast(pbProgress);
        currentPB.SetPosition(100);
        lblResult.SetText('Elaborazione Terminata');
        DisplayDialogInfo('Power', e.result);
    }


    function tPing_OnTick(s, e) {
        cPing.PerformCallback();
    }


    function cPing_OnCallbackComplete(s, e) {
        if (e.result != null) {
            var progrAndCommandArray = e.result.split('|');
            var currentPB = ASPxClientProgressBar.Cast(pbProgress);
            var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
            currentPB.SetPosition(currentProgress);
            lblResult.SetText(progrAndCommandArray[1]);
        }
    }

    function tPingImportTxt_OnTick(s, e) {
        cPingImportTxt.PerformCallback();
    }

    function cPingImportTxt_OnCallbackComplete(s, e) {
        if (e.result != null) {
            var progrAndCommandArray = e.result.split('|');
            var currentPB = ASPxClientProgressBar.Cast(pbProgressImportTxt);
            var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
            currentPB.SetPosition(currentProgress);
            lblResultImpTxt.SetText(progrAndCommandArray[1]);
        }
    }


</script>

<div style="clear: both">

    <dx:ASPxCallbackPanel ID="importTxtPanel" runat="server" Width="100%" ClientInstanceName="importTxtPanel"
        OnCallback="importTxtPanel_Callback" ClientSideEvents-EndCallback="importTxtPanel_EndCallback">
        <ClientSideEvents EndCallback="importTxtPanel_EndCallback"></ClientSideEvents>
        <PanelCollection>
            <dx:PanelContent ID="importTxtPanelContent" runat="server" SupportsDisabledAttribute="True">
                <dx:ASPxFormLayout ID="flImportTXT" runat="server" Width="100%">
                    <Items>
                        <dx:LayoutGroup Caption="Import TXT Timbrature" SettingsItemHelpTexts-Position="Bottom">
                            <Items>
                                <dx:LayoutItem Caption=" " HelpText="">
                                    <LayoutItemNestedControlCollection>
                                        <dx:LayoutItemNestedControlContainer>
                                            <table style="width: 100%">
                                                <tr>
                                                    <td style="width: 20%">
                                                        <dx:ASPxLabel ID="lblFileDaImportare" runat="server"></dx:ASPxLabel>
                                                    </td>
                                                    <td>
                                                        <dx:ASPxUploadControl ID="uploader" runat="server" ClientInstanceName="uploader"
                                                            ShowProgressPanel="True" OnFileUploadComplete="upldImport_FileUploadComplete"
                                                            CssClass="btnInline">
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
                                                <tr>
                                                    <td style="width: 50%;">
                                                        <dx:ASPxLabel ID="LblSeverFileNameToImport" ClientInstanceName="lblServerFileNameToImport" runat="server" />
                                                    </td>
                                                    <td style="padding-left: 5px; width: 25%;">
                                                        <dx:ASPxButton ID="btnImportFromServer" Width="100%" runat="server" ClientInstanceName="btnImportFromServer" HorizontalAlign="Center" VerticalAlign="Top"
                                                            ClientEnabled="False" UseSubmitBehavior="false" AutoPostBack="False">
                                                            <ClientSideEvents Click="function(s, e) { LaunchImportFormServer() }" />
                                                        </dx:ASPxButton>
                                                    </td>
                                                    <td style="padding-left: 5px; width: 25%;">
                                                        <dx:ASPxButton ID="btnDeleteSuspended" Width="100%" runat="server" OnCustomJSProperties="btnDeleteSuspended_CustomJSProperties" ClientInstanceName="btnDeleteSuspended" HorizontalAlign="Center" VerticalAlign="Top"
                                                            ClientEnabled="False" UseSubmitBehavior="false" AutoPostBack="False">
                                                            <ClientSideEvents Click="function(s, e) { btnDeleteSuspended_OnClick() }" />
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
                                                        <dx:ASPxProgressBar ID="ASPxProgressBarImpTxt" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="pbProgressImportTxt">
                                                        </dx:ASPxProgressBar>
                                                    </td>
                                                    <td style="width: 30%">
                                                        <dx:ASPxLabel ID="lblResultImpTxt" Text="" runat="server" ClientInstanceName="lblResultImpTxt"></dx:ASPxLabel>
                                                    </td>
                                                </tr>
                                            </table>
                                        </dx:LayoutItemNestedControlContainer>
                                    </LayoutItemNestedControlCollection>
                                </dx:LayoutItem>
                            </Items>

                            <SettingsItemHelpTexts Position="Bottom"></SettingsItemHelpTexts>
                        </dx:LayoutGroup>
                    </Items>
                </dx:ASPxFormLayout>
            </dx:PanelContent>
        </PanelCollection>
    </dx:ASPxCallbackPanel>


</div>

<dx:ASPxFormLayout ID="flElaborate" runat="server" Width="100%">
    <Items>
        <dx:LayoutGroup Caption="ELABORAZIONI" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table id="ElaborateTable">
                                <tr>
                                    <td style="width: 5%;">
                                        <dx:ASPxLabel ID="lblDal" runat="server" Width="100%" ClientInstanceName="lblDal">
                                        </dx:ASPxLabel>
                                    </td>
                                    <td style="padding-left: 5px; width: 20%;">
                                        <dx:ASPxDateEdit ID="deFrom" ClientInstanceName="deFrom" runat="server"></dx:ASPxDateEdit>
                                    </td>
                                    <td style="padding-left: 5px; width: 5%;">
                                        <dx:ASPxLabel ID="lblAl" runat="server" Width="100%" ClientInstanceName="lblDal">
                                        </dx:ASPxLabel>
                                    </td>
                                    <td style="padding-left: 5px; width: 20%;">
                                        <dx:ASPxDateEdit ID="deTo" ClientInstanceName="deTo" runat="server"></dx:ASPxDateEdit>
                                    </td>
                                    <td style="padding-left: 5px; width: 25%;">
                                        <dx:ASPxButton ID="btnElaborate" Width="100%" ClientInstanceName="btnElaborate" runat="server" OnCustomJSProperties="btnElaborate_CustomJSProperties" AutoPostBack="false" UseSubmitBehavior="false">
                                            <ClientSideEvents Click="btnElaborate_OnClick" />
                                        </dx:ASPxButton>
                                    </td>
                                    <td style="padding-left: 5px; width: 25%;">
                                        <dx:ASPxButton ID="btnTrips" Width="100%" ClientInstanceName="btnTrips" runat="server" OnCustomJSProperties="btnElaborate_CustomJSProperties" AutoPostBack="false" UseSubmitBehavior="false">
                                            <ClientSideEvents Click="btnTrips_onClick" />
                                        </dx:ASPxButton>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding-left: 5px;" />
                                    <td style="padding-left: 5px;" />
                                    <td style="padding-left: 5px;" />
                                    <td style="padding-left: 5px;" />
                                    <td style="padding-left: 50px;" />
                                    <td style="padding-left: 5px; padding-top: 5px;">
                                        <dx:ASPxButton ID="btnLaunchXmlExport" Width="100%" runat="server" AutoPostBack="False" ClientInstanceName="btnLaunchXmlExport" UseSubmitBehavior="False" OnCustomJSProperties="btnLaunchXmlExport_OnCustomJSProperties" OnClick="btnLaunchXmlExport_OnClick">
                                            <ClientSideEvents Click="" />
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
                                        <dx:ASPxProgressBar ID="ASPxProgressBarElaborate" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="pbProgress">
                                        </dx:ASPxProgressBar>
                                    </td>
                                    <td style="width: 100%; float: left">
                                        <dx:ASPxLabel ID="lblResult" Text="MESSAGGIO" runat="server" ClientInstanceName="lblResult"></dx:ASPxLabel>
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

<dx:ASPxFormLayout ID="flLaunchTypeSelection" runat="server" Width="100%">
    <Items>
        <dx:LayoutGroup Caption="Selezione tipo di lancio" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td style="padding-left: 5px;">
                                        <dx:ASPxLabel ID="LblTipoSchedulazione" runat="server" Width="100%" ClientInstanceName="LblTipoSchedulazione" />
                                    </td>
                                    <td style="padding-left: 5px;">
                                        <dx:ASPxComboBox runat="server" ID="CmbSelTipoSched" ClientInstanceName="CmbSelTipoSched" />
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

<dx:ASPxFormLayout ID="flNotificationButtons" runat="server" Width="100%">
    <Items>
        <dx:LayoutGroup Caption="Notifiche" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="" Width="100%">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer Width="100%">
                            <table style="width: 100%">
                                <tr id="boxRitardi" runat="server">
                                    <td id="buttonRitardiImmediati" style="width: 50%; text-align: center; margin: auto; padding-bottom: 10px">
                                        <dx:ASPxButton ID="sendDelayButton" Text="Invia Ritardi" Width="40%" ClientInstanceName="sendDelayButton" runat="server" AutoPostBack="false" UseSubmitBehavior="false">
                                            <ClientSideEvents Click="sendDelayButton_OnClick" />
                                        </dx:ASPxButton>
                                    </td>
                                    <td id="buttonRitardiSchedulati" style="width: 50%; text-align: center; margin: auto; padding-bottom: 10px">
                                        <dx:ASPxButton ID="scheduleDelayButton" Text="Schedula Ritardi" Width="40%" ClientInstanceName="scheduleDelayButton" runat="server" AutoPostBack="false" UseSubmitBehavior="false">
                                            <ClientSideEvents Click="scheduleDelayButton_OnClick" />
                                        </dx:ASPxButton>
                                    </td>
                                </tr>
                                <tr id="boxChiamate" runat="server" style="border-top: 1px Solid #C2C4CB">
                                    <td id="buttonChiamateImmediate" style="width: 50%; text-align: center; margin: auto; padding-top: 10px">
                                        <dx:ASPxButton ID="sendChiamateButton" Text="Invia Chiamate" Width="40%" ClientInstanceName="sendChiamateButton" runat="server" AutoPostBack="false" UseSubmitBehavior="false">
                                            <ClientSideEvents Click="sendChiamateButton_OnClick" />
                                        </dx:ASPxButton>
                                    </td>
                                    <td id="buttonChiamateSchedulate" style="width: 50%; text-align: center; margin: auto 10px; padding-top: 10px">
                                        <dx:ASPxButton ID="scheduleChiamateButton" Text="Schedula Chiamate" Width="40%" ClientInstanceName="scheduleChiamateButton" runat="server" AutoPostBack="false" UseSubmitBehavior="false">
                                            <ClientSideEvents Click="scheduleChiamateButton_OnClick" />
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



<div aria-busy="True">
    <dx:ASPxCallback ID="cSendDelay" ClientInstanceName="cSendDelay" runat="server" OnCallback="cSendDelay_Callback">
        <ClientSideEvents CallbackComplete="cSendDelay_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cSendChiamate" ClientInstanceName="cSendChiamate" runat="server" OnCallback="cSendChiamate_Callback">
        <ClientSideEvents CallbackComplete="cSendChiamate_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cPing" ClientInstanceName="cPing" runat="server" OnCallback="cPing_Callback">
        <ClientSideEvents CallbackComplete="cPing_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxTimer ID="tPing" ClientInstanceName="tPing" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tPing_OnTick" />
    </dx:ASPxTimer>

    <dx:ASPxCallback ID="cElaborate" ClientInstanceName="cElaborate" runat="server" OnCallback="cElaborate_Callback">
        <ClientSideEvents CallbackComplete="cComplete_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cAdd2Minutes" ClientInstanceName="cAdd2Minutes" runat="server" OnCallback="cAdd2Minutes_Callback">
        <ClientSideEvents CallbackComplete="cComplete_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cTrips" ClientInstanceName="cTrips" runat="server" OnCallback="cTrips_Callback">
        <ClientSideEvents CallbackComplete="cComplete_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cImportFormServer" ClientInstanceName="cImportFormServer" runat="server" OnCallback="cImportFormServer_Callback">
        <ClientSideEvents CallbackComplete="cImportFormServer_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cDeleteSuspended" ClientInstanceName="cDeleteSuspended" runat="server" OnCallback="cDeleteSuspended_Callback">
        <ClientSideEvents CallbackComplete="cDeleteSuspended_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxCallback ID="cPingImportTxt" ClientInstanceName="cPingImportTxt" runat="server" OnCallback="cPingImportTxt_Callback">
        <ClientSideEvents CallbackComplete="cPingImportTxt_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxTimer ID="tPingImportTxt" ClientInstanceName="tPingImportTxt" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tPingImportTxt_OnTick" />
    </dx:ASPxTimer>
</div>










