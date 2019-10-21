<%@ Page Title="" Language="C#" MasterPageFile="~/GridMasterPage.Master" AutoEventWireup="true"
    CodeBehind="Cant_VarPage.aspx.cs" Inherits="PowerWeb.Pages.Cant_VarPage" %>

<%@ Register TagPrefix="pw" TagName="Cant_VarModule" Src="~/Modules/Cant_VarModule.ascx" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:Cant_VarModule runat="server" ID="mdlCant_Var" />
</asp:Content>