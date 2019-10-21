<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MonteMinutiModule.ascx.cs"
    Inherits="PowerWeb.Modules.MonteMinutiModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>

<link href="/Styles/CSS/MonteMinutiPage/MonteMinutiPage.css" rel="stylesheet" type="text/css"/>

<script async type="text/javascript">function dateChanged_EndCallback(s) { jQuery('#retrieveMonteminutiLoadPanel').dxLoadPanel("instance").hide(); if (s.cpErrorMessage != "") { DevExpress.ui.notify(s.cpErrorMessage, "error", 5000); } else { colMonteminuti = JSON.parse(s.cpMonteminutiArray); angular.element(jQuery("body")).scope().getColMonteminuti(); } }</script>
<script async type="text/javascript">function rowUpdate_EndCallback(s) { jQuery('#updateMonteminutiLoadPanel').dxLoadPanel("instance").hide(); if (s.cpErrorMessage != "") { DevExpress.ui.notify(s.cpErrorMessage, "error", 5000); } else { DevExpress.ui.notify("Monteminuti aggiornato!", "success", 1000); } }</script>

<html>
    <body ng-app ="monteminuti" ng-controller ="monteminutiController">
        <div id="datePicker" dx-date-box="datePickerOptions"></div>
        <div id="colGrid" dx-data-grid="dataGridOptions"></div>
                
        <!-- Pannelli di caricamento -->
        <div id="retrieveMonteminutiLoadPanel"></div>
        <div id="updateMonteminutiLoadPanel"></div>
        <!-- Variabili per il passaggio di dati tra server-side e client-side -->
        <input type="hidden" id="selectedDateId" value="" runat="server"/>
        <input type="hidden" id="rowToUpdateId" value="" runat="server"/>
    </body>
</html>

<!-- Callback dell'elaborazione del cartellino (scatenato alla pressione di generateCartellinoButton, dopo i controlli di validità sui dati) -->
<dx:ASPxCallback runat="server" ID="dateChanged" ClientInstanceName="dateChanged" OnCallback="dateChanged_OnCallback"  ClientSideEvents-CallbackComplete="dateChanged_EndCallback">
    <ClientSideEvents CallbackComplete="dateChanged_EndCallback"></ClientSideEvents>
</dx:ASPxCallback>
<!-- Callback del salvataggio del monteminuti (scatenato alla pressione di 'salva' della griglia) -->
<dx:ASPxCallback runat="server" ID="rowUpdate" ClientInstanceName="rowUpdate" OnCallback="rowUpdate_OnCallback"  ClientSideEvents-CallbackComplete="rowUpdate_EndCallback">
    <ClientSideEvents CallbackComplete="rowUpdate_EndCallback"></ClientSideEvents>
</dx:ASPxCallback>

<script type="text/javascript">function setSelectedParameters(date) { $('#<%=selectedDateId.ClientID%>').val(date); }</script>
<script type="text/javascript">function setRowToUpdate(row) { $('#<%=rowToUpdateId.ClientID%>').val(row); }</script>

<script type="text/javascript" src="/Scripts/Custom/MonteMinutiPage/MonteMinutiController.js"></script>