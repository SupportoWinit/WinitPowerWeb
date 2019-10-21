using System;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using DevExpress.Web.ASPxScheduler;
using DevExpress.Web.ASPxScheduler.Drawing;
using Domain;
using Common;


public partial class HorizontalSameDayAppointmentTemplate : System.Web.UI.UserControl {

    HorizontalAppointmentTemplateContainer Container { get { return (HorizontalAppointmentTemplateContainer)Parent; } }
	HorizontalAppointmentTemplateItems Items { get { return Container.Items; } }

    Reg_V regVStub = null;

    private String GetString(object obj)
    {
        if (obj == null)
            return String.Empty;
        return obj.ToString();
    }

	protected void Page_Load(object sender, EventArgs e) {

        appointmentDiv.Style.Value = Items.AppointmentStyle.GetStyleAttributes(Page).Value;
        lblCol.ControlStyle.MergeWith(Items.StartTimeText.Style);
        lblCant.ControlStyle.MergeWith(Items.EndTimeText.Style);
        lblTime.ControlStyle.MergeWith(Items.Title.Style);
        //dSeparator.Style.Value = Items.HorizontalSeparator.Style.GetStyleAttributes(Page).Value;
        lblMotiv.ControlStyle.MergeWith(Items.StartTimeText.Style);

        var colMemonic = Container.AppointmentViewInfo.Appointment.CustomFields[CommonService.GetPropertyName(() => regVStub.Col_Mnemonic)];
        var colDesc = Container.AppointmentViewInfo.Appointment.CustomFields[CommonService.GetPropertyName(() => regVStub.Col_Desc)];
        var cantMemonic = Container.AppointmentViewInfo.Appointment.CustomFields[CommonService.GetPropertyName(() => regVStub.Cant_Mnemonic)];
        var cantDesc = Container.AppointmentViewInfo.Appointment.CustomFields[CommonService.GetPropertyName(() => regVStub.Cant_Desc)];

        lblCol.Text = String.Format("{0} - {1}", GetString(colDesc), GetString(colMemonic).Trim());
        lblCant.Text = String.Format("{0} - {1}", GetString(cantDesc), GetString(cantMemonic).Trim());

        DateTime start = Container.AppointmentViewInfo.Appointment.Start;
        DateTime end = Container.AppointmentViewInfo.Appointment.End;

        if (end > DateTime.MinValue && start != end)
        {
            TimeSpan delta = end - start;

            DateTime deltaTime = new DateTime(delta.Ticks);

            lblTime.Text = String.Format(@"{0} - {1} [{2}]", start.ToShortTimeString(), end.ToShortTimeString(), deltaTime.ToShortTimeString());
        }
        else
        {
            lblCol.Text += String.Format(@" - [{0}]", start.ToShortTimeString());
        }
        var motivReg = Container.AppointmentViewInfo.Appointment.CustomFields[CommonService.GetPropertyName(() => regVStub.Motivazione_Reg_Cod)];

        lblMotiv.Text = GetString(motivReg);
        //if (String.IsNullOrEmpty(lblMotiv.Text))
        //    dSeparator.Visible = false;
	}
	
}
