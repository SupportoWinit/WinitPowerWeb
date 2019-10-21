<%@ Page Title="PowerWeb - Elaborazione registrazioni" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="ElaboratePage.aspx.cs" Inherits="PowerWeb.Pages.ElaboratePage" EnableSessionState="ReadOnly" %>

<%@ Register TagPrefix="pw" TagName="ElaborateModule" Src="~/Modules/ElaborateModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ElaborateModule runat="server" ID="mdlElaborateModule" />
</asp:Content>
