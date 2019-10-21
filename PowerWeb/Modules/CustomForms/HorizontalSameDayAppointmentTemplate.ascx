<%@ Control Language="C#" AutoEventWireup="true" Inherits="HorizontalSameDayAppointmentTemplate" Codebehind="HorizontalSameDayAppointmentTemplate.ascx.cs" %>
<%@ Register Assembly="DevExpress.Web.ASPxScheduler.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxScheduler" TagPrefix="dxwschs" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dxe" %>
<div id="appointmentDiv" runat="server" class='<%#((HorizontalAppointmentTemplateContainer)Container).Items.AppointmentStyle.CssClass %>'>
       <table width="100%" cellpadding="1" cellspacing="0">
        <tr>
            <td class="SchedulerBackHeader">
                <dxe:ASPxLabel runat="server" EnableViewState="false" EncodeHtml="true" ID="lblCol" Font-Bold="true">
                </dxe:ASPxLabel>
            </td>
        </tr>
        <tr>
            <td>
               <%-- <div runat="server" id="Div1" class='<%#((VerticalAppointmentTemplateContainer)Container).Items.HorizontalSeparator.Style.CssClass %>'>
                </div>--%>
            </td>
        </tr>
        <tr>
            <td style="text-align: center;">
                <dxe:ASPxLabel runat="server" EnableViewState="false" EncodeHtml="true" ID="lblCant">
                </dxe:ASPxLabel>
            </td>
        </tr>
        <tr>
            <td style="text-align: center;">
                <dxe:ASPxLabel runat="server" EnableViewState="false" EncodeHtml="true" ID="lblTime" Font-Bold="true">
                </dxe:ASPxLabel>
            </td>
        </tr>
        <tr>
            <td>
              <%--  <div runat="server" id="dSeparator" class='<%#((VerticalAppointmentTemplateContainer)Container).Items.HorizontalSeparator.Style.CssClass %>'>
                </div>--%>
            </td>
        </tr>
        <tr>
            <td>
                <dxe:ASPxLabel runat="server" EnableViewState="false" EncodeHtml="true" ID="lblMotiv">
                </dxe:ASPxLabel>
            </td>
        </tr>
    </table>
</div>