<%@ Page Title="PowerWeb - Autorizzazione Straordinari" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true" 
    CodeBehind="Aut_StrPage.aspx.cs" Inherits="PowerWeb.Pages.Aut_StrPage" %>

<%@ Register TagPrefix="pw" TagName="Aut_StrModule" Src="~/Modules/Aut_StrModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Aut_StrModule runat="server" ID="mdlAut_Str" />
</asp:Content>
