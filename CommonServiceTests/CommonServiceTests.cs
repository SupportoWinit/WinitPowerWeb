using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Common.Tests
{
    [TestClass()]
    public class CommonServiceTests
    {

        #region GetMinutesNumberFromHHMMString
        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHMMDot()
        {
            string test = "1.35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (1 * 60) + 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHMM()
        {
            string test = "1:35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (1 * 60) + 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHMMDot()
        {
            string test = "23.35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (23 * 60) + 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHMM()
        {
            string test = "23:35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (23 * 60) + 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHMMDot()
        {
            string test = "123.24";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (123 * 60) + 24;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHMM()
        {
            string test = "123:24";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (123 * 60) + 24;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHHMMDot()
        {
            string test = "1234.35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (1234 * 60) + 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHHMM()
        {
            string test = "1234:35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (1234 * 60) + 35;
            Assert.AreEqual(expected, unitUnderTest);
        }


        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHMMDotMinus()
        {
            string test = "-1.35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((1 * 60) + 35);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHMMMinus()
        {
            string test = "-1:35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((1 * 60) + 35);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHMMDotMinus()
        {
            string test = "-23.35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((23 * 60) + 35);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHMMMinus()
        {
            string test = "-23:35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((23 * 60) + 35);
            Assert.AreEqual(expected, unitUnderTest);
        }


        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHMMDotMinus()
        {
            string test = "-123.24";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((123 * 60) + 24);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHMMMinus()
        {
            string test = "-123:24";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((123 * 60) + 24);
            Assert.AreEqual(expected, unitUnderTest);
        }
        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHHMMDotMinus()
        {
            string test = "-1234.35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((1234 * 60) + 35);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHHHMMMinus()
        {
            string test = "-1234:35";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -((1234 * 60) + 35);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHH()
        {
            string test = "12";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = (12 * 60);
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetMinutesNumberFromHHMMStringTestHHMinus()
        {
            string test = "-12";
            int unitUnderTest = CommonService.GetMinutesNumberFromHHMMString(test);
            int expected = -(12 * 60);
            Assert.AreEqual(expected, unitUnderTest);
        }
        #endregion

        #region GetDoubleFromMinutes
        [TestMethod()]
        public void GetDoubleFromMinutesTest35Dec()
        {
            int test = 35;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = 0.58;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest35()
        {
            int test = 35;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = 0.35;
            Assert.AreEqual(expected, unitUnderTest);
        }
        [TestMethod()]
        public void GetDoubleFromMinutesTest35DecMinus()
        {
            int test = -35;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = -0.58;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest35Minus()
        {
            int test = -35;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = -0.35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest658Dec()
        {
            int test = 658;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = 10.97;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest658()
        {
            int test = 658;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = 10.58;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest658DecMinus()
        {
            int test = -658;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = -10.97;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest658Minus()
        {
            int test = -658;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = -10.58;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest264Dec()
        {
            int test = 264;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = 4.4;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest264()
        {
            int test = 264;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = 4.24;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest264DecMinus()
        {
            int test = -264;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = -4.4;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest264Minus()
        {
            int test = -264;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = -4.24;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest12345Dec()
        {
            int test = 12345;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = 205.75;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest12345()
        {
            int test = 12345;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = 205.45;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest12345DecMinus()
        {
            int test = -12345;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, true);
            double expected = -205.75;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromMinutesTest12345Minus()
        {
            int test = -12345;
            double unitUnderTest = CommonService.GetDoubleFromMinutes(test, false);
            double expected = -205.45;
            Assert.AreEqual(expected, unitUnderTest);
        }
        #endregion

        #region FromHoursToMinutes
        [TestMethod()]
        public void FromHoursToMinutesTest35Dec()
        {
            double test = 0.58;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest35()
        {
            double test = 0.35;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = 35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest35DecMinus()
        {
            double test = -0.58;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = -35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest35Minus()
        {
            double test = -0.35;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = -35;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest658Dec()
        {
            double test = 10.97;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = 658;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest658()
        {
            double test = 10.58;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = 658;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest658DecMinus()
        {
            double test = -10.97;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = -658;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest658Minus()
        {
            double test = -10.58;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = -658;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest264Dec()
        {
            double test = 4.4;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = 264;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest264()
        {
            double test = 4.24;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = 264;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest264DecMinus()
        {
            double test = -4.4;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = -264;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest264Minus()
        {
            double test = -4.24;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = -264;
            Assert.AreEqual(expected, unitUnderTest);
        }


        [TestMethod()]
        public void FromHoursToMinutesTest12345Dec()
        {
            double test = 205.75;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = 12345;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest12345()
        {
            double test = 205.45;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = 12345;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest12345DecMinus()
        {
            double test = -205.75;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, true);
            int expected = -12345;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void FromHoursToMinutesTest12345Minus()
        {
            double test = -205.45;
            int unitUnderTest = CommonService.FromHoursToMinutes(test, false);
            int expected = -12345;
            Assert.AreEqual(expected, unitUnderTest);
        }
        #endregion

        #region GetDoubleFromTimeSpan
        [TestMethod()]
        public void GetDoubleFromTimeSpan225()
        {
            TimeSpan test = new TimeSpan(2, 25, 0);
            double unitUnderTest = CommonService.GetDoubleFromTimeSpan(test);
            double expected = 2.25;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromTimeSpan225Cent()
        {
            TimeSpan test = new TimeSpan(2, 25, 0);
            double unitUnderTest = Math.Round(CommonService.GetDoubleFromTimeSpan(test, true), 2);
            double expected = 2.42;

            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromTimeSpan0()
        {
            TimeSpan test = new TimeSpan(0, 0, 0);
            double unitUnderTest = CommonService.GetDoubleFromTimeSpan(test);
            double expected = 0;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromTimeSpan0Cent()
        {
            TimeSpan test = new TimeSpan(0, 0, 0);
            double unitUnderTest = Math.Round(CommonService.GetDoubleFromTimeSpan(test, true), 2);
            double expected = 0;

            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromTimeSpan2359()
        {
            TimeSpan test = new TimeSpan(23, 59, 0);
            double unitUnderTest = CommonService.GetDoubleFromTimeSpan(test);
            double expected = 23.59;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void GetDoubleFromTimeSpan2359Cent()
        {
            TimeSpan test = new TimeSpan(23, 59, 0);
            double unitUnderTest = Math.Round(CommonService.GetDoubleFromTimeSpan(test, true), 2);
            double expected = 23.98;

            Assert.AreEqual(expected, unitUnderTest);
        }
        #endregion

        #region SumDoubleHours
        [TestMethod()]
        public void SumDoubleHours401()
        {
            double first = 1.25;
            double second = 2.36;
            double unitUnderTest = CommonService.SumDoubleHours(first, second);
            double expected = 4.01;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void SumDoubleHours401Cent()
        {
            double first = 1.25;
            double second = 2.36;
            double unitUnderTest = CommonService.SumDoubleHours(first, second, true);
            double expected = 3.61;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void SumDoubleHours0()
        {
            double first = 0;
            double second = 0;
            double unitUnderTest = CommonService.SumDoubleHours(first, second);
            double expected = 0;
            Assert.AreEqual(expected, unitUnderTest);
        }

        [TestMethod()]
        public void SumDoubleHours0Cent()
        {
            double first = 0;
            double second = 0;
            double unitUnderTest = CommonService.SumDoubleHours(first, second, true);
            double expected = 0;
            Assert.AreEqual(expected, unitUnderTest);
        }

        #endregion

        [TestMethod()]
        public void SubtractDoubleHoursTest()
        {
            double first = 3.25;
            double second = 2.36;
            double unitUnderTest = CommonService.SubtractDoubleHours(first, second);
            double expected = 0.49;
            Assert.AreEqual(expected, unitUnderTest);
        }
    }
}