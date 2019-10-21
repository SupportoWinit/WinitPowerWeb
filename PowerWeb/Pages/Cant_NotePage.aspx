<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="Cant_NotePage.aspx.cs" Inherits="PowerWeb.Pages.Cant_NotePage" %>

<%@ Register TagPrefix="pw" TagName="Cant_NoteModule" Src="~/Modules/Cant_NoteModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Cant_NoteModule runat="server" ID="mdlCant_Note" />
</asp:Content>
