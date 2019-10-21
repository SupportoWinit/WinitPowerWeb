<%@ Page Title="PowerWeb - Cartellino presenze" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" CodeBehind="TimesheetPage.aspx.cs" Inherits="PowerWeb.Pages.TimesheetPage" %>
<%@ Register TagPrefix="pw" TagName="TimesheetModule" Src="~/Modules/TimesheetModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:TimesheetModule runat="server" ID="mdlTimesheetModule" />
</asp:Content>
