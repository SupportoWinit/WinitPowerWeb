<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Col_NotePage.aspx.cs" Inherits="PowerWeb.Pages.Col_NotePage" %>

<%@ Register TagPrefix="pw" TagName="Col_NoteModule" Src="~/Modules/Col_NoteModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Col_NoteModule runat="server" ID="mdlCol_Note" />
</asp:Content>
