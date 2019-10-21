<%@ Page Title="Accedi" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="LoginPage.aspx.cs" Inherits="PowerWeb.Pages.Account.LoginPage" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
    <script type="text/javascript" src="/Scripts/Custom/LoginPage/LoginController.js"></script>

    <link href="/Styles/CSS/LoginPage/LoginPage.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">

   

    <div ng-app="loginModule" ng-controller="loginController">
        <div class="form-container">

            <input type="hidden" id="enableDoubleCheck" value="<%=Business.Repository.RepoManager.ParamRepo.GetCustomizationFromEnum(Common.CustomizationEnum.EnableDoubleCheckLogin) == 1%>"">

            <div class="form-wrapper">
                <span class="form-title">Accedi</span>

                <div dx-validation-group="loginValidationGroupOptions" class="validationGroup">

                    <div dx-text-box="userNameTextBoxOptions" dx-validator="{name: 'Utente', validationRules: [{type: 'required', message:'Il campo UTENTE è obbligatorio!'}] }" ng-keydown="catchEnterPress($event)"></div>

                    <div dx-text-box="passWordTextBoxOptions" dx-validator="{name:'Password', validationRules: [{type: 'required', message:'Il campo PASSWORD è obbligatorio!'}] }" ng-keydown="catchEnterPress($event)"></div>

                    <div dx-check-box ="requestSuperUserCheckBoxOption"></div>

                    <div dx-text-box="superUserNameTextBoxOptions" ng-keydown="catchEnterPress($event)"></div>

                    <div dx-text-box="superUserPasswordTextBoxOptions"  ng-keydown="catchEnterPress($event)"></div>

                    <p class="changePasswordParagraph">Inserire la nuova password!</p>

                    <div dx-text-box="newPassWordTextBoxOptions" ng-keydown="catchEnterPress($event)"></div>

                    <div dx-text-box="secretQuestionTextBoxOptions"></div>

                    <div dx-text-box="secretAnswerTextBoxOptions"></div>

                    <div class="form-password-request">
                        <a id="forgetPasswordButton" href="" ng-click="forgetPasswordButtonClick()">Password dimenticata</a>
                    </div>

                    <div dx-validation-summary="{}"></div>

                    <div class="form-actions"></div>

                    <div class="form-submit-container">
                        <div id="submitButton" dx-button="submitButtonOptions" ng-keydown="catchEnterPress($event)"></div>
                    </div>

                    

                </div>
            </div>

            <div dx-popup="errorPoup"></div>

        </div>
    </div>
</asp:Content>
