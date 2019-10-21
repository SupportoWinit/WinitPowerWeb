<%@ Page Title="PowerWeb - Schedulazioni" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="SchedulesPage.aspx.cs" Inherits="PowerWeb.Pages.SchedulesPage" %>

<%@ Register TagPrefix="pw" TagName="SchedulesModule" Src="~/Modules/SchedulesModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:SchedulesModule runat="server" ID="mdlSchedules" />
</asp:Content>
