<%@ Page Title="PowerWeb - Segnalazioni" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="SegnalazioniPage.aspx.cs" Inherits="PowerWeb.Pages.SegnalazioniPage" %>

<%@ Import Namespace="Newtonsoft.Json.Linq"%>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">

    <script type="text/javascript">function IsSegnToImport() {return (<%= _filesToImport.ToString().ToLower()%>);}</script>
    <script type="text/javascript">function GetTabExcelModels() {return (<%= JArray.FromObject(_pageExcelModels)%>);}</script>
    <script type="text/javascript">function GetGridResources() {return (<%=  JObject.FromObject(_gridResources)%>);}</script>

    <script type="text/javascript" src="../Scripts/devextreme-modules/dx.aspnet.data.js"></script>
    <script type="text/javascript" src="../Scripts/Custom/SegnalazioniPage/SegnalazioniController.js"></script>
    <link href="../Styles/CSS/SegnalazioniPage/SegnalazioniPage.css" rel="stylesheet" type="text/css" />


</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div ng-app="segnalazioniModule" ng-controller="segnalazioniController">

        <div class="commandsRow">
            <div dx-button="importButtonOptions"></div>
            <div dx-button="elabButtonOptions"></div>
            <div dx-button="printButtonOptions"></div>
        </div>

        <div dx-data-grid="maingridOptions"></div>

        <div id="printPopup" dx-popup="printPopupOptions"></div>
        <div id="elaboratePopup" dx-popup="elaboratePopupOptions"></div>

    </div>

</asp:Content>
