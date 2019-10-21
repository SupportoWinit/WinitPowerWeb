using Business.BusinessClasses.CartellinoServiceDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Classes.Config
{
    internal class TimesheetConfig<T> where T : class
    {
        private T _entità;
        public T Entità
        {
            get
            {
                return _entità;
            }

            set
            {
                _entità = value;
            }
        }

        private DateTime _minDate;
        public DateTime MinDate
        {
            get
            {
                return _minDate;
            }

            set
            {
                _minDate = value;
            }
        }

        private DateTime _maxDate;
        public DateTime MaxDate
        {
            get
            {
                return _maxDate;
            }

            set
            {
                _maxDate = value;
            }
        }

        private DateTime _minSummaryDate;
        public DateTime MinSummaryDate
        {
            get
            {
                return _minSummaryDate;
            }

            set
            {
                _minSummaryDate = value;
            }
        }
        private DateTime _maxSummaryDate;
        public DateTime MaxSummaryDate
        {
            get
            {
                return _maxSummaryDate;
            }

            set
            {
                _maxSummaryDate = value;
            }
        }


        private DateTime _monthRequested;
        public DateTime MonthRequested
        {
            get
            {
                return _monthRequested;
            }

            set
            {
                _monthRequested = value;
            }
        }

        private TimesheetParams _params;
        public TimesheetParams Params
        {
            get
            {
                return _params;
            }

            set
            {
                _params = value;
            }
        }

        
    }
}
