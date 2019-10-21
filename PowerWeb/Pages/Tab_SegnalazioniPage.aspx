<%@ Page Title="PowerWeb - Tab_Segnalazioni" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Tab_SegnalazioniPage.aspx.cs" Inherits="PowerWeb.Pages.Tab_SegnalazioniPage" %>


<%@ Import Namespace="Newtonsoft.Json.Linq"%>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">

    <script type="text/javascript">function GetGridResources() {return (<%= JObject.FromObject(_gridResources)%>);}</script> 

    <script type="text/javascript" src="../Scripts/devextreme-modules/dx.aspnet.data.js"></script>
    <script type="text/javascript" src="../Scripts/Custom/Tab_SegnalazioniPage/Tab_SegnalazioniController.js"></script>
    <link type="text/css" href="../Styles/CSS/Tab_SegnalazioniPage/Tab_SegnalazioniPage.css" rel="stylesheet" />


</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div ng-app="tab_segnalazioniModule" ng-controller="tab_segnalazioniController">
        <div dx-data-grid="maingridOptions"></div>
    </div>
</asp:Content>
