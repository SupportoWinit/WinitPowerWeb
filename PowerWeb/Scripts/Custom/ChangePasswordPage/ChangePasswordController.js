

var module = angular.module('changePasswordModule', ['dx']);

var form = {};

var _answerTextBox;
var _secretQuestionTextBox;
var _validationGroup;

module.controller("changePasswordController", function ($scope) {

    $scope.changePasswordValidationGroupOptions = {
        onInitialized:function(e){
            _validationGroup = e.component;
        },
    }


    $scope.answerTextBoxOptions = {

        elementAttr: { "class": "form-input" },
        onInitialized:function(e){
            form.answerTextBox = e.component;
        },
        placeholder: "Risposta",
        
    }

    $scope.secretQuestionTextBoxOptions = {
       
        elementAttr: { 'class': 'form-input' },
        hint:"Rispondi alla domanda per verificare la tua identità",
        onInitialized: function (e) {
            form.secretQuestionTextBox = e.component;
        },
        readOnly: true,
        value: getSecretQuestion(),
    }

    $scope.submitButtonOptions = {

        elementAttr:{"class": "form-submit-button" },
        onClick: function (e) {
            if (form.answerTextBox.option("value")) {
                jQuery.ajax({
                    url: "ChangePasswordPage.aspx/ValidateQuestion",
                    type: "POST",
                    contentType: "application/json",
                    data: JSON.stringify({ answer: form.answerTextBox.option("value") })
                }).done(result => {
                    if (result.d == "true") {

                        form.secretQuestionTextBox.option("readOnly", false);
                        form.secretQuestionTextBox.reset();
                        form.secretQuestionTextBox.option("placeholder", "Nuova password");
                        form.secretQuestionTextBox.option("mode", "password");

                        form.answerTextBox.reset();
                        form.answerTextBox.option("placeholder", "Conferma password");
                        form.answerTextBox.option("mode", "password");

                        form.newPassWord = form.answerTextBox;

                        e.component.option("onClick", $scope.validatePassword);

                    } else {
                        DevExpress.ui.notify("La risposta inserita non è corretta!");
                    }
                }).fail(result => console.log(result))
            }
            else {
                DevExpress.ui.notify("Inserire una risposta valida!", "error", 3000);
            }
        },
        text: "Sottoponi"

    }

    $scope.validatePassword = function (e) {
        if (_validationGroup.validate().isValid) { //Controllare password uguali
            jQuery.ajax({
                url: "ChangePasswordPage.aspx/ValidatePassword",
                type: "POST",
                data: JSON.stringify({ password: form.secretQuestionTextBox.option("value") }),
                contentType: "application/json"
            }).done(result => {
                
                var validationResult = JSON.parse(result.d);

                if(validationResult.isValid){
                    DevExpress.ui.dialog.alert("Password modificata con successo! Sarete reindirizzati alla pagina di login!", 'Attenzione').done(function () { window.location.replace("LoginPage.aspx") });
                } else {
                    validationResult.formErrors.forEach((item) => {
                        form[item.field].element().dxValidator("instance").option("validationRules").push({ type: 'custom', message: item.message, validationCallback: function (a) { return false; } });
                    });

                    _validationGroup.validate();

                    validationResult.formErrors.forEach((item) => {
                        form[item.field].element().dxValidator("instance").option("validationRules").pop();
                    });
                }
               
            });
        }
    }

});