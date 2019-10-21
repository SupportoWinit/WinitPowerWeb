<%@ Page Title="PowerWeb - ErrorPage" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="ErrorPage.aspx.cs" Inherits="PowerWeb.Pages.ErrorPages.ErrorPage" %>


<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .circle_container {
            width: 400px;
            height: 400px;
            margin:0 auto;
            padding: 0;
            /*	border : 1px solid red; */
        }

        .circle_main {
            width: 100%;
            height: 100%;
            border-radius: 50%;
            background-color:orange;
            margin: 0;
            padding: 0;
        }

        .circle_text_container {
            /* area constraints */
            width: 70%;
            height: 70%;
            max-width: 70%;
            max-height: 70%;
            margin: 0;
            padding: 0;
            /* some position nudging to center the text area */
            position: relative;
            left: 15%;
            top: 15%;
            /* preserve 3d prevents blurring sometimes caused by the text centering in the next class */
            transform-style: preserve-3d;
            /*border : 1px solid green;*/
        }

        .circle_text {
            /* change font/size/etc here */
            font: 30px "Tahoma", Arial, Serif;
            text-align: center;
            /* vertical centering technique */
            position: relative;
            top: 50%;
            transform: translateY(-50%);
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div class="circle_container">
        <div class="circle_main">
            <div class="circle_text_container">
                <div class="circle_text">
                    Ops!<br> Si è verificato un errore. <br>Contattare l'assistenza se necessario!
                </div>
            </div>
        </div>
    </div>
</asp:Content>
