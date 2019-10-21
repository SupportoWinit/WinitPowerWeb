<%@ Page Title="PowerWeb - Tracking" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="TrackingPage.aspx.cs" Inherits="PowerWeb.Pages.TrackingPage" %>

<%@ Register TagPrefix="pw" TagName="TrackingModule" Src="~/Modules/TrackingModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:TrackingModule runat="server" ID="mdlTracking" />
</asp:Content>
