/*
{************************************************************************************}
{                                                                                    }
{   DO NOT MODIFY THIS FILE!                                                         }
{                                                                                    }
{   It will be overwritten without prompting when a new version becomes              }
{   available. All your changes will be lost.                                        }
{                                                                                    }
{   This file contains the default template and is required for the appointment      }
{   rendering. Improper modifications may result in incorrect appearance of the      }
{   appointment.                                                                     }
{                                                                                    }
{   In order to create and use your own custom template, perform the following       }
{   steps:                                                                           }
{       1. Save a copy of this file with a different name in another location.       }
{       2. Add a Register tag in the .aspx page header for each template you use,    }
{          as follows: <%@ Register Src="PathToTemplateFile" TagName="NameOfTemplate"}
{          TagPrefix="ShortNameOfTemplate" %>                                        }
{       3. In the .aspx page find the tags for different scheduler views within      }
{          the ASPxScheduler control tag. Insert template tags into the tags         }
{          for the views which should be customized.                                 }
{          The template tag should satisfy the following pattern:                    }
{          <Templates>                                                               }
{              <VerticalAppointmentTemplate>                                         }
{                  < ShortNameOfTemplate: NameOfTemplate runat="server"/>            }
{              </VerticalAppointmentTemplate>                                        }
{          </Templates>                                                              }
{          where ShortNameOfTemplate, NameOfTemplate are the names of the            }
{          registered templates, defined in step 2.                                  }
{************************************************************************************}
*/
using System;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using DevExpress.Web.ASPxScheduler;
using DevExpress.Web.ASPxScheduler.Drawing;
using Domain;
using Common;

public partial class VerticalAppointmentTemplate : System.Web.UI.UserControl
{
    VerticalAppointmentTemplateContainer Container { get { return (VerticalAppointmentTemplateContainer)Parent; } }
    VerticalAppointmentTemplateItems Items { get { return Container.Items; } }

    Reg_V regVStub = null;

    private String GetString(object obj)
    {
        if (obj == null)
            return String.Empty;
        return obj.ToString();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        appointmentDiv.Style.Value = Items.AppointmentStyle.GetStyleAttributes(Page).Value;
        lblCol.ControlStyle.MergeWith(Items.StartTimeText.Style);
        lblCant.ControlStyle.MergeWith(Items.EndTimeText.Style);
        lblTime.ControlStyle.MergeWith(Items.Title.Style);
        dSeparator.Style.Value = Items.HorizontalSeparator.Style.GetStyleAttributes(Page).Value;
        lblMotiv.ControlStyle.MergeWith(Items.Description.Style);

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
        if (String.IsNullOrEmpty(lblMotiv.Text))
            dSeparator.Visible = false;
    }
}